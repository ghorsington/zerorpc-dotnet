using System;
using System.Collections.Generic;
using ZeroRpc.Net.Core;
using ZeroRpc.Net.Util;

namespace ZeroRpc.Net
{
    public static class Config
    {
        public static Func<object> UuidGenerator { get; set; } = Net.UuidGenerator.NextUuidBytes;
        public static IEqualityComparer<object> MessageIdComparer { get; set; } = new MessageIdComparer();
    }
}
