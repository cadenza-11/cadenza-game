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
        public static double NormalDist(double t, double stddev)
        {
            return math.exp(-(t * t) / (2f * stddev * stddev));
        }
    }
}
