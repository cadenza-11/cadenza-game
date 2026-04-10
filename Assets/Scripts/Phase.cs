using System;

namespace Cadenza
{
    [Serializable]
    public struct Phase : IEquatable<Phase>
    {
        public int Index;
        public string FMODMarkerName;

        public static bool operator ==(Phase s1, Phase s2) => s1.Equals(s2);

        public static bool operator !=(Phase s1, Phase s2) => !s1.Equals(s2);

        public readonly bool Equals(Phase other)
        {
            return
                this.Index == other.Index &&
                this.FMODMarkerName == other.FMODMarkerName;
        }

        public readonly override bool Equals(object o)
        {
            return o is Phase other && this.Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(this.Index, this.FMODMarkerName);
        }

        public readonly override string ToString()
        {
            return $"Phase (Index={this.Index}, FMODMarkerName='{this.FMODMarkerName}')";
        }
    }
}
