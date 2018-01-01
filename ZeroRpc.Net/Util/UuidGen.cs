using System;

namespace ZeroRpc.Net.Util
{
    internal static class UuidGen
    {
        private static readonly string uuidBase;
        private static ulong uuidCounter;

        static UuidGen()
        {
            uuidBase = Guid.NewGuid().ToString().Substring(0, 24);
        }

        public static string ComputeUuid()
        {
            ulong counter = uuidCounter++;
            if (uuidCounter > 0xFFFFFFFFFFFF)
                uuidCounter = 0;
            string counterStr = counter.ToString("X");
            return uuidBase + new string('0', 12 - counterStr.Length) + counterStr;
        }
    }
}