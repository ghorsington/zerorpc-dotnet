using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroRpc.Net.Core
{
    public class ChannelKeyComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            if (x == null && y == null)
                return true; //objects are equals if they are both NULL.

            if (x == null ^ y == null)
                return false; //objects are not equal if one of them is NULL but not the other.

            //If code passes here, both objects are not NULL

            if (x.GetType().Equals(y.GetType()))
            {
                switch (x)
                {
                    case string xs:
                        return xs == (string)y;
                    case byte[] xba:
                        byte[] yba = (byte[])y;
                        if (xba.Length != yba.Length)
                            return false;
                        else
                        {
                            for (int i = 0; i < xba.Length; i++)
                            {
                                if (xba[i] != yba[i])
                                    return false;
                            }
                            return true;
                        }
                    default:
                        throw new NotImplementedException(string.Format("Type {0} is not implemented in ChannelKeyComparer Equals method", x.GetType()));
                }
            }
            else
            {
                //type mismatch between the two objects.
                return false;
            }
        }

        public int GetHashCode(object obj)
        {
            switch (obj)
            {
                case string xs:
                    return xs.GetHashCode();
                case byte[] xba:
                    //This is a rather simple (but rather efficient) hash code computation base on 2 really simple prime numbers.
                    int result = 17;
                    for (int i = 0; i < xba.Length; i++)
                    {
                        unchecked
                        {
                            result = result * 23 + xba[i];
                        }
                    }
                    return result;
                default:
                    throw new NotImplementedException(string.Format("Type {0} is not implemented in ChannelKeyComparer GetHashCode method", obj.GetType()));
            }
        }
    }
}
