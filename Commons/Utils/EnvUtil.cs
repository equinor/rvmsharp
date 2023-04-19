namespace Commons.Utils;

public static class EnvUtil
{
    /// <summary>
    /// Check if the current build is a debug build. Returns true #if DEBUG
    /// </summary>
    /// <remarks>
    /// Useful when you dont want to use #if DEBUG in the code for conditional debug utils
    /// </remarks>
    public static bool IsDebugBuild =>
#if DEBUG
        true;
#else
        false;
#endif

    /// <summary>
    /// Check if the debugger is currently attached.
    /// </summary>
    public static bool IsDebuggerAttached => System.Diagnostics.Debugger.IsAttached;
}
