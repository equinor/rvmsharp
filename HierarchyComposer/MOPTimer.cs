namespace HierarchyComposer;

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

internal class MopTimer
{
    private readonly ILogger _logger;
    private readonly string _operation;
    private readonly Stopwatch _stopwatch;

    private MopTimer(string operation, ILogger logger)
    {
        this._operation = operation;
        this._logger = logger;
        logger.LogInformation("Starting {Operation}", operation);
        _stopwatch = Stopwatch.StartNew();
    }

    public static MopTimer Create(string operation, ILogger logger)
    {
        return new MopTimer(operation, logger);
    }

    public void LogCompletion()
    {
        _stopwatch.Stop();
        _logger.LogInformation("{Operation} completed in {Elapsed}", _operation, _stopwatch.Elapsed);
    }

    public static void RunAndMeasure(string operation, ILogger logger, Action action)
    {
        var timer = Create(operation, logger);
        action.Invoke();
        timer.LogCompletion();
    }

    public static T RunAndMeasure<T>(string operation, ILogger logger, Func<T> function)
    {
        var timer = Create(operation, logger);
        var result = function.Invoke();
        timer.LogCompletion();
        return result;
    }
}
