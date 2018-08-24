using System;
using System.Collections.Generic;
using MsgPack;
using NetMQ.Sockets;
using ZeroRpc.Net.Core;
using ZeroRpc.Net.Data;

namespace ZeroRpc.Net
{
    /// <summary>
    ///     A ZeroRPC client capable of connecting to ZeroServices and invoking methods.
    /// </summary>
    public class Client : SocketBase
    {
        /// <summary>
        ///     A callback method that is called asynchronously when the result <see cref="Client.InvokeAsync" /> arrives.
        /// </summary>
        /// <param name="error">
        ///     If an error occurred on the server's side, returns an <see cref="ErrorInformation" /> object with
        ///     information about the error. Otherwise, <b>null</b>.
        /// </param>
        /// <param name="result"></param>
        /// <param name="stream"></param>
        public delegate void InvokeCallback(ErrorInformation error, object result, bool stream);

        /// <summary>
        ///     Default heartbeat rate (5 seconds).
        /// </summary>
        public static readonly TimeSpan DefaultHeartbeat = TimeSpan.FromMilliseconds(5000);

        /// <summary>
        ///     Default timeout time (30 seconds).
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The UUID generator function used when client generate a message (see CreateChannel). 
        /// By default, the function used is ZeroRpc.Net.Util.UuidGen.ComputeUuid. 
        /// But you can pass ZeroRpc.Net.Util.UuidGen.ComputeUuidByteArray or even your own function.
        /// </summary>
        public Func<object> UuidGenerator = new Func<object>(ZeroRpc.Net.Util.UuidGen.ComputeUuid);

        private TimeSpan timeout;

        /// <summary>
        ///     Creates a new client with timeout set to <see cref="DefaultTimeout" /> and heartbeat rate set to
        ///     <see cref="DefaultHeartbeat" />.
        /// </summary>
        public Client() : this(DefaultTimeout, DefaultHeartbeat) { }

        /// <summary>
        ///     Creates a new client.
        /// </summary>
        /// <param name="timeout">Time to wait after a method invokation before the connection is considered lost.</param>
        /// <param name="heartbeatInterval">Intervals at wich the connection is tested between a server and a client.</param>
        public Client(TimeSpan timeout, TimeSpan heartbeatInterval) : base(new DealerSocket(), heartbeatInterval)
        {
            this.timeout = timeout;
            ArgumentUnpacker = ArgumentUnpackers.Simple;
        }

        /// <summary>
        ///     The argument unpacker used by the client to deserialize incoming result values.
        /// </summary>
        /// <remarks>
        ///     You can create own custom argument unpackers to fit your specific needs, but <see cref="ArgumentUnpackers" />
        ///     provides a small collection of
        ///     basic built-in unpackers.
        ///     If you don't specify a custom argument unpacker, <see cref="ArgumentUnpackers.Simple" /> will be used.
        /// </remarks>
        public IArgumentUnpacker ArgumentUnpacker { get; set; }

        /// <summary>
        ///     Invokes a remote method asynchronously.
        /// </summary>
        /// <param name="method">Name of the remote method to invoke.</param>
        /// <param name="parameters">The list of paramenters to pass to the method.</param>
        /// <param name="callback">Callback method that will be called once a response is received.</param>
        /// <remarks>
        ///     This is the only invokation method directly exposed by <see cref="Client" />.
        ///     Use various extension methods that provide more specific invokation functionality.
        /// </remarks>
        public void InvokeAsync(string method, object[] parameters, InvokeCallback callback)
        {
            Channel ch = OpenChannel(null);

            ch.MessageRecieved += (sender, args) =>
            {
                string name = args.Event.Name;
                switch (name)
                {
                    case "ERR":
                        IList<MessagePackObject> data = args.Event.Args;
                        if (data.Count != 3)
                        {
                            RaiseError("ProtocolError", "Invalid event: Bad error");
                            return;
                        }
                        callback?.BeginInvoke(new ErrorInformation(data[0].AsString(), data[1].AsString(), data[2].AsString()),
                                             null,
                                             false,
                                             null,
                                             null);
                        CloseChannel(ch);
                        break;
                    case "OK":
                        callback?.BeginInvoke(null, ArgumentUnpacker.Unpack(args.Event.Args[0]), false, null, null);
                        CloseChannel(ch);
                        break;
                    case "STREAM":
                        callback?.BeginInvoke(null, ArgumentUnpacker.Unpack(args.Event.Args), true, null, null);
                        break;
                    case "STREAM_DONE":
                        callback?.BeginInvoke(null, null, false, null, null);
                        CloseChannel(ch);
                        break;
                    default:
                        callback?.BeginInvoke(new ErrorInformation("ProtocolError", "Invalid event: unknown name"), null, false, null, null);
                        CloseChannel(ch);
                        break;
                }
            };
            ch.Error += (sender, args) => callback?.BeginInvoke(args.Info, null, false, null, null);

            ch.StartTimeoutWatch(timeout,
                                 () =>
                                 {
                                     RaiseError("TimeoutExpired", $"Timeout after {timeout.TotalMilliseconds} ms");
                                     CloseChannel(ch);
                                 });
            ch.Send(method, parameters);
        }

        internal override Channel CreateChannel(Event srcEvent)
        {
            return new ClientChannel(UuidGenerator(), this, CHANNEL_CAPACITY, HeartbeatInterval);
        }
    }
}