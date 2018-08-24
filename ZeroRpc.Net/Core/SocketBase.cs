using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using ZeroRpc.Net.Data;
using ZeroRpc.Net.Util;

namespace ZeroRpc.Net.Core
{
    /// <summary>
    ///     The base socket with support for multiplexing base implementation for ZeroMQ sockets.
    /// </summary>
    /// <remarks>
    ///     The <see cref="SocketBase" /> is a core internal component that is responsible for handling a generic
    ///     connection through ZeroMQ.
    ///     The base implementation supports multiplexing, which is supported through <see cref="Channel" /> collections.
    ///     This base socket is for internal uses only. Use either <see cref="Client" /> or <see cref="Server" /> that
    ///     implement the base socket.
    /// </remarks>
    public abstract class SocketBase : IDisposable
    {
        /// <summary>
        ///     Default capacity of a channel.
        /// </summary>
        public const int CHANNEL_CAPACITY = 100;

        /// <summary>
        ///     Initializes the base socket.
        /// </summary>
        /// <param name="socket">The underlying ZeroMQ socket.</param>
        /// <param name="heartbeatInterval">Heartbeat interval.</param>
        protected SocketBase(NetMQSocket socket, TimeSpan heartbeatInterval)
        {
            HeartbeatInterval = heartbeatInterval;

            Channels = new Dictionary<object, Channel>(Config.MessageIdComparer);
            Closed = false;
            Socket = socket;
            TimerPoller = new TimerPoller();

            Poller = new NetMQPoller {Socket};
            Socket.ReceiveReady += ReceiveMessage;

            TimerPoller.Start();
            Poller.RunAsync();
        }

        /// <summary>
        ///     Specifies, whether the socket is closed (and possibly disposed of).
        /// </summary>
        public bool Closed { get; private set; }

        /// <summary>
        ///     The heartbeat interval specified for this socket.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; }

        /// <summary>
        ///     Specifies, whether the socket currently has any communication channels open.
        /// </summary>
        public bool IsActive => Channels.Count > 0;

        /// <summary>
        ///     The underlying ZeroMQ socket.
        /// </summary>
        protected NetMQSocket Socket { get; }

        internal TimerPoller TimerPoller { get; }

        private Dictionary<object, Channel> Channels { get; }
        private NetMQPoller Poller { get; }

        /// <summary>
        ///     Closes and disposes of the socket and any related resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Fired when an error occurs during work.
        /// </summary>
        public event EventHandler<ErrorArgs> Error;

        /// <summary>
        ///     Terminates all active connections, sends out all remaining data and closes the socket.
        /// </summary>
        /// <param name="linger">Time to wait for the connections to close before forcefully terminating the socket.</param>
        public void Close(TimeSpan linger)
        {
            if (Closed)
                return; // Do not raise an exception

            if (linger != TimeSpan.Zero)
                Socket.Options.Linger = linger;

            Socket.ReceiveReady -= ReceiveMessage;
            TimerPoller.Stop();
            Poller.Stop();
            Socket.Close();
            foreach (var pair in Channels)
                pair.Value.Destroy();
            Channels.Clear();
            Closed = true;
        }

        /// <summary>
        ///     Binds the socket to the specified address. The socket will listen for any inbound connections and messages to the
        ///     address.
        /// </summary>
        /// <param name="address">Address to bind the socket to. Address is in the same format as accepted by ZeroMQ.</param>
        public void Bind(string address)
        {
            Socket.Bind(address);
        }

        /// <summary>
        ///     Connects the socket to the specified address. Any sent messages will go the connected address(es).
        /// </summary>
        /// <param name="address">Address to connect to. Address is in the same format as accepted by ZeroMQ.</param>
        public void Connect(string address)
        {
            Socket.Connect(address);
        }

        /// <summary>
        ///     Raises an error by firing the <see cref="Error" /> event.
        /// </summary>
        /// <param name="info">Information about the error.</param>
        protected void RaiseError(ErrorInformation info)
        {
            Error?.BeginInvoke(this, new ErrorArgs {Info = info}, null, null);
        }

        /// <summary>
        ///     Raises an error by firing the <see cref="Error" /> event.
        /// </summary>
        /// <param name="name">Name of the error.</param>
        /// <param name="message">Error message.</param>
        /// <param name="stack">Optional stack trace.</param>
        protected void RaiseError(string name, string message, string stack = "")
        {
            RaiseError(new ErrorInformation(name, message, stack));
        }

        /// <summary>
        ///     Disposes of the socket.
        /// </summary>
        /// <param name="disposing">Whether or not to manually dispose of other disposables.</param>
        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
                try
                {
                    Socket.Dispose();
                    Poller.Dispose();
                }
                catch (Exception)
                {
                    // TODO: Maybe better disposal checking?
                }
        }

        internal event EventHandler<EventReceivedArgs> EventReceived;

        internal void Send(Event evt)
        {
            NetMQMessage message = SerializerUtils.Serialize(evt);
            Socket.SendMultipartMessage(message);
        }

        internal abstract Channel CreateChannel(Event srcEvent);

        internal Channel OpenChannel(Event sourceEvent)
        {
            Channel result = CreateChannel(sourceEvent);
            Channels[result.Id] = result;

            return result;
        }

        internal void RemoveClosedChannel(Channel channel)
        {
            Channels.Remove(channel.Id);
        }

        internal void CloseChannel(Channel channel)
        {
            channel.Close();
            Channels.Remove(channel.Id);
        }

        private void ReceiveMessage(object sender, NetMQSocketEventArgs args)
        {
            NetMQMessage message = args.Socket.ReceiveMultipartMessage();

            if (message[message.FrameCount - 2].MessageSize != 0)
            {
                RaiseError("ProtocolError", "Invalid event: Second to last argument must be an empty buffer!");
                return;
            }

            var envelope = message.Take(message.FrameCount - 2).Select(n => n.ToByteArray()).ToList();

            Event evt;

            try
            {
                evt = SerializerUtils.Deserialize(envelope, message.Last.ToByteArray());
            }
            catch (Exception ex)
            {
                RaiseError("ProtocolError", $"Invalid event: {ex.Message}", ex.StackTrace);
                return;
            }

            if (evt.Header.ResponseTo != null && Channels.TryGetValue(evt.Header.ResponseTo, out Channel ch))
                ch.ProcessAsync(evt);
            else
                EventReceived?.BeginInvoke(this, new EventReceivedArgs {Event = evt}, null, null);
        }

        private void ReleaseUnmanagedResources()
        {
            Close(TimeSpan.Zero);
        }

        /// <inheritdoc />
        ~SocketBase()
        {
            Dispose(false);
        }
    }
}