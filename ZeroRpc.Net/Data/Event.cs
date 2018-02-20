using System.Collections.Generic;
using MsgPack;

namespace ZeroRpc.Net.Data
{
    public class Event
    {
        public IList<MessagePackObject> Args { get; set; }
        public List<byte[]> Envelope { get; set; }
        public EventHeader Header { get; set; }
        public string Name { get; set; }
    }
}