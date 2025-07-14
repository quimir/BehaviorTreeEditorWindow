using System;
using System.Collections.Generic;
using ExTools.Singleton;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Script.Tool.Timer;
using UnityEngine;
using UnityEngine.Events;

namespace ExTools.Time
{
    public class TimerManager : SingletonWithLazy<TimerManager>
    {
        private readonly List<Timer> timer_list_ = new(10);

        private float time_;

        public void ClearTime()
        {
            timer_list_.Clear();
        }

        /// <summary>
        /// Updates the TimerManager by processing and executing any timers whose scheduled time has elapsed.
        /// </summary>
        /// <remarks>
        /// This method iterates through the active timers, checks if their specified execution time has been reached,
        /// and invokes their associated actions. If an exception is encountered during the execution of a timer action,
        /// it logs a warning with the exception details and continues processing remaining timers.
        /// </remarks>
        /// <exception cref="Exception">Logs any exceptions thrown by a timer action but does not rethrow them.</exception>
        public void Update()
        {
            time_ = UnityEngine.Time.unscaledTime;

            var i = 0;

            while (i<timer_list_.Count)
            {
                if (timer_list_[i].Time>time_)
                {
                    break;
                }

                Timer timer = timer_list_[i];
                var action = timer.Action;
                timer_list_.RemoveAt(i);
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(new LogSpaceNode("TimerManager"),new LogEntry(LogLevel.kWarning,
                        $"Timer:{timer.Uuid} Target:{action?.Target}\nException:{e}"));
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Adds a timer to be executed after a specified delay.
        /// </summary>
        /// <param name="delay">The delay time in seconds before the timer action is executed. Must be a non-negative value.</param>
        /// <param name="action">The action to be executed when the timer elapses. Cannot be null.</param>
        /// <returns>Returns a unique identifier (UUID) for the created timer.</returns>
        /// <exception cref="ArgumentException">Thrown if the delay is negative or the action is null.</exception>
        public int AddTimer(float delay, UnityAction action)
        {
            if (delay < 0) throw new ArgumentException("delay不能为负数", nameof(delay));
            if (action == null) throw new ArgumentException("action 不能为null", nameof(action));

            var timer = new Timer(time_ + delay, action);

            if (timer_list_.Count==0)
            {
                CheckCapacity();
                timer_list_.Add(timer);
                
                return timer.Uuid;
            }

            for (int i = timer_list_.Count-1; i >=0; i--)
            {
                if (timer.Time>=timer_list_[i].Time)
                {
                    timer_list_.Insert(i+1,timer);
                    return timer.Uuid;
                }
            }
            
            CheckCapacity();
            timer_list_.Insert(0,timer);
            return timer.Uuid;
        }

        /// <summary>
        /// Removes the timer with the specified unique identifier from the list of active timers.
        /// </summary>
        /// <param name="uuid">The unique identifier of the timer to be removed.</param>
        /// <remarks>
        /// This method searches for a timer with the specified identifier in the list of active timers.
        /// If found, it removes the timer from the list. If no timer with the given identifier exists, no action is taken.
        /// </remarks>
        public void RemoveTimer(int uuid)
        {
            timer_list_.RemoveAt(timer_list_.FindIndex(t => t.Uuid == uuid));
        }

        /// <summary>
        /// Ensures that the internal timer list has enough capacity to accommodate additional timers without frequent allocations.
        /// </summary>
        /// <remarks>
        /// This method dynamically adjusts the capacity of the timer list to optimize memory usage and performance.
        /// If the current capacity is reached, it calculates a new capacity by doubling the existing one and adding 1.
        /// The method sets the new capacity to the timer list to avoid frequent resizing operations during timer additions.
        /// </remarks>
        private void CheckCapacity()
        {
            if (timer_list_.Count == timer_list_.Capacity)
            {
                var current_capacity = timer_list_.Capacity;
                int new_capacity;

                if (current_capacity == 0)
                    new_capacity = 10;
                else
                    new_capacity = current_capacity * 2 + 1;

                timer_list_.Capacity = new_capacity;
            }
        }

        protected override void InitializationInternal()
        {
            time_ = 0;
            ClearTime();
        }
    }
}