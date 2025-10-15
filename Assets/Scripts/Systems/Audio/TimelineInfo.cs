
using System.Runtime.InteropServices;

namespace Cadenza
{
    /// <summary>
    /// A wrapper class for FMOD's <seealso cref="FMOD.Studio.TIMELINE_BEAT_PROPERTIES"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TimelineInfo
    {
        /// <summary>
        /// The beat number at the current position (unit-less)
        /// </summary>
        public int currentBeat = 0;

        /// <summary>
        /// The bar number at the current position (unit-less)
        /// </summary>
        public int currentBar = 0;

        /// <summary>
        /// The track-local timestamp of the most recently played beat, in milliseconds.
        /// </summary>
        public int beatPosition = 0;

        /// <summary>
        /// The current tempo value, in beats per minute
        /// </summary>
        public float currentTempo = 0;

        /// <summary>
        /// The previous tempo value, in beats per minute
        /// </summary>
        public float previousTempo = 0;

        /// <summary>
        /// The current track-local timestamp, in milliseconds
        /// </summary>
        public int currentPosition = 0;

        /// <summary>
        /// Length of the current track, in milliseconds
        /// </summary>
        public int trackLength = 0;

        /// <summary>
        /// Name of the most recently passed timeline marker
        /// </summary>
        public FMOD.StringWrapper lastMarkerName = new FMOD.StringWrapper();
    }
}
