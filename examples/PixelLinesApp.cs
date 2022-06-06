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

        Point lineDrawStart;
        Line lineBeingDrawn;

        public PixelLinesApp(string[] args)
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

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

            // This has to be set before drawing.
            screen.PixelTexture = Content.Load<Texture2D>("pixel");
        }

        protected override void Update(GameTime gameTime)
        {
            input.Update();

            if (input.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // Clear the screen.
            screen.Clear();

            Point mouseOnScreen = screen.ToGridCoords(input.MousePosition);

            if (input.WasMouse1Down)
            {
                lineDrawStart = mouseOnScreen;
            }
            if (input.IsMouse1Down)
            {
                var end = mouseOnScreen;
                lineBeingDrawn = new Line(
                    lineDrawStart.ToVector2(),
                    end.ToVector2()
                );
                screen.PlotLine(lineBeingDrawn, Color.White);
            }
            else if (lineBeingDrawn != null)
            {
                // User has released, so save the line.
                lines.Add(lineBeingDrawn);
                lineBeingDrawn = null;
            }

            // Draw the lines that user has drawn.
            foreach (var line in lines)
            {
                screen.PlotLine(line, Color.White);
            }

            base.Update(gameTime);
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
