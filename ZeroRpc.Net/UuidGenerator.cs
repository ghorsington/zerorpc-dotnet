using System;
using System.Text;

namespace ZeroRpc.Net
{
    /// <summary>
    /// A basic default UUID generator
    /// </summary>
    public static class UuidGenerator
    {
        private static readonly string uuidBase;
        private static ulong uuidCounter;

        static UuidGenerator()
        {
            uuidBase = Guid.NewGuid().ToString("N").Substring(0, 16);
        }

        public static string NextUuid()
        {
            ulong counter = uuidCounter++;
            string counterStr = counter.ToString("X");
            return uuidBase + new string('0', 16 - counterStr.Length) + counterStr;
        }

        public static byte[] NextUuidBytes() => Encoding.ASCII.GetBytes(NextUuid());
    }
}