using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pipoga
{
    // TODO Implement IObservable for keyboard (and generally on HW-controls).
    public class Input : IObservable<Input>
    {
        // This was found by printing and then observing (using System.Linq):
        //   Enum.GetValues(typeof(Keys)).Cast<uint>().Max()
        const int KEYS_ENUM_MAX = 254 + 1; // Starts from zero (0)
        public bool[] released;

        KeyboardState _keyboardState;
        List<IObserver<Input>> _observers;


        // Forms a vector from WASD-keys (usually for character movement)
        public Vector2 WASD()
        {
            float x = this._keyboardState[Keys.A] == KeyState.Down
                      ? -1f : 0f;
            x += this._keyboardState[Keys.D] == KeyState.Down
                 ? 1f : 0f;

            float y = this._keyboardState[Keys.W] == KeyState.Down
                      ? -1f : 0f;
            y += this._keyboardState[Keys.S] == KeyState.Down
                 ? 1f : 0f;

            return NormalizedDir(x, y);
        }

        // Forms a vector from arrow keys (pretty much same as WASD)
        public Vector2 Arrows()
        {
            float x = this._keyboardState[Keys.Left] == KeyState.Down
                      ? -1f : 0f;
            x += this._keyboardState[Keys.Right] == KeyState.Down
                 ? 1f : 0f;

            float y = this._keyboardState[Keys.Up] == KeyState.Down
                      ? -1f : 0f;
            y += this._keyboardState[Keys.Down] == KeyState.Down
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

        public Pipoga.MouseState Mouse { get; private set; }

        public Input()
        {
            released = new bool[Input.KEYS_ENUM_MAX + 2];
            Array.Fill(released, true);
            _observers = new List<IObserver<Input>>(0xff);
        }

        public void Update()
        {
            _keyboardState = Keyboard.GetState();

            var mouseState =
                Microsoft.Xna.Framework.Input.Mouse.GetState();
            Mouse = new MouseState(
                position: mouseState.Position,

                m1IsDown: mouseState.LeftButton == ButtonState.Pressed,
                m1WasDown: WasInputDown(
                    KEYS_ENUM_MAX,
                    mouseState.LeftButton == ButtonState.Pressed
                ),

                m2IsDown: mouseState.RightButton == ButtonState.Pressed,
                m2WasDown: WasInputDown(
                    KEYS_ENUM_MAX + 1,
                    mouseState.RightButton == ButtonState.Pressed
                )
            );

            NotifyObservers();
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
            return WasInputDown((uint)key, _keyboardState.IsKeyDown(key));
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
            return _keyboardState.IsKeyDown(key);
        }

        /// <summary>
        /// Check for the current states of multiple (TODO? ordered) keys.
        /// </summary>
        /// <param name="Keys">
        /// The keys to check for. The second item (bool) marks the "type" of
        /// state to check for with true meaning "IsDown" and false meaning
        /// "WasDown".
        /// </param>
        /// <returns>True, if the keys are in their required state.</returns>
        public bool IsKeyCom(params (Keys, bool)[] keys)
        {
            bool result = true;
            foreach (var (key, isDown) in keys)
            {
                result &= isDown ? IsKeyDown(key) : WasKeyDown(key);
            }
            return result;
        }

        private void NotifyObservers()
        {
            foreach (var observer in _observers)
            {
                observer.OnNext(this);
            }
        }

        public virtual IDisposable Subscribe(IObserver<Input> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
            return new Unsubscriber(_observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<Input>> observers;
            private IObserver<Input> observer;

            public Unsubscriber(
                List<IObserver<Input>> observers,
                IObserver<Input> observer
            )
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                if (observer != null && observers.Contains(observer))
                {
                    observers.Remove(observer);
                }
            }
        }
    }

    public struct MouseState
    {
        public readonly Point position;
        public readonly bool m1WasDown;
        public readonly bool m1IsDown;
        public readonly bool m2WasDown;
        public readonly bool m2IsDown;

        public MouseState(
            Point position,
            bool m1WasDown, bool m1IsDown,
            bool m2WasDown, bool m2IsDown
        )
        {
            this.position = position;
            this.m1WasDown = m1WasDown;
            this.m1IsDown  = m1IsDown;
            this.m2WasDown = m2WasDown;
            this.m2IsDown  = m2IsDown;
        }
    }
}
