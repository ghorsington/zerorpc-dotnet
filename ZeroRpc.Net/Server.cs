using System;
using NetMQ.Sockets;
using ZeroRpc.Net.Core;
using ZeroRpc.Net.Data;

namespace ZeroRpc.Net
{
    /// <summary>
    ///     A ZeroRPC server that provides a ZeroService through an <see cref="IService" /> interface.
    /// </summary>
    public class Server : SocketBase
    {
        /// <summary>
        ///     A callback that replies to the received method invokation.
        /// </summary>
        /// <param name="error">
        ///     If not <b>null</b>, the server will reply with an error. This instance of
        ///     <see cref="ErrorInformation" /> provides the information about the error.
        /// </param>
        /// <param name="value">If not <b>null</b>, the server will reply with this value.</param>
        /// <param name="hasMore">
        ///     If <b>false</b>, the server specified that the provided value is the final value. If <b>true</b>,
        ///     specifies that more values will be sent in the future.
        /// </param>
        public delegate void ReplyCallback(ErrorInformation error, object value = null, bool hasMore = false);

        /// <summary>
        ///     Default heartbeat rate (5 seconds).
        /// </summary>
        public static readonly TimeSpan DefaultHeartbeat = TimeSpan.FromSeconds(5);

        /// <summary>
        ///     Initializes the server with the specified ZeroService.
        ///     Heartbeat rate is set to <see cref="DefaultHeartbeat" />.
        /// </summary>
        /// <param name="service">A <see cref="IService" /> that implements a ZeroService.</param>
        public Server(IService service) : this(service, DefaultHeartbeat) { }

        /// <summary>
        ///     Initializes the server with the specified ZeroService.
        /// </summary>
        /// <param name="service">A <see cref="IService" /> that implements a ZeroService.</param>
        /// <param name="heartbeatInterval">Intervals at wich the connection is tested between a server and a client.</param>
        public Server(IService service, TimeSpan heartbeatInterval) : base(new RouterSocket(), heartbeatInterval)
        {
            Service = service;
            ArgumentUnpacker = ArgumentUnpackers.Simple;

            EventReceived += Receive;
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
        ///     The implementation of a ZeroService.
        /// </summary>
        public IService Service { get; }

        internal override Channel CreateChannel(Event srcEvent)
        {
            return new ServerChannel(srcEvent, this, CHANNEL_CAPACITY, HeartbeatInterval);
        }

        private void Receive(object sender, EventReceivedArgs args)
        {
            Channel ch = OpenChannel(args.Event);

            bool isFirst = true;

            void Reply(ErrorInformation error, object value, bool more)
            {
                if (error != null)
                {
                    SendError(ch, error);
                }
                else
                {
                    if (isFirst && !more)
                        ch.Send("OK", value);
                    else if (value != null)
                        ch.Send("STREAM", value);

                    if (!more)
                    {
                        if (!isFirst)
                            ch.Send("STREAM_DONE");
                        CloseChannel(ch);
                    }
                }

                isFirst = false;
            }

            ch.Error += (o, errorArgs) => RaiseError(errorArgs.Info);

            try
            {
                if (CoreServices.HasEvent(args.Event.Name))
                    CoreServices.Invoke(this, args.Event.Name, ArgumentUnpacker.Unpack(args.Event.Args), Reply);
                else
                    Service.Invoke(args.Event.Name, ArgumentUnpacker.Unpack(args.Event.Args), Reply);
            }
            catch (Exception e)
            {
                SendError(ch, new ErrorInformation(e.GetType().Name, e.Message, e.StackTrace));
            }
        }

        private void SendError(Channel ch, ErrorInformation info)
        {
            ch.Send("ERR", info.ToArray());
            CloseChannel(ch);
        }
    }
}