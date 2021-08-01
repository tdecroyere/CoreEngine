namespace CoreEngine
{
    // TODO: Store the data in a continuous storage?
    public readonly record struct ComponentHash : IEquatable<ComponentHash>, IComparable<ComponentHash>
    {
        private readonly byte[] hash;

        public ComponentHash(byte[] hash)
        {
            this.hash = hash;
        }

        public ComponentHash(ReadOnlySpan<ComponentHash> hashList)
        {
            // TODO: Perf Issue! Do something better here

            var list = new List<byte>();

            for (var i = 0; i < hashList.Length; i++)
            {
                list.AddRange(hashList[i].hash);
            }

            this.hash = list.ToArray();
        }

        public int CompareTo(ComponentHash other)
        {
            if (this.hash.Length == other.hash.Length)
            {
                return this.hash.AsSpan().SequenceCompareTo(other.hash);
            }

            return this.hash.Length.CompareTo(other.hash.Length);
        }

        public bool Equals(ComponentHash other)
        {
            return this.hash.AsSpan().SequenceEqual(other.hash);
        }

        public override int GetHashCode()
        {
            var hash = 0;

            for (var i = 0; i < this.hash.Length; i++)
            {
                hash ^= this.hash[i];
            }

            return hash;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < this.hash.Length; i++)
            {
                stringBuilder.Append($"{this.hash[i]:X2}");
                if ((i % 4) == 3) stringBuilder.Append(' ');
            }

            return stringBuilder.ToString();
        }

        public byte[] ToArray()
        {
            return this.hash;
        }

        public static bool operator <(ComponentHash layout1, ComponentHash layout2) 
        {
            return layout1.CompareTo(layout2) < 0;
        }

        public static bool operator <=(ComponentHash layout1, ComponentHash layout2) 
        {
            return layout1.CompareTo(layout2) <= 0;
        }

        public static bool operator >(ComponentHash layout1, ComponentHash layout2) 
        {
            return layout1.CompareTo(layout2) > 0;
        }

        public static bool operator >=(ComponentHash layout1, ComponentHash layout2) 
        {
            return layout1.CompareTo(layout2) >= 0;
        }
    }
}