using System;
using ZeroRpc.Net.Data;
using ZeroRpc.Net.Util;

namespace ZeroRpc.Net.Core
{
    internal class ClientChannel : Channel
    {
        private bool isFresh;

        public ClientChannel(SocketBase socket, int capacity, TimeSpan hearbeatInterval) : base(UuidGen.ComputeUuid(),
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