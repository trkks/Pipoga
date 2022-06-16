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
        List<Line> lines;
        List<Button> buttons;
        // Unordered collection of actions to run once during the simulation.
        Queue<Action> primedActions;

        Point mouseOnScreen;
        Cursor cursor;

        Point lineDrawStart;
        Line lineBeingDrawn;

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

            lines = new List<Line>(0xff);
            buttons = new List<Button> {
                new Button(new Point(125, 125), new Point(200, 100)),
                new Button(
                    new Point(300, 400), new Point(400, 200),
                    () => primedActions.Enqueue(
                        () => buttons.Add(
                            new Button(new Point(15, 7), new Point(50, 50))
                        )
                    ),
                    Color.Pink, Color.Red
                ),
            };
            primedActions = new Queue<Action>(0xff);
            cursor = new Cursor();
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
            screen.PixelTexture = Content.Load<Texture2D>("pixel");
            cursor.Default = Content.Load<Texture2D>("cursor");
            cursor.Hover = Content.Load<Texture2D>("cursorHover");
        }

        protected override void Update(GameTime gameTime)
        {
            // Run the previously primed actions before anything else.
            {
                Action action;
                while (primedActions.TryDequeue(out action))
                {
                    action();
                }
            }

            input.Update();
            cursor.Update(input.Mouse);

            HandleInput();

            // Clear the screen.
            screen.Clear();

            UpdateLineBeingDrawn();
            UpdateLines();
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

            mouseOnScreen = screen.ToScreenPos(input.Mouse.position);
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
            if (input.Mouse.m1IsDown)
            {
                var end = mouseOnScreen;
                lineBeingDrawn = new Line(
                    lineDrawStart.ToVector2(),
                    end.ToVector2()
                );
                screen.PlotLine(lineBeingDrawn, Color.White);
                // Color the starting pixel for looking more consistent.
                screen[lineDrawStart] = Color.Red;
            }
            else if (lineBeingDrawn != null)
            {
                // User has released, so save the line.
                lines.Add(lineBeingDrawn);
                lineBeingDrawn = null;
            }
        }

        /// <summary>
        /// Re-"draw" the lines that user has already drawn on the screen.
        /// </summary>
        void UpdateLines()
        {
            foreach (var line in lines)
            {
                screen.PlotLine(line, Color.White);
                // Keep coloring the starting pixel of the lines.
                screen[line.start.ToPoint()] = Color.Red;
            }
        }

        /// <summary>
        /// Re-"draw" the UI-elements.
        /// </summary>
        void UpdateUI()
        {
            foreach (var button in buttons)
            {
                // Rendering.
                var onScreen = new RectangleBody(
                    screen.ToScreenCoords(button.Body.ToRectangle())
                );
                // The buttons are rectangles (at least for now).
                screen.PlotRect(
                    onScreen.ToRectangle(), button.BackgroundColor
                );

                Line[] buttonLines = new[] {
                    new Line(onScreen.TopLeft,     onScreen.TopRight),
                    new Line(onScreen.TopRight,    onScreen.BottomRight),
                    new Line(onScreen.BottomRight, onScreen.BottomLeft),
                    new Line(onScreen.BottomLeft,  onScreen.TopLeft),
                };

                foreach (var line in buttonLines)
                {
                    screen.PlotLine(line, button.ForegroundColor);
                }

                // Actions.
                button.ProcessMouseEvents(input.Mouse);
            }

            screen.PlotCursor(cursor);
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
