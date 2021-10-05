namespace CadRevealAutomation;

using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;

/// <summary>
/// Wrapper around mesh2ctm.exe. Converts OBJ to CTM.
/// </summary>
[Cmdlet(VerbsData.Convert, "ObjToCtm")]
[OutputType(typeof(string))]
public class ConvertObjToCtmCmdlet : ExeWrapperCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0),
     ValidateNotNullOrEmpty,
     Alias("Path", "PSPath")]
    public string InputPath { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true, Position = 1),
     ValidateNotNullOrEmpty]
    public string OutputPath { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true),
     ValidateNotNullOrEmpty]
    public string Comment { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true),
     ValidateSet("RAW", "MG1", "MG2")]
    public string Method { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true),
     ValidateRange(0, 9)]
    public uint? CompressionLevel { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true),
     ValidateNotNullOrEmpty]
    public string UpAxis { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public SwitchParameter NoTexCoords { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public SwitchParameter NoColors { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public SwitchParameter NoClobber { get; set; }

    protected override void ProcessRecord()
    {
        var inputPath = GetUnresolvedProviderPathFromPSPath(InputPath);
        var outputPath = string.IsNullOrWhiteSpace(OutputPath)
            ? Path.Combine(Path.GetDirectoryName(inputPath), $"{Path.GetFileNameWithoutExtension(inputPath)}.ctm")
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

        WriteProgress(new ProgressRecord(0, "Processing OBJ file", inputPath));

        var processStartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            FileName = "mesh2ctm"
        };

        processStartInfo.ArgumentList.Add(inputPath);
        processStartInfo.ArgumentList.Add(outputPath);

        if (CompressionLevel.HasValue)
        {
            processStartInfo.ArgumentList.Add($"--level {CompressionLevel}");
        }
        if (!string.IsNullOrWhiteSpace(Comment))
        {
            processStartInfo.ArgumentList.Add($"--comment \"{Comment}\"");
        }
        if (!string.IsNullOrWhiteSpace(Method))
        {
            processStartInfo.ArgumentList.Add($"--method {Method}");
        }
        if (!string.IsNullOrWhiteSpace(UpAxis))
        {
            processStartInfo.ArgumentList.Add($"--upaxis {UpAxis}");
        }
        if (NoTexCoords.IsPresent)
        {
            processStartInfo.ArgumentList.Add("--no-texcoords");
        }
        if (NoColors.IsPresent)
        {
            processStartInfo.ArgumentList.Add("--no-colors");
        }

        var sw = Stopwatch.StartNew();
        if (!TryStartProgram(processStartInfo, out var process))
        {
            return;
        }

        ReadStandard(process.StandardOutput, out var standardOutput);
        ReadStandard(process.StandardError, out var standardError);
        process.WaitForExit();

        if (standardOutput.Length > 0)
        {
            WriteWarning(standardOutput.ToString());
        }

        if (standardError.Length > 0)
        {
            WriteWarning(standardError.ToString());
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