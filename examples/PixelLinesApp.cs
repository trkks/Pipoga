using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Pipoga;

namespace Pipoga.Examples
{
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

            circleRadiusSlider = new Slider(
                min: 10,
                max: 100,
                body: new Rectangle(100, 150, 300, 150)
            );
            circleRadiusSlider.Value = 50;
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
                new Button(new Point(20, 20), new Point(150, 50),
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
                new Button(new Point(180, 20), new Point(150, 50),
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
                new Button(new Point(20, 90), new Point(50, 25),
                    (b) => primedActions.Enqueue(
                        (app) => { app.circleRadiusSlider.Value += 10; }
                    ),
                    Color.Blue, Color.Gray
                ),
                // Button for decreasing circle radius.
                new Button(new Point(20, 130), new Point(50, 25),
                    (b) => primedActions.Enqueue(
                        (app) => { app.circleRadiusSlider.Value -= 10; }
                    ),
                    Color.LightBlue, Color.Gray
                ),
            };
            gui.AddRange(buttons);

            // Label to show the current circle radius.
            radiusLabel = new Label(
                circleRadiusSlider.Value.ToString(),
                new Point(20, 20),
                Color.White
            );
            // TODO Make this SpriteFont a constant of Gui-class.
            radiusLabel.Font = new SpriteFont(
                texture: Content.Load<Texture2D>("FontAscii"),
                glyphBounds: Enumerable.Range(0, 10)
                    .Select(i => new Rectangle(i * 10, 0, 10, 10))
                    .ToList(),
                cropping: Enumerable.Range(0, 10)
                    .Select(i => new Rectangle(i * 10, 0, 10, 10))
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

            // Clear the screen.
            screen.Clear();

            UpdateObjects();
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
            // Only mouse actions happening over the background-canvas are allowed.
            // TODO Instead of checking against all GUI-elements, implement a
            // specialized Canvas-element for drawing lines.
            if (gui.IsOver(input.Mouse.position))
            {
                return;
            }

            if (input.Mouse.m1WasDown)
            {
                lineDrawStart = mouseOnScreen;
            }
            if (input.Mouse.m1IsDown)
            {
                var end = mouseOnScreen;
                lineBeingDrawn = new Line(
                    lineDrawStart.ToVector2(),
                    end.ToVector2()
                );
                screen.Plot(lineBeingDrawn);
                // Color the starting pixel for looking more consistent.
                screen[lineDrawStart] = Color.Red;
            }
            else if (lineBeingDrawn != null)
            {
                // User has released, so save the line.
                undoStack.Push(lineBeingDrawn);

                lineBeingDrawn = null;
            }
        }

        /// <summary>
        /// Re-"draw" the lines that user has already drawn on the screen.
        /// </summary>
        void UpdateObjects()
        {
            foreach (var obj in undoStack)
            {
                screen.Plot(obj);

                // Keep coloring the starting pixel of the lines.
                if (obj is Line)
                {
                    screen[((Line)obj).start.ToPoint()] = Color.Red;
                }
            }
        }

        /// <summary>
        /// Re-"draw" the UI-elements.
        /// </summary>
        void UpdateUI()
        {
            // TODO Change this into a listener of changes to circleRadius.
            radiusLabel.Text = ((int)circleRadiusSlider.Value).ToString();
            // Rendering.
            screen.Plot(gui);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();


            screen.Draw(spriteBatch);


            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
