using System;
using System.Collections.Generic;
using System.Threading;
using ZeroRpc.Net.Util;

namespace ZeroRpc.Net.Core
{
    internal class Timer
    {
        public Timer(TimeSpan interval, Action onElapsed, bool loop = false)
        {
            Interval = (long) interval.TotalMilliseconds;
            OnElapsed = onElapsed;
            Loop = loop;
            Wait = Interval;
            Enabled = true;
        }

        public bool Enabled { get; set; }
        public long Interval { get; }
        public bool Loop { get; }
        public Action OnElapsed { get; }
        internal long Wait { get; set; }
    }

    internal class TimerPoller
    {
        private long prevTime = -1;
        private bool running;
        private readonly List<Timer> timers;
        private readonly Thread workThread;

        public TimerPoller()
        {
            timers = new List<Timer>();
            workThread = new Thread(Run);
        }

        public void Start()
        {
            if (running)
                return;
            workThread.Start();
            running = true;
        }

        public void Stop()
        {
            if (!running)
                return;
            running = false;
            workThread.Join();
        }

        public void Add(Timer timer)
        {
            lock (timers)
            {
                timers.Add(timer);
            }
        }

        public void Remove(Timer timer)
        {
            timer.Enabled = false;
            lock (timers)
            {
                timers.Remove(timer);
            }
        }

        private void Run()
        {
            while (running)
            {
                if (prevTime == -1)
                {
                    prevTime = Clock.NowMs();
                    continue;
                }

                long time = Clock.NowMs();
                long timePassed = time - prevTime;
                prevTime = time;

                Timer[] currTimers;
                lock (timers)
                {
                    currTimers = timers.ToArray();
                }
                foreach (Timer timer in currTimers)
                    if (timer.Enabled)
                    {
                        timer.Wait -= timePassed;
                        if (timer.Wait <= 0)
                        {
                            timer.OnElapsed();
                            if (timer.Loop)
                                timer.Wait = timer.Interval;
                        }
                    }
            }
        }
    }
}