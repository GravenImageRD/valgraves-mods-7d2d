using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Valgraves.Common
{
    public static class TimeCall
    {
        public static void Run(string actionName, Action action)
        {
            var timer = Stopwatch.StartNew();
            action();
            Logging.Error($"[{actionName}] {timer.ElapsedMilliseconds}ms");
        }
        
        public static TReturn Run<TReturn>(string actionName, Func<TReturn> action)
        {
            var timer = Stopwatch.StartNew();
            var result = action();
            Logging.Error($"[{actionName}] {(decimal)timer.ElapsedTicks / TimeSpan.TicksPerMillisecond}ms");
            return result;
        }
    }
}