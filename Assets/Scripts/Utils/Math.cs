using Unity.Mathematics;

namespace Cadenza.Utils
{
    public static class Math
    {
        /// <summary>
        /// Loops the value t, so that it is never larger than length and never smaller than 0.
        /// </summary>
        public static double Repeat(double t, double length)
        {
            return math.clamp(t - math.floor(t / length) * length, 0f, length);
        }

        /// <summary>
        /// Applies the normal distribution function to the value t with some constant standard deviation.
        /// </summary>
        /// <param name="t">The input value to the normal distribution</param>
        /// <param name="stddev">The desired standard deviation of the normal distributon</param>
        public static double NormalDist(double t, double stddev)
        {
            return math.exp(-(t * t) / (2f * stddev * stddev));
        }

        /// <summary>
        /// Calculates the next exponentially-weighted moving average of a previous
        /// average value and the next data point.
        /// </summary>
        /// <param name="avg">The previous average</param>
        /// <param name="next">The next data point to aggregate with the average</param>
        /// <param name="alpha">A value from 0-1 of how heavily the next data point should affect the average</param>
        /// <returns></returns>
        public static double EWMA(double avg, double next, double alpha)
        {
            return (next * alpha) + (avg * (1 - alpha));
        }
    }
}
