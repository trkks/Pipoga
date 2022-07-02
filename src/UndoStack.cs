using System;
using System.Linq;
using System.Collections.Generic;

namespace Pipoga
{
    public class UndoStack<T>
    {
        List<T> stack;
        // Handle to the top of the stack.
        int stackPointer;

        public UndoStack(int size)
        {
            stack = new List<T>(size);
            stackPointer = 0;
        }

        public void Push(T obj)
        {
            // Adding a new element on the stack invalidates the current
            // redo-stack (the elements beyond the top "stack pointer"), and
            // starts a new "branch" of elements.
            stack.RemoveRange(
                stackPointer,
                stack.Count - stackPointer
            );

            stack.Add(obj);
            stackPointer++;
        }

        public bool Redo()
        {
            if (stackPointer < stack.Count)
            {
                // "Redo" the previous addition of an object.
                stackPointer++;
                return true;
            }
            return false;
        }

        public bool Undo()
        {
            if (stackPointer > 0)
            {
                // "Undo" the previous addition of an object.
                stackPointer--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// C# is quirky and this method apparently is enough to allow using in
        /// foreach-statements...
        /// </summary>
        /// <returns>
        /// Iterator over the elements, that are below the stack pointer
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < stackPointer; i++)
            {
                yield return stack[i];
            }
        }
    }
}
