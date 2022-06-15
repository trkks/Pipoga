using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Pipoga
{
    public class Input
    {
        // This was found by printing and then observing (using System.Linq):
        //   Enum.GetValues(typeof(Keys)).Cast<uint>().Max()
        const int KEYS_ENUM_MAX = 254 + 1; // Starts from zero (0)
        public bool[] released;
        KeyboardState keyboardState;
        MouseState mouseState;

        // Forms a vector from WASD-keys (usually for character movement)
        public Vector2 WASD()
        {
            float x = this.keyboardState[Keys.A] == KeyState.Down
                      ? -1f : 0f;
            x += this.keyboardState[Keys.D] == KeyState.Down
                 ? 1f : 0f;

            float y = this.keyboardState[Keys.W] == KeyState.Down
                      ? -1f : 0f;
            y += this.keyboardState[Keys.S] == KeyState.Down
                 ? 1f : 0f;

            return NormalizedDir(x, y);
        }

        // Forms a vector from arrow keys (pretty much same as WASD)
        public Vector2 Arrows()
        {
            float x = this.keyboardState[Keys.Left] == KeyState.Down
                      ? -1f : 0f;
            x += this.keyboardState[Keys.Right] == KeyState.Down
                 ? 1f : 0f;

            float y = this.keyboardState[Keys.Up] == KeyState.Down
                      ? -1f : 0f;
            y += this.keyboardState[Keys.Down] == KeyState.Down
                 ? 1f : 0f;

            return NormalizedDir(x, y);
        }

        /// <summary>
        /// Use to ensure that the unit-components form at most a unit vector
        /// </summary>
        /// <param name="x">Between -1 and 1</param>
        /// <param name="y">Between -1 and 1</param>
        /// <returns>Normalized vector</returns>
        Vector2 NormalizedDir(float x, float y)
        {
            // TODO Is it too much to calculate vector length every frame?
            var v = new Vector2(x, y);
            if (v.Length() > 0)
            {
                v.Normalize();
                return v;
            }
            return v;
        }

        public MouseButtonState Mouse1State { get; private set; }
        public MouseButtonState Mouse2State { get; private set; }

        public Point MousePosition => this.mouseState.Position;

        public Input()
        {
            released = new bool[Input.KEYS_ENUM_MAX + 2];
            Array.Fill(released, true);
        }

        public void Update()
        {
            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();

            Mouse1State =  new MouseButtonState(
                isDown: mouseState.LeftButton == ButtonState.Pressed,
                wasDown: WasInputDown(
                    KEYS_ENUM_MAX,
                    mouseState.LeftButton == ButtonState.Pressed
                )
            );

            Mouse2State =  new MouseButtonState(
                isDown: mouseState.RightButton == ButtonState.Pressed,
                wasDown: WasInputDown(
                    KEYS_ENUM_MAX + 1,
                    mouseState.RightButton == ButtonState.Pressed
                )
            );

        }

        /// <summary>
        /// Check if key was pressed but not still being pressed ie. pressing
        /// and holding a key only returns true once.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>
        ///   true if `key` was down after previously being up
        /// </returns>
        public bool WasKeyDown(Keys key)
        {
            return WasInputDown((uint)key, keyboardState.IsKeyDown(key));
        }

        /// <summary>
        /// Generalizes checking if input was pressed but not still being
        /// pressed to any possible input (currently keyboard and mouse).
        /// </summary>
        /// <param name="input">The code of the input to check</param>
        /// <param name="inputIsDown">
        ///   If the input is currently being pressed
        /// </param>
        /// <returns>
        ///   true if `input` was down after previously being up
        /// </returns>
        bool WasInputDown(uint input, bool inputIsDown)
        {
            // TODO If called multiple times on an update, only the first query
            // is correct (the "was-down" -state changes after first call).
            if (inputIsDown)
            {
                if (released[input])
                {
                    released[input] = false;
                    return true;
                }
            }
            else
            {
                released[input] = true;
            }
            return false;
        }

        public bool IsKeyDown(Keys key)
        {
            return keyboardState.IsKeyDown(key);
        }
    }

    public struct MouseButtonState
    {
        public readonly bool wasDown;
        public readonly bool isDown;

        public MouseButtonState(bool wasDown, bool isDown)
        {
            this.wasDown = wasDown;
            this.isDown = isDown;
        }
    }
}
