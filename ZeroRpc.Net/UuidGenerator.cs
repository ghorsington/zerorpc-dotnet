using System;
using System.Text;

namespace ZeroRpc.Net
{
    /// <summary>
    ///     A basic default UUID generator
    /// </summary>
    public static class UuidGenerator
    {
        private static readonly string uuidBase;
        private static ulong uuidCounter;

        static UuidGenerator()
        {
            uuidBase = Guid.NewGuid().ToString("N").Substring(0, 16);
        }

        /// <summary>
        ///     Generate next unique 16-byte UUID and format it as a 32-character hex string.
        /// </summary>
        /// <returns>A 32 character-long UUID that can be used to identify a message.</returns>
        public static string NextUuid()
        {
            ulong counter = uuidCounter++;
            string counterStr = counter.ToString("X");
            return uuidBase + new string('0', 16 - counterStr.Length) + counterStr;
        }

        /// <summary>
        ///     Generate next unique 16-byte UUID and encode it as a byte array.
        /// </summary>
        /// <returns>A 32-element byte array that contains an UUID that can be used to identify a message.</returns>
        public static byte[] NextUuidBytes()
        {
            return Encoding.ASCII.GetBytes(NextUuid());
        }
    }
}