using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Pipoga;

namespace Pipoga.Examples
{
    using XnaRect = Microsoft.Xna.Framework.Rectangle;

    public class PixelLinesApp : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Input input;

        PixelDisplay screen;
        UndoStack<IRasterizable> undoStack;
        // Unordered collection of actions to run once during the simulation.
        Queue<Action<PixelLinesApp>> primedActions;

        Point mouseOnScreen;
        Gui gui;

        Point lineDrawStart;
        Line lineBeingDrawn;
        Slider circleRadiusSlider;
        Label radiusLabel;
        Rectangle drawArea;

        public PixelLinesApp(string[] args)
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;

            if (args.Length == 3)
            {
                var pixelSize = new Point(Int32.Parse(args[0]));
                var screenSize = new Point(
                    Int32.Parse(args[1]), Int32.Parse(args[2])
                );
                screen = new PixelDisplay(pixelSize, screenSize);
            }
            else
            {
                screen = new PixelDisplay(new Point(4), new Point(256, 128));
            }

            input = new Input();

            undoStack = new UndoStack<IRasterizable>(0xff);
            primedActions = new Queue<Action<PixelLinesApp>>(0xff);
        }

        protected override void Initialize()
        {
            // Set the app-window according to the screen and pixel size.
            graphics.PreferredBackBufferWidth = screen.ScreenSize.X;
            graphics.PreferredBackBufferHeight = screen.ScreenSize.Y;
            graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // These properties need to be set before drawing.
            screen.PixelTexture = Content.Load<Texture2D>("Pixel");

            // Initialize the GUI.
            int margin = 10;

            var unredoSize = new Point(50, 35);
            var undoPos = new Point(25, 25);
            var redoPos = undoPos + new Point(unredoSize.X + margin, 0);

            var radSize = unredoSize;
            var radDecPos = undoPos + new Point(0, unredoSize.Y + margin);
            var radIncPos = radDecPos + new Point(radSize.X + margin, 0);

            var radLabelPos = radIncPos + new Point(radSize.X + margin, 0);

            var radSliderSize = new Point(150, 50);
            var radSliderPos = radDecPos + new Point(0, radSize.Y + margin);

            var drawAreaPos = new Point(250, 25);
            var drawAreaSize = new Point(750, 450);

            var cursor = new Cursor(
                defaultIcon: new CursorIcon(
                    Content.Load<Texture2D>("Cursor"),
                    new Point(1, 1)
                ),
                pointer: new CursorIcon(
                    Content.Load<Texture2D>("CursorHover"),
                    new Point(4, 1)
                )
            );

            gui = new Gui(cursor, input);

            var buttons = new List<Button> {
                // Undo-button.
                new Button(undoPos, unredoSize,
                    (b) => primedActions.Enqueue(
                        (app) => {
                            if (!app.undoStack.Undo())
                            {
                                b.BackgroundColor = Color.Lerp(
                                    Color.Gray,
                                    b.BackgroundColor,
                                    0.5f
                                );
                            }
                        }
                    ),
                    Color.Pink, Color.Red
                ),
                // Redo-button.
                new Button(redoPos, unredoSize,
                    (b) => primedActions.Enqueue(
                        (app) => {
                            if (!app.undoStack.Redo())
                            {
                                b.BackgroundColor = Color.Lerp(
                                    Color.Gray,
                                    b.BackgroundColor,
                                    0.5f
                                );
                            }
                        }
                    ),
                    Color.LightGreen, Color.Green
                ),
                // Button for increasing circle radius.
                new Button(radIncPos, radSize,
                    (b) => primedActions.Enqueue(
                        (app) => { app.circleRadiusSlider.Value += 10; }
                    ),
                    Color.Blue, Color.Gray
                ),
                // Button for decreasing circle radius.
                new Button(radDecPos, radSize,
                    (b) => primedActions.Enqueue(
                        (app) => { app.circleRadiusSlider.Value -= 10; }
                    ),
                    Color.LightBlue, Color.Gray
                ),
            };
            gui.AddRange(buttons);


            circleRadiusSlider = new Slider(
                min: 10,
                max: 100,
                body: new Rectangle(
                        radSliderPos.ToVector2(),
                        radSliderSize.ToVector2()
                    ),
                slideAxis: Vector2.UnitY
            );
            circleRadiusSlider.Value = 50;


            // Label to show the current circle radius.
            radiusLabel = new Label(
                circleRadiusSlider.Value.ToString(),
                radLabelPos,
                Color.White
            );
            // TODO Make this SpriteFont a constant of Gui-class.
            radiusLabel.Font = new SpriteFont(
                texture: Content.Load<Texture2D>("FontAscii"),
                glyphBounds: Enumerable.Range(0, 10)
                    .Select(i => new XnaRect(i * 10, 0, 10, 10))
                    .ToList(),
                cropping: Enumerable.Range(0, 10)
                    .Select(i => new XnaRect(i * 10, 0, 10, 10))
                    .ToList(),
                characters: "0123456789".ToList(),
                lineSpacing: 10,
                spacing: 10,
                kerning: Enumerable.Range(0, 10)
                    .Select(i => new Vector3(0,0,0))
                    .ToList(),
                defaultCharacter: '0'
            );
            gui.Add(radiusLabel);

            gui.Add(circleRadiusSlider);


            drawArea = new Rectangle(
                drawAreaPos.ToVector2(),
                drawAreaSize.ToVector2()
            );
            drawArea.Color = Color.DarkGray;
        }

        protected override void Update(GameTime gameTime)
        {
            // Run the previously primed actions before anything else.
            {
                Action<PixelLinesApp> action;
                while (primedActions.TryDequeue(out action))
                {
                    action(this);
                }
            }

            input.Update();

            HandleInput();


            UpdateLineBeingDrawn();
            UpdateUI();

            base.Update(gameTime);
        }

        /// <summary>
        /// Perform actions based on user input.
        /// </summary>
        void HandleInput()
        {
            if (input.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // TODO Implement a timer on keypress so that Ctrl-Z (and others)
            // could be input multiple times by holding down.
            if (input.IsKeyCom((Keys.LeftControl, true), (Keys.Z, false)))
            {
                undoStack.Undo();
            }
            if (input.IsKeyCom((Keys.LeftControl, true), (Keys.Y, false)))
            {
                undoStack.Redo();
            }

            mouseOnScreen = screen.ToScreenPos(input.Mouse.position);
            if (input.Mouse.m2WasDown)
            {
                undoStack.Push(
                    new Circle(
                        circleRadiusSlider.Value,
                        input.Mouse.position.ToVector2()
                    )
                );
            }
        }

        /// <summary>
        /// Redraw the line that user is drawing by dragging the mouse.
        /// </summary>
        void UpdateLineBeingDrawn()
        {
            if (input.Mouse.m1WasDown)
            {
                lineDrawStart = mouseOnScreen;
            }

            // Only mouse actions happening over the background-canvas are
            // allowed.
            if (!drawArea.Contains(input.Mouse.position.ToVector2()))
            {
                if (!input.Mouse.m1IsDown)
                {
                    // Line gets drawn if mouse is released outside.
                    CheckStopLineDraw();
                }
                return;
            }

            if (input.Mouse.m1IsDown)
            {
                var end = mouseOnScreen;
                lineBeingDrawn = new Line(
                    lineDrawStart.ToVector2(),
                    end.ToVector2()
                );
            }
            else
            {
                CheckStopLineDraw();
            }
        }

        void CheckStopLineDraw()
        {
            if (lineBeingDrawn != null)
            {
                // User has released, so save the line.
                undoStack.Push(lineBeingDrawn);

                lineBeingDrawn = null;
            }
        }

        /// <summary>
        /// Re-"draw" the UI-elements.
        /// </summary>
        void UpdateUI()
        {
            // TODO Change this into a listener of changes to circleRadius.
            radiusLabel.Text = ((int)circleRadiusSlider.Value).ToString();
        }

        protected override void Draw(GameTime gameTime)
        {
            // Draw on the pixel-screen.
            screen.Clear();
            // Draw elements in order of layer bottom first.
            screen.Plot(drawArea);
            foreach (var x in undoStack)
            {
                screen.Plot(x);
            }
            if (lineBeingDrawn != null)
            {
                screen.Plot(lineBeingDrawn);
            }
            screen.Plot(gui);


            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();


            screen.Draw(spriteBatch);


            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
