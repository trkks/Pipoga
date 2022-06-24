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
        int lastLineHandle;
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
                new Button(new Point(20, 20), new Point(150, 50),
                    (b) => primedActions.Enqueue(
                        (app) => {
                            if (!PixelLinesApp.UndoLineDraw(app))
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
                new Button(new Point(180, 20), new Point(150, 50),
                    (b) => primedActions.Enqueue(
                        (app) => {
                            if (!PixelLinesApp.RedoLineDraw(app))
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
                )
            };
            gui.AddRange(buttons);
        }

        static bool RedoLineDraw(PixelLinesApp app)
        {
            if (app.lastLineHandle < app.lines.Count)
            {
                // "Redo" the previous line-draw.
                app.lastLineHandle++;
                return true;
            }
            return false;
        }

        static bool UndoLineDraw(PixelLinesApp app)
        {
            if (app.lastLineHandle > 0)
            {
                // "Undo" the previous line-draw.
                app.lastLineHandle--;
                return true;
            }
            return false;
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

            // TODO Implement a timer on keypress so that Ctrl-Z (and others)
            // could be input multiple times by holding down.
            if (input.IsKeyCom((Keys.LeftControl, true), (Keys.Z, false)))
            {
                PixelLinesApp.UndoLineDraw(this);
            }
            if (input.IsKeyCom((Keys.LeftControl, true), (Keys.Y, false)))
            {
                PixelLinesApp.RedoLineDraw(this);
            }

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
                // Adding a new line invalidates the redo-stack, and starts a
                // new "branch" of actions.
                lines.RemoveRange(lastLineHandle, lines.Count - lastLineHandle);
                // User has released, so save the line.
                lines.Add(lineBeingDrawn);
                lastLineHandle++;

                lineBeingDrawn = null;
            }
        }

        /// <summary>
        /// Re-"draw" the lines that user has already drawn on the screen.
        /// </summary>
        void UpdateLines()
        {
            for (int i = 0; i < lastLineHandle; i++)
            {
                var line = lines[i];
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
