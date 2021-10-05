namespace CadRevealAutomation;

using CadRevealComposer.Utils;
using RvmSharp;
using RvmSharp.Containers;
using RvmSharp.Exporters;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;

[Cmdlet(VerbsData.Convert, "RvmToObj", DefaultParameterSetName = SingleRvmFile)]
public class ConvertRvmToObjCmdlet : PSCmdlet
{
    private const string SingleRvmFile = nameof(SingleRvmFile);
    private const string MultipleRvmFiles = nameof(MultipleRvmFiles);

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true),
     Alias("Path", "PSPath")]
    public string RvmFile { get; init; }

    [Parameter(Mandatory = true, ParameterSetName = MultipleRvmFiles)]
    public string ObjFile { get; init; }

    [Parameter]
    public SwitchParameter NoClobber { get; init; }

    private CancellationTokenSource _cancellationTokenSource;
    private string _tempOutputPath;
    private string _outputPath;
    private ObjExporter _objExporter;

    protected override void BeginProcessing()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        if (ParameterSetName == MultipleRvmFiles)
        {
            var outputPath = GetUnresolvedProviderPathFromPSPath(ObjFile);

            if (File.Exists(outputPath) && NoClobber.IsPresent)
            {
                ThrowTerminatingError(new ErrorRecord(null, "Output path exist.", ErrorCategory.ResourceExists, null));
            }

            _outputPath = outputPath;
            _tempOutputPath = $"{outputPath}.temp";
            _objExporter = new ObjExporter(_tempOutputPath);
        }
    }

    protected override void EndProcessing()
    {
        if (ParameterSetName == MultipleRvmFiles)
        {
            _objExporter.Dispose();
            _objExporter = null;

            if (File.Exists(_outputPath) && !NoClobber.IsPresent)
            {
                File.Delete(_outputPath);
            }

            File.Move(_tempOutputPath, _outputPath);

            WriteObject(_outputPath);
        }
    }

    protected override void StopProcessing()
    {
        _cancellationTokenSource.Cancel();

        if (ParameterSetName == MultipleRvmFiles)
        {
            _objExporter.Dispose();
            _objExporter = null;
            if (File.Exists(_tempOutputPath))
            {
                File.Delete(_tempOutputPath);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var inputPath = GetUnresolvedProviderPathFromPSPath(RvmFile);

        if (ParameterSetName == SingleRvmFile)
        {
            var outputPath = string.IsNullOrWhiteSpace(ObjFile)
                ? Path.Combine(Path.GetDirectoryName(inputPath), $"{Path.GetFileNameWithoutExtension(inputPath)}.obj")
                : GetUnresolvedProviderPathFromPSPath(ObjFile);

            _outputPath = outputPath;
            _tempOutputPath = $"{outputPath}.temp";
            _objExporter = new ObjExporter(_tempOutputPath);
        }

        WriteProgress(new ProgressRecord(0, "Processing RVM file", inputPath));

        using var stream = File.OpenRead(inputPath);
        var rvmFile = RvmParser.ReadRvm(stream);
        var rvmPrimitives = EnumerateNodes(rvmFile)
            .SelectMany(x => x.Children.OfType<RvmPrimitive>())
            .Select(x => TessellatorBridge.Tessellate(x, 1.0f))
            .WhereNotNull();

        foreach (var rvmPrimitive in rvmPrimitives)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                break;
            }
            _objExporter.WriteMesh(rvmPrimitive);
        }

        if (ParameterSetName == SingleRvmFile)
        {
            _objExporter.Dispose();
            _objExporter = null;

            if (File.Exists(_outputPath) && !NoClobber.IsPresent)
            {
                File.Delete(_outputPath);
            }

            File.Move(_tempOutputPath, _outputPath);

            WriteObject(_outputPath);
        }
    }

    private static IEnumerable<RvmNode> EnumerateNodes(RvmFile rvmFile)
    {
        var stack = new Stack<RvmNode>();
        foreach (var node in rvmFile.Model.Children)
        {
            stack.Push(node);
        }

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();
            foreach (var node in currentNode.Children.OfType<RvmNode>())
            {
                stack.Push(node);
            }

            yield return currentNode;
        }
    }
}