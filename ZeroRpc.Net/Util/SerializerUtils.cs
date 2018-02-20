using System;
using System.Collections.Generic;
using MsgPack;
using MsgPack.Serialization;
using NetMQ;
using ZeroRpc.Net.Data;

namespace ZeroRpc.Net.Util
{
    internal static class SerializerUtils
    {
        private static readonly MessagePackSerializer<object[]> serializer;

        static SerializerUtils()
        {
            SerializationContext.Default.SerializationMethod = SerializationMethod.Map;
            serializer = MessagePackSerializer.Get<object[]>();
        }

        public static IList<MessagePackObject> Serialize(object[] args) =>
                serializer.ToMessagePackObject(args).AsList();

        public static NetMQMessage Serialize(Event evt)
        {
            NetMQMessage message = new NetMQMessage();
            object[] payload = {evt.Header, evt.Name, evt.Args};

            if (evt.Envelope != null)
                foreach (byte[] frame in evt.Envelope)
                    message.Append(frame);

            message.AppendEmptyFrame();
            message.Append(serializer.PackSingleObject(payload));
            return message;
        }

        public static Event Deserialize(List<byte[]> envelope, byte[] data)
        {
            MessagePackObject payloadData = Unpacking.UnpackObject(data).Value;
            IList<MessagePackObject> parts;
            MessagePackObjectDictionary header;

            if (!payloadData.IsArray || (parts = payloadData.AsList()).Count != 3)
                throw new Exception("Expected array of size 3");
            if (!parts[0].IsMap || !(header = parts[0].AsDictionary()).ContainsKey("message_id"))
                throw new Exception("Bad header");
            if (parts[1].AsString() == null)
                throw new Exception("Bad name");

            EventHeader headerObj = new EventHeader
            {
                    Version = header["v"].AsInt32(),
                    MessageId = ProcessUuid(header, "message_id"),
                    ResponseTo = ProcessUuid(header, "response_to")
            };

            Event msg = new Event
            {
                    Envelope = envelope,
                    Header = headerObj,
                    Name = parts[1].AsString(),
                    Args = parts[2].AsList()
            };

            return msg;
        }

        private static object ProcessUuid(MessagePackObjectDictionary dic, string value) =>
                !dic.TryGetValue(value, out MessagePackObject uuid) ? string.Empty : uuid.ToObject();
    }
}