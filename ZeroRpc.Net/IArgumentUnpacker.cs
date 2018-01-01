using System.Collections.Generic;
using MsgPack;

namespace ZeroRpc.Net
{
    /// <summary>
    ///     An argument unpacker that can deserialize a <see cref="MessagePackObject" />.
    /// </summary>
    /// <remarks>
    ///     Argument unpackers are used by both <see cref="Client" /> and <see cref="Server" /> to convert the incoming
    ///     event or method arguments into a more programmer-friendly form.
    ///     ZeroRpc.Net provides some default message unpackers in <see cref="ArgumentUnpackers" /> class.
    /// </remarks>
    public interface IArgumentUnpacker
    {
        /// <summary>
        ///     Deserialize provided Msgpack object into a native object.
        /// </summary>
        /// <param name="obj">Msgpack object to deserialize.</param>
        /// <returns>A deserialized object.</returns>
        object Unpack(MessagePackObject obj);
    }

    /// <summary>
    ///     Extension methods for <see cref="IArgumentUnpacker" />.
    /// </summary>
    public static class ArgumentUnpackerExtensions
    {
        /// <summary>
        ///     Unpacks an list of Msgpack objects.
        /// </summary>
        /// <param name="self">"This" parameter.</param>
        /// <param name="list">A list of Msgpack objects to unpack.</param>
        /// <returns>An array of unpacked objects.</returns>
        public static object[] Unpack(this IArgumentUnpacker self, IList<MessagePackObject> list)
        {
            object[] result = new object[list.Count];

            for (int i = 0; i < list.Count; i++)
                result[i] = self.Unpack(list[i]);

            return result;
        }
    }
}