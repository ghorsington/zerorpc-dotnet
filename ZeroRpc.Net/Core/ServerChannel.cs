using System;
using ZeroRpc.Net.Data;

namespace ZeroRpc.Net.Core
{
    internal class ServerChannel : Channel
    {
        public ServerChannel(Event sourceEvent, SocketBase socket, int capacity, TimeSpan heartbeatInterval) :
                base(sourceEvent.Header.MessageId, sourceEvent.Envelope, socket, capacity, heartbeatInterval) { }
    }
}