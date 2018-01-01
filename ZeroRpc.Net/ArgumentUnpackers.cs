using System.Collections.Generic;
using System.Linq;
using MsgPack;

namespace ZeroRpc.Net
{
    /// <summary>
    ///     A collection of built-in unpackers.
    /// </summary>
    public static class ArgumentUnpackers
    {
        /// <summary>
        ///     A "raw" argument unpacker. Does not unpack anything; returns a <see cref="MessagePackObject" /> itself.
        /// </summary>
        public static readonly RawArgumentUnpacker Raw = new RawArgumentUnpacker();

        /// <summary>
        ///     A simple argument unpacker that can unpack all primitive types.
        ///     In addition, unpacks lists into arrays of primitives and object maps into
        ///     string to object <see cref="Dictionary{TKey,TValue}" />.
        ///     Objects in the lists and maps are unpacked recursively until the initial object is fully unpacket.
        /// </summary>
        public static readonly SimpleArgumentUnpacker Simple = new SimpleArgumentUnpacker();
    }

    public class RawArgumentUnpacker : IArgumentUnpacker
    {
        internal RawArgumentUnpacker() { }

        /// <inheritdoc />
        public object Unpack(MessagePackObject obj)
        {
            return obj;
        }
    }

    public class SimpleArgumentUnpacker : IArgumentUnpacker
    {
        internal SimpleArgumentUnpacker() { }

        /// <inheritdoc />
        public object Unpack(MessagePackObject obj)
        {
            if (obj.IsList)
            {
                IList<MessagePackObject> list = obj.AsList();
                return this.Unpack(list);
            }
            if (obj.IsMap)
            {
                MessagePackObjectDictionary map = obj.AsDictionary();
                return map.ToDictionary(k => k.Key.ToString(), k => Unpack(k.Value));
            }

            return obj.ToObject();
        }
    }
}