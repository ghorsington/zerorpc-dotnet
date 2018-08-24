using System;
using ZeroRpc.Net.Data;

namespace ZeroRpc.Net.Core
{
    internal class ClientChannel : Channel
    {
        private bool isFresh;

        public ClientChannel(object id, SocketBase socket, int capacity, TimeSpan hearbeatInterval) : base(id,
                                                                                                           null,
                                                                                                           socket,
                                                                                                           capacity,
                                                                                                           hearbeatInterval)
        {
            isFresh = true;
        }

        protected override EventHeader CreateHeader()
        {
            if (!isFresh)
                return base.CreateHeader();
            isFresh = false;
            return new EventHeader {Version = PROTOCOL_VERSION, MessageId = Id, ResponseTo = null};
        }
    }
}