using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Utility class for coroutine or async behavior.
    /// </summary>
    public static class Timer
    {
        /// <summary>
        /// Trigger an action after a set time using Coroutines. Is affected by Time.timeScale.
        /// </summary>
        public static Coroutine Schedule(this MonoBehaviour behavior, float seconds, Action action)
        {
            return behavior.StartCoroutine(Schedule(seconds, action));
        }

        private static IEnumerator Schedule(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        /// <summary>
        /// Trigger an action after a set time using Tasks. Is not affected by Time.timeScale.
        /// </summary>
        public static void ScheduleAsync(float seconds, Action action)
        {
            _ = ScheduleAsyncImpl(seconds, action);
        }

        private static async Task ScheduleAsyncImpl(float seconds, Action action)
        {
            int delayMs = (int)(seconds * 1000);
            await Task.Delay(delayMs);
            action();
        }
    }
}
