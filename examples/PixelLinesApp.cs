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
        // Unordered collection of actions to run once during the simulation.
        Queue<Action<PixelLinesApp>> primedActions;

        Point mouseOnScreen;
        Gui gui;

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
            screen.PixelTexture = Content.Load<Texture2D>("pixel");

            // Initialize the GUI.
            var cursor = new Cursor(
                defaultIcon: new CursorIcon(
                    Content.Load<Texture2D>("cursor"),
                    new Point(1, 1)
                ),
                pointer: new CursorIcon(
                    Content.Load<Texture2D>("cursorHover"),
                    new Point(4, 1)
                )
            );
            gui = new Gui(cursor, input);
            var buttons = new List<Button> {
                new Button(new Point(125, 125), new Point(200, 100),
                    (b) => primedActions.Enqueue(
                        (app) => {
                            if (app.lines.Count > 0)
                            {
                                // Undo the previous line-draw.
                                app.lines.RemoveAt(lines.Count - 1);
                            }
                            else
                            {
                                b.BackgroundColor = Color.Gray;
                            }
                        }
                    )
                ),
                new Button(
                    new Point(300, 400), new Point(400, 200),
                    (_) => primedActions.Enqueue(
                        (app) => {
                            app.gui.Add(
                                new Button(new Point(15, 7), new Point(50, 50))
                            );
                        }
                    ),
                    Color.Pink, Color.Red
                ),
            };
            gui.AddRange(buttons);
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

            // TODO Implement `KeyCombinationPressed` or smth method on Input
            // for Ctrl-Z and other common keyboard inputs.

            mouseOnScreen = screen.ToScreenPos(input.Mouse.position);
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
                screen.Plot(line);
                // Keep coloring the starting pixel of the lines.
                screen[line.start.ToPoint()] = Color.Red;
            }
        }

        /// <summary>
        /// Re-"draw" the UI-elements.
        /// </summary>
        void UpdateUI()
        {
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
