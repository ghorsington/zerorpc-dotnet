using System;
using System.Collections.Generic;

namespace ZeroRpc.Net
{
    /// <summary>
    /// Global configuration of the clients and the servers
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// The UUID generator function used when generating a new message.
        ///
        /// By default, zerorpc-dotnet uses <see cref="Net.UuidGenerator.NextUuidBytes"/>; that is the UUID will be encoded as a byte array. 
        /// </summary>
        /// <remarks>
        /// While zerorpc does allow any type to be a valid ID, zerorpc-dotnet provides two main versions available via <see cref="ZeroRpc.Net.UuidGenerator"/>:
        /// a string generator (used by e.g. zerorpc-node) and byte generator (used by e.g. zerorpc-python).
        /// </remarks>
        public static Func<object> UuidGenerator { get; set; } = Net.UuidGenerator.NextUuidBytes;

        /// <summary>
        /// An equality comparer used to compare between message IDs to map messages to correct channels.
        ///
        /// By default uses <see cref="MessageIdComparer"/>
        /// </summary>
        /// <remarks>
        /// By itself, zerorpc does not enforce any strict rules on how a message ID should be structured, as long
        /// as the type is preserved when communicating between endpoints. Thus it is fully possible to have a client that
        /// sends message IDs as a string and a server that sends its message IDs as a byte array.
        ///
        /// Thus the developer who wishes to change the equality comparer must accomodate for the message ids being of different types.
        /// </remarks>
        public static IEqualityComparer<object> MessageIdComparer { get; set; } = new MessageIdComparer();
    }
}
