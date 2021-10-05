namespace CadRevealAutomation;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Text;

public abstract class ExeWrapperCmdlet : PSCmdlet
{
    protected void ReadStandard(StreamReader streamReader, out StringBuilder stringBuilder)
    {
        stringBuilder = new StringBuilder();
        while (streamReader.ReadLine() is { } errorLine)
        {
            stringBuilder.AppendLine(errorLine);
        }
    }

    protected bool TryStartProgram(ProcessStartInfo processStartInfo, out Process process)
    {
        try
        {
            var proc = Process.Start(processStartInfo);
            if (proc != null)
            {
                process = proc;
                return true;
            }

            WriteError(new ErrorRecord(new Exception("Process object is null."), string.Empty, ErrorCategory.NotSpecified, null));
            process = default;
            return false;
        }
        catch (Win32Exception e) when (e.HResult == -2147467259)
        {
            ThrowTerminatingError(new ErrorRecord(new Exception($"Could not find {processStartInfo.FileName}"), string.Empty, ErrorCategory.NotSpecified, null));
            process = default;
            return false;
        }
        catch (Exception e)
        {
            WriteError(new ErrorRecord(e, string.Empty, ErrorCategory.NotSpecified, null));
            process = default;
            return false;
        }
    }
}