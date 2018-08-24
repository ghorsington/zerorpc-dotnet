using System;
using System.Linq;
using ZeroRpc.Net.Data;

namespace ZeroRpc.Net.Util
{
    /// <summary>
    /// A UUID generator that creates a byte buffer.
    /// </summary>
    public class ByteUuidGen : IMessageIdGen
    {
        private readonly byte[] uuidBase;
        private ulong uuidCounter;

        /// <summary>
        /// Initializes the ID gen.
        /// </summary>
        public ByteUuidGen()
        {
            uuidBase = Guid.NewGuid().ToByteArray().Take(8).ToArray();
            uuidCounter = 0;
        }

        public object Next()
        {
            ulong counter = uuidCounter++;
            return uuidBase.Concat(BitConverter.GetBytes(counter)).ToArray();
        }
    }

    /// <summary>
    /// A UUID generator that creates a conventional string-based ID.
    /// </summary>
    public class StringUuidGen : IMessageIdGen
    {
        private readonly string uuidBase;
        private ulong uuidCounter;

        /// <summary>
        /// Initializes the ID gen.
        /// </summary>
        public StringUuidGen()
        {
            uuidBase = Guid.NewGuid().ToString("N").Substring(0, 16);
            uuidCounter = 0;
        }

        public object Next()
        {
            ulong counter = uuidCounter++;
            string counterStr = counter.ToString("X");
            return uuidBase + new string('0', 16 - counterStr.Length) + counterStr;
        }
    }
}