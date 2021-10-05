namespace CadRevealAutomation;

using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;

/// <summary>
/// Wrapper around f3df-dump.exe. Converts F3D file to json.
/// </summary>
[Cmdlet(VerbsData.Convert, "F3dToJson")]
[OutputType(typeof(string))]
public class ConvertF3dToJsonCmdlet : ExeWrapperCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0),
     ValidateNotNullOrEmpty,
     Alias("Path", "PSPath")]
    public string InputPath { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true, Position = 1),
     ValidateNotNullOrEmpty]
    public string OutputPath { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public SwitchParameter Compact { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public SwitchParameter Renderables { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public SwitchParameter Yaml { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public SwitchParameter NoClobber { get; set; }

    protected override void ProcessRecord()
    {
        var inputPath = GetUnresolvedProviderPathFromPSPath(InputPath);
        var outputPath = string.IsNullOrWhiteSpace(OutputPath)
            ? $"{inputPath}.dump"
            : GetUnresolvedProviderPathFromPSPath(OutputPath);

        if (!File.Exists(inputPath))
        {
            WriteError(new ErrorRecord(new Exception($"Does not exist {inputPath}"), string.Empty, ErrorCategory.NotSpecified, null));
            return;
        }

        if (NoClobber.IsPresent && File.Exists(outputPath))
        {
            WriteError(new ErrorRecord(new Exception($"Already exist {outputPath}"), string.Empty, ErrorCategory.NotSpecified, null));
            return;
        }

        WriteProgress(new ProgressRecord(0, "Processing F3D file", inputPath));

        var processStartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            FileName = "f3df-dump"
        };

        if (Compact.IsPresent)
        {
            processStartInfo.ArgumentList.Add("--compact");
        }
        if (Renderables.IsPresent)
        {
            processStartInfo.ArgumentList.Add("--renderables");
        }
        if (Yaml.IsPresent)
        {
            processStartInfo.ArgumentList.Add("--yaml");
        }
        processStartInfo.ArgumentList.Add(inputPath);

        var sw = Stopwatch.StartNew();
        if (!TryStartProgram(processStartInfo, out var process))
        {
            return;
        }

        using var streamWriter = File.CreateText(outputPath);
        using var standardOutput = process.StandardOutput;
        string line;
        while ((line = standardOutput.ReadLine()) != null)
        {
            streamWriter.WriteLine(line);
        }
        streamWriter.Flush();

        ReadStandard(process.StandardError, out var stringBuilder);
        process.WaitForExit();

        if (stringBuilder.Length > 0)
        {
            WriteWarning(stringBuilder.ToString());
        }

        if (process.ExitCode != 0)
        {
            WriteError(new ErrorRecord(new Exception($"{processStartInfo.FileName} failed with exit code {process.ExitCode}."), string.Empty, ErrorCategory.NotSpecified, null));
        }

        sw.Stop();
        WriteVerbose($"Wrote {outputPath} in {sw.ElapsedMilliseconds} ms");

        WriteObject(OutputPath);
    }

    private readonly Stopwatch _totalTimer = new Stopwatch();

    protected override void BeginProcessing()
    {
        _totalTimer.Start();
    }

    protected override void EndProcessing()
    {
        _totalTimer.Stop();
        WriteVerbose($"Converted all files in {_totalTimer.ElapsedMilliseconds} ms");
    }
}