using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerProxy.Utility
{
    // Based on: https://stackoverflow.com/questions/14314223/an-asynchronous-counter-which-can-be-awaited-on
    /// <summary>
    /// Increments and Decrements a count in a thread-safe manner. 
    /// Provides "WaitAsync" method whose Task will complete after ReferenceCounter.Complete() is called and the count has returned to 0.
    /// </summary>
    public class ReferenceCounter
    {
        private object _lock = new object();

        private readonly TaskCompletionSource _taskCompletionSource = new TaskCompletionSource();

        private bool _isCompleted;
        private int _count = 0;

        /// <summary>
        /// Tries to increment the count by 1. 
        /// Returns True if successful. 
        /// Returns False if the counter is finished accepting increments.
        /// </summary>
        /// <returns></returns>
        public bool TryIncrement(out int count)
        {
            lock (_lock)
            {
                // If the counter is completed, exit
                if (_isCompleted)
                {
                    count = default;
                    return false;
                }

                // Otherwise, increment
                _count++;

                count = _count;
                return true;
            }
        }

        /// <summary>
        /// Decrements the count by 1.
        /// Returns the new count.
        /// </summary>
        /// <returns></returns>
        public int Decrement()
        {
            lock (_lock)
            {
                // Decrement
                _count--;

                // Check to see if we are completed and at 0
                SetTaskCompletionSourceIfNeeded();

                return _count;
            }
        }

        /// <summary>
        /// Sets the reference counter to stop accepting increments.
        /// </summary>
        public void Complete()
        {
            lock (_lock)
            {
                _isCompleted = true;

                // Check to see if we are already at 0
                SetTaskCompletionSourceIfNeeded();
            }
        }

        /// <summary>
        /// Returns a task that completes when the reference counter has returned to 0
        /// </summary>
        /// <returns></returns>
        public Task WaitAsync() => _taskCompletionSource.Task;

        /// <summary>
        /// Sets the TaskCompletionSource if necessary.
        /// 
        /// *** Must be called within a lock statement. ***
        /// </summary>
        private void SetTaskCompletionSourceIfNeeded()
        {
            // If we are completed and at 0 count, set the TaskCompletionSource
            if (_isCompleted && _count == 0)
            {
                _taskCompletionSource.SetResult();
            }
        }
    }
}
