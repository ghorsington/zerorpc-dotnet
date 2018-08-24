using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroRpc.Net
{
    public class MessageIdComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            if (x == null && y == null)
                return true; //objects are equals if they are both NULL.

            if ((x == null) ^ (y == null))
                return false; //objects are not equal if one of them is NULL but not the other.

            //type mismatch between the two objects.
            if (x.GetType() != y.GetType())
                return false;

            switch (x)
            {
                case string xs: return xs == (string) y;
                case byte[] xba: return xba.SequenceEqual((byte[]) y);
                default: throw new NotImplementedException($"Type {x.GetType()} is not implemented in MessageIdComparer Equals method");
            }
        }

        public int GetHashCode(object obj)
        {
            switch (obj)
            {
                case string xs: return xs.GetHashCode();
                case byte[] xba:
                    //This is a rather simple (but rather efficient) hash code computation base on 2 really simple prime numbers.
                    return xba.Aggregate(17, (current, t) => current * 23 + t);
                default:
                    throw new NotImplementedException($"Type {obj.GetType()} is not implemented in MessageIdComparer GetHashCode method");
            }
        }
    }
}