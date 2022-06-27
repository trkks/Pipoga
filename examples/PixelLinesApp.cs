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
        List<IRasterizable> objects;
        int lastObjectHandle;
        // Unordered collection of actions to run once during the simulation.
        Queue<Action<PixelLinesApp>> primedActions;

        Point mouseOnScreen;
        Gui gui;

        Point lineDrawStart;
        Line lineBeingDrawn;
        float circleRadius;

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

            objects = new List<IRasterizable>(0xff);
            primedActions = new Queue<Action<PixelLinesApp>>(0xff);

            circleRadius = 50f;
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
                // Undo-button.
                new Button(new Point(20, 20), new Point(150, 50),
                    (b) => primedActions.Enqueue(
                        (app) => {
                            if (!PixelLinesApp.UndoAddObject(app))
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
                            if (!PixelLinesApp.RedoAddObject(app))
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
                        (app) => { app.circleRadius += 10; }
                    ),
                    Color.Blue, Color.Gray
                ),
                // Button for decreasing circle radius.
                new Button(new Point(20, 130), new Point(50, 25),
                    (b) => primedActions.Enqueue(
                        (app) => { app.circleRadius -= 10; }
                    ),
                    Color.LightBlue, Color.Gray
                ),
            };
            gui.AddRange(buttons);
        }

        static bool RedoAddObject(PixelLinesApp app)
        {
            if (app.lastObjectHandle < app.objects.Count)
            {
                // "Redo" the previous addition of an object.
                app.lastObjectHandle++;
                return true;
            }
            return false;
        }

        static bool UndoAddObject(PixelLinesApp app)
        {
            if (app.lastObjectHandle > 0)
            {
                // "Undo" the previous addition of an object.
                app.lastObjectHandle--;
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
                PixelLinesApp.UndoAddObject(this);
            }
            if (input.IsKeyCom((Keys.LeftControl, true), (Keys.Y, false)))
            {
                PixelLinesApp.RedoAddObject(this);
            }

            mouseOnScreen = screen.ToScreenPos(input.Mouse.position);
            if (input.Mouse.m2WasDown)
            {
                objects.RemoveRange(
                    lastObjectHandle,
                    objects.Count - lastObjectHandle
                );
                objects.Add(
                    new Circle(circleRadius, input.Mouse.position.ToVector2())
                );
                lastObjectHandle++;
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
                // Adding a new line invalidates the redo-stack, and starts a
                // new "branch" of actions.
                objects.RemoveRange(
                    lastObjectHandle,
                    objects.Count - lastObjectHandle
                );
                // User has released, so save the line.
                objects.Add(lineBeingDrawn);
                lastObjectHandle++;

                lineBeingDrawn = null;
            }
        }

        /// <summary>
        /// Re-"draw" the lines that user has already drawn on the screen.
        /// </summary>
        void UpdateObjects()
        {
            for (int i = 0; i < lastObjectHandle; i++)
            {
                var obj = objects[i];
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
