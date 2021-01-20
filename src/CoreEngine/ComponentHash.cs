using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEngine
{
    public class ComponentHash : IEquatable<ComponentHash>, IComparable<ComponentHash>
    {
        private readonly ReadOnlyMemory<byte> hash;

        public ComponentHash()
        {
            this.hash = Array.Empty<byte>();
        }

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
                list.AddRange(hashList[i].hash.Span.ToArray());
            }

            this.hash = new ReadOnlyMemory<byte>(list.ToArray());
        }

        public int CompareTo(ComponentHash? other)
        {
            if (other is not null)
            {
                if (this.hash.Length == other.hash.Length)
                {
                    for (var i = 0; i < this.hash.Length; i++)
                    {
                        if (this.hash.Span[i] != other.hash.Span[i])
                        {
                            return this.hash.Span[i].CompareTo(other.hash.Span[i]);
                        }
                    }
                }

                else
                {
                    return this.hash.Length.CompareTo(other.hash.Length);
                }
            }

            return 0;
        }

        public override bool Equals(object? obj)
        {
            if (obj is ComponentHash other)
            {
                return Equals(other);
            }

            return base.Equals(obj);
        }

        public bool Equals(ComponentHash? other)
        {
            return this.CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            var hash = 0;

            for (var i = 0; i < this.hash.Length; i++)
            {
                hash ^= this.hash.Span[i];
            }

            return hash;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < this.hash.Length; i++)
            {
                stringBuilder.Append($"{this.hash.Span[i]:X2}");
                if ((i % 4) == 3) stringBuilder.Append(' ');
            }

            return stringBuilder.ToString();
        }

        public static bool operator ==(ComponentHash layout1, ComponentHash layout2) 
        {
            if (layout1 is null || layout2 is null)
            {
                return false;
            }

            return layout1.Equals(layout2);
        }

        public static bool operator !=(ComponentHash layout1, ComponentHash layout2) 
        {
            if (layout1 is null || layout2 is null)
            {
                return false;
            }

            return !layout1.Equals(layout2);
        }

        public static bool operator <(ComponentHash layout1, ComponentHash layout2) 
        {
            if (layout1 is null || layout2 is null)
            {
                return false;
            }

            return layout1.CompareTo(layout2) < 0;
        }

        public static bool operator <=(ComponentHash layout1, ComponentHash layout2) 
        {
            if (layout1 is null || layout2 is null)
            {
                return false;
            }

            return layout1.CompareTo(layout2) <= 0;
        }

        public static bool operator >(ComponentHash layout1, ComponentHash layout2) 
        {
            if (layout1 is null || layout2 is null)
            {
                return false;
            }

            return layout1.CompareTo(layout2) > 0;
        }

        public static bool operator >=(ComponentHash layout1, ComponentHash layout2) 
        {
            if (layout1 is null || layout2 is null)
            {
                return false;
            }

            return layout1.CompareTo(layout2) >= 0;
        }
    }
}