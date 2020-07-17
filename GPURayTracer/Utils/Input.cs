using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Utils
{
    using OpenTK.Input;

    // This is a slimmed down version of InputManager from
    // https://github.com/NullandKale/SimpleTopDownShooter/blob/master/CS162Final/Managers/InputManager.cs
    public class InputManager
    {
        // keyboard state storage, one for the current frame and one for the last frame
        private KeyboardState lastKeyState;
        public KeyboardState currentKeyState;

        public InputManager()
        {

        }

        public void Update()
        {
            // on update if the currentKeyState is not invalid set lastKeyState to the old currentKeyState
            if (currentKeyState != null)
            {
                lastKeyState = currentKeyState;
            }

            // update currentKeyState
            currentKeyState = Keyboard.GetState();
        }

        // keyboard state functions
        public bool IsKeyRising(Key k)
        {
            if (!IsKeystateValid())
            {
                return false;
            }
            else
            {
                return lastKeyState.IsKeyDown(k) && currentKeyState.IsKeyUp(k);
            }
        }

        public bool IsKeyFalling(Key k)
        {
            if (!IsKeystateValid())
            {
                return false;
            }
            else
            {
                return lastKeyState.IsKeyUp(k) && currentKeyState.IsKeyDown(k);
            }
        }

        public bool IsKeyHeld(Key k)
        {
            if (!IsKeystateValid())
            {
                return false;
            }
            else
            {
                return lastKeyState.IsKeyDown(k) && currentKeyState.IsKeyDown(k);
            }
        }

        // check that the keyboard state is valid | this might not be needed
        private bool IsKeystateValid()
        {
            return currentKeyState != null && lastKeyState != null;
        }
    }
}
