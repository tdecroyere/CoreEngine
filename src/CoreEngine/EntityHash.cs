using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CoreEngine
{
    public class EntityHash : IEquatable<EntityHash>, IComparable<EntityHash>
    {
        private byte[] hash;
        
        public EntityHash(Type type)
        {
            this.hash = GetHash(new SHA256Managed(), type.FullName);
        }

        public EntityHash(Type[] types)
        {
            var list = new List<byte>();

            for (var i = 0; i < types.Length; i++)
            {
                list.AddRange(GetHash(new SHA256Managed(), types[i].FullName));
            }

            this.hash = list.ToArray();
        }

        public int CompareTo(EntityHash? other)
        {
            return Equals(other) ? 0 : -1;
        }

        public override bool Equals(object? obj)
        {
            if (obj is EntityHash other)
            {
                return Equals(other);
            }

            return base.Equals(obj);
        }

        public bool Equals(EntityHash? other)
        {
            if (other is not null && this.hash.Length == other.hash.Length)
            {
                for (var i = 0; i < this.hash.Length; i++)
                {
                    if (this.hash[i] != other.hash[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
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

        public static bool operator ==(EntityHash layout1, EntityHash layout2) 
        {
            return layout1.Equals(layout2);
        }

        public static bool operator !=(EntityHash layout1, EntityHash layout2) 
        {
            return !layout1.Equals(layout2);
        }

        private static byte[] GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            return hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        }
    }
}