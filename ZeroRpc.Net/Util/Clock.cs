using System.Diagnostics;

namespace ZeroRpc.Net.Util
{
    internal class Clock
    {
        public static long NowUs()
        {
            long ticksPerSecond = Stopwatch.Frequency;
            long tickCount = Stopwatch.GetTimestamp();

            double ticksPerMicrosecond = ticksPerSecond / 1000000.0;
            return (long) (tickCount / ticksPerMicrosecond);
        }

        public static long NowMs()
        {
            return NowUs() / 1000;
        }
    }
}