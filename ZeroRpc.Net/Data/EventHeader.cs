using MsgPack.Serialization;

namespace ZeroRpc.Net.Data
{
    internal class EventHeader
    {
        [MessagePackMember(1, Name = "message_id")]
        public object MessageId { get; set; }

        [MessagePackMember(2, Name = "response_to", NilImplication = NilImplication.MemberDefault)]
        public object ResponseTo { get; set; }

        [MessagePackMember(0, Name = "v")]
        public int Version { get; set; }
    }
}