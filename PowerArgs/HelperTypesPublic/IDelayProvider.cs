﻿using System;
using System.Threading.Tasks;

namespace PowerArgs
{
    /// <summary>
    /// An abstraction for time delay so that we can have a consistent delay API across wall clock time and Time simulation time
    /// </summary>
    public interface IDelayProvider
    {
        /// <summary>
        /// Delays for the given time
        /// </summary>
        /// <param name="ms">milliseconds</param>
        /// <returns>an async task</returns>
        Task DelayAsync(double ms);

        /// <summary>
        /// Delays for the given time
        /// </summary>
        /// <param name="timeout">the delay time</param>
        /// <returns>an async task</returns>
        Task DelayAsync(TimeSpan timeout);

        /// <summary>
        /// Delays until the given event fires
        /// </summary>
        /// <param name="ev">the event to wait on</param>
        /// <param name="timeout">the max time to wait</param>
        /// <param name="evalFrequency">how frequently to check</param>
        /// <returns>an async task</returns>
        Task DelayAsync(Event ev, TimeSpan? timeout = null, TimeSpan? evalFrequency = null);

        /// <summary>
        /// Delays until the given condition is true
        /// </summary>
        /// <param name="condition">the condition</param>
        /// <param name="timeout">the max time to wait</param>
        /// <param name="evalFrequency">how frequently to evaluate the condition</param>
        /// <returns>an async task</returns>
        Task DelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null);

        /// <summary>
        /// Try to delay until the given condition is true
        /// </summary>
        /// <param name="condition">the condition</param>
        /// <param name="timeout">the max time to wait</param>
        /// <param name="evalFrequency">how frequently to evaluate the condition</param>
        /// <returns>true if the condition was true, false if we timed out</returns>
        Task<bool> TryDelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null);

        /// <summary>
        /// Yields immidiately
        /// </summary>
        /// <returns>an async task</returns>
        Task YieldAsync();
    }

    /// <summary>
    /// An implementation of IDelayProvider that is based on wall clock time
    /// </summary>
    public class WallClockDelayProvider : IDelayProvider
    {
        /// <summary>
        /// Delays for the given time
        /// </summary>
        /// <param name="ms">milliseconds</param>
        /// <returns>an async task</returns>
        public Task DelayAsync(double ms) => Task.Delay(TimeSpan.FromMilliseconds(ms));

        /// <summary>
        /// Delays for the given time
        /// </summary>
        /// <param name="timeout">the delay time</param>
        /// <returns>an async task</returns>
        public Task DelayAsync(TimeSpan timeout) => Task.Delay(timeout);

        /// <summary>
        /// Delays until the given event fires
        /// </summary>
        /// <param name="ev">the event to wait on</param>
        /// <param name="timeout">the max time to wait</param>
        /// <param name="evalFrequency">how frequently to check</param>
        /// <returns>an async task</returns>
        public async Task DelayAsync(Event ev, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            var fired = false;

            ev.SubscribeOnce(() =>
            {
                fired = true;
            });

            await DelayAsync(() => fired, timeout, evalFrequency);
        }

        /// <summary>
        /// Delays until the given condition is true
        /// </summary>
        /// <param name="condition">the condition</param>
        /// <param name="timeout">the max time to wait</param>
        /// <param name="evalFrequency">how frequently to evaluate the condition</param>
        /// <returns>an async task</returns>
        public async Task DelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            if (await TryDelayAsync(condition, timeout, evalFrequency) == false)
            {
                throw new TimeoutException("Timed out awaiting delay condition");
            }
        }

        /// <summary>
        /// Try to delay until the given condition is true
        /// </summary>
        /// <param name="condition">the condition</param>
        /// <param name="timeout">the max time to wait</param>
        /// <param name="evalFrequency">how frequently to evaluate the condition</param>
        /// <returns>true if the condition was true, false if we timed out</returns>
        public async Task<bool> TryDelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            var startTime = DateTime.UtcNow;
            var lastEval = DateTime.MinValue;
            while (true)
            {
                if (evalFrequency.HasValue && DateTime.UtcNow - lastEval < evalFrequency.Value)
                {
                    await Task.Yield();
                }
                else if (condition())
                {
                    return true;
                }
                else if (timeout.HasValue && DateTime.UtcNow - startTime >= timeout.Value)
                {
                    return false;
                }
                else
                {
                    lastEval = DateTime.UtcNow;
                    await Task.Yield();
                }
            }
        }

        /// <summary>
        /// Yields immidiately
        /// </summary>
        /// <returns>an async task</returns>
        public async Task YieldAsync() => await Task.Yield();
    }
}
