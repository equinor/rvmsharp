namespace CadRevealComposer.Utils
{
    using System;
    using System.Diagnostics;

    public static class StopwatchExtensions
    {
        public static TimeSpan GetElapsedAndRestart(this Stopwatch stopwatch)
        {
            var elapsed = stopwatch.Elapsed;
            stopwatch.Restart();
            return elapsed;
        }
    }
}