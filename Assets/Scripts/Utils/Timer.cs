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
        public static Coroutine Schedule(this MonoBehaviour behavior, float seconds, Action action)
        {
            return behavior.StartCoroutine(Schedule(seconds, action));
        }

        public static IEnumerator Schedule(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        public static async Task ScheduleAsync(float seconds, Action action)
        {
            int delayMs = (int)(seconds * 1000);
            await Task.Delay(delayMs);
            action();
        }
    }
}
