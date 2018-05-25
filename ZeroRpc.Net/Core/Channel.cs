using System;
using System.Collections.Generic;
using MsgPack;
using ZeroRpc.Net.Data;
using ZeroRpc.Net.Util;

namespace ZeroRpc.Net.Core
{
    public enum ChannelState
    {
        Open,
        Closing,
        Closed
    }

    internal class EventReceivedArgs : EventArgs
    {
        public Event Event { get; internal set; }
    }

    internal class ClosingArgs : EventArgs
    {
        public Channel Channel { get; internal set; }
    }

    public class ErrorArgs : EventArgs
    {
        public ErrorInformation Info { get; internal set; }
    }

    internal class Channel
    {
        public const int PROTOCOL_VERSION = 3;
        private readonly int capacity;
        private readonly List<byte[]> envelope;
        private readonly TimeSpan hearbeatInterval;
        private DateTime heartbeatExpirationTime;
        private Timer heartbeatTimer;
        private readonly BufferedQueue<Event> inBuffer;
        private readonly BufferedQueue<Event> outBuffer;
        private readonly SocketBase socket;
        private ChannelState state;
        private Timer timeoutTimer;

        internal Channel(object id, List<byte[]> envelope, SocketBase socket, int capacity, TimeSpan hearbeatInterval)
        {
            Id = id;
            this.envelope = envelope;
            this.socket = socket;
            this.capacity = capacity;
            this.hearbeatInterval = hearbeatInterval;
            state = ChannelState.Open;

            inBuffer = new BufferedQueue<Event>(capacity) {Capacity = 1};
            outBuffer = new BufferedQueue<Event>(1);

            ResetHeartbeat();
            RunHearbeat();
        }

        public object Id { get; }
        public event EventHandler<EventReceivedArgs> MessageRecieved;
        public event EventHandler<EventArgs> Closing;
        public event EventHandler<EventArgs> Closed;
        public event EventHandler<ErrorArgs> Error;

        public void ProcessAsync(Event evt)
        {
            EventProcessor worker = ProcessEvent;
            worker.BeginInvoke(evt, null, null);
        }

        public void Close()
        {
            state = ChannelState.Closing;
            Closing?.Invoke(null, new ClosingArgs {Channel = this});
            Flush();
        }

        public void Destroy()
        {
            socket.TimerPoller.Remove(heartbeatTimer);
            state = ChannelState.Closed;
            if (timeoutTimer != null)
            {
                timeoutTimer.Enabled = false;
                socket.TimerPoller.Remove(timeoutTimer);
            }
            Closed?.Invoke(null, new ClosingArgs {Channel = this});
        }

        public void Flush()
        {
            while (outBuffer.Count > 0)
            {
                socket.Send(outBuffer.Dequeue());
                outBuffer.ReduceCapacity();
            }

            if (state == ChannelState.Closing)
            {
                Destroy();
                socket.RemoveClosedChannel(this);
            }
        }

        public void StartTimeoutWatch(TimeSpan timeout, Action callback)
        {
            if (timeoutTimer != null)
                return;

            void Timeout()
            {
                socket.TimerPoller.Remove(timeoutTimer);
                timeoutTimer = null;
                callback();
            }

            timeoutTimer = new Timer(timeout, Timeout);
            socket.TimerPoller.Add(timeoutTimer);
        }

        public void StopTimeoutWatch()
        {
            if (timeoutTimer == null)
                return;
            socket.TimerPoller.Remove(timeoutTimer);
            timeoutTimer = null;
        }

        public void Send(string evtName, params object[] args)
        {
            if (state != ChannelState.Open)
                throw new Exception("Channel is closed!");

            Event evt = new Event {Envelope = envelope, Header = CreateHeader(), Name = evtName, Args = SerializerUtils.Serialize(args)};

            if (outBuffer.HasCapacity)
            {
                socket.Send(evt);
                outBuffer.ReduceCapacity();
            }
            else
            {
                outBuffer.Enqueue(evt);
            }
        }

        protected virtual EventHeader CreateHeader()
        {
            return new EventHeader {Version = PROTOCOL_VERSION, MessageId = UuidGen.ComputeUuid(), ResponseTo = Id};
        }

        private void ProcessEvent(Event evt)
        {
            if (evt.Name == "_zpc_more")
            {
                if (evt.Args.Count > 0 && evt.Args[0].UnderlyingType == typeof(int))
                {
                    outBuffer.Capacity = evt.Args[0].AsInt32();
                    Flush();
                }
                else
                {
                    Error?.BeginInvoke(this,
                                       new ErrorArgs {Info = new ErrorInformation("ProtocolError", "Invalid event: Bad buffer message")},
                                       null,
                                       null);
                    Destroy();
                    socket.RemoveClosedChannel(this);
                }
            }
            else if (evt.Name == "_zpc_hb")
            {
                ResetHeartbeat();
            }
            else if (state == ChannelState.Open)
            {
                inBuffer.Enqueue(evt);
                inBuffer.ReduceCapacity();

                AsyncEventProcessor processor = () =>
                {
                    // Add mutex?
                    Event evtMsg = inBuffer.Dequeue();

                    if (evtMsg.Name == "STREAM" && inBuffer.Capacity < capacity / 2)
                        ResetCapacity();

                    MessageRecieved?.Invoke(null, new EventReceivedArgs {Event = evtMsg});
                };

                processor.BeginInvoke(null, null);
            }
        }

        private void RunHearbeat()
        {
            void Heartbeat()
            {
                if (DateTime.Now > heartbeatExpirationTime)
                {
                    Error?.BeginInvoke(this,
                                       new ErrorArgs
                                       {
                                           Info = new ErrorInformation("HeartbeatError",
                                                                       $"Lost remote after {hearbeatInterval.TotalMilliseconds} ms")
                                       },
                                       null,
                                       null);
                    Destroy();
                    socket.RemoveClosedChannel(this);
                    return;
                }
                if (state != ChannelState.Open)
                    return; // Don't heartbeat closing connections; we just want to flush everything and be done with it
                try
                {
                    Event evt = new Event
                    {
                        Envelope = envelope,
                        Header = CreateHeader(),
                        Name = "_zpc_hb",
                        Args = new List<MessagePackObject>()
                    };
                    socket.Send(evt);
                }
                catch (Exception)
                {
                    // If this ever happens, skip a heartbeat and let gracefully close itself
                }
            }

            heartbeatTimer = new Timer(hearbeatInterval, Heartbeat, true);

            socket.TimerPoller.Add(heartbeatTimer);
        }

        private void ResetHeartbeat()
        {
            heartbeatExpirationTime = DateTime.Now + hearbeatInterval + hearbeatInterval;
        }

        private void ResetCapacity()
        {
            int newCapacity = capacity - inBuffer.Count;

            if (newCapacity > 0)
            {
                Event evt = new Event
                {
                    Envelope = envelope,
                    Header = CreateHeader(),
                    Name = "_zpc_more",
                    Args = new List<MessagePackObject> {newCapacity}
                };
                socket.Send(evt);
            }
        }

        private delegate void EventProcessor(Event evt);

        private delegate void AsyncEventProcessor();
    }
}