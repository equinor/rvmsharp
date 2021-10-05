namespace CadRevealAutomation;

using CadRevealComposer.Operations;
using RvmSharp.BatchUtils;
using RvmSharp.Exporters;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;

[Cmdlet(VerbsData.Export, "FacetGroups")]
[OutputType(typeof(FileInfo))]
public class ExportFacetGroupsCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string RvmDirectory { get; set; }

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string ExportFile { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? Take { get; set; }

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = nameof(GroupKey))]
    public long GroupKey { get; set; }

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = nameof(NotInstanced))]
    public SwitchParameter NotInstanced { get; set; }

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected override void StopProcessing()
    {
        _cancellationTokenSource.Cancel();
    }

    protected override void ProcessRecord()
    {
        var cancellationToken = _cancellationTokenSource.Token;
        var progressRecord = new ProgressRecord(0, "Export facet groups", "Reading RVM files");

        WriteProgress(progressRecord);
        var workload = Workload.CollectWorkload(new[] { RvmDirectory });
        var rvmStore = Workload.ReadRvmData(workload);

        var rvmNodes = rvmStore.RvmFiles
            .Select(f => f.Model)
            .SelectMany(m => m.Children);
        var facetGroups = rvmNodes
            .SelectMany(RecursivelyIterateFacetGroups)
            .ToArray();

        IEnumerable<RvmFacetGroup> facetGroupsToExport;
        if (ParameterSetName == nameof(GroupKey))
        {
            facetGroupsToExport = facetGroups
                .Where(f => RvmFacetGroupMatcher.CalculateKey(f) == GroupKey);
        }
        else if (ParameterSetName == nameof(NotInstanced))
        {
            var matchResults = RvmFacetGroupMatcher.MatchAll(facetGroups, _ => true);

            facetGroupsToExport = matchResults
                .OfType<RvmFacetGroupMatcher.NotInstancedResult>()
                .Select(r => r.FacetGroup);
        }
        else
        {
            throw new ArgumentOutOfRangeException(
                nameof(ParameterSetName),
                ParameterSetName,
                "Unhandled parameter set.");
        }

        progressRecord.StatusDescription = "Writing export file";
        WriteProgress(progressRecord);
        var exportFilePath = GetUnresolvedProviderPathFromPSPath(ExportFile);
        var tmpFile = $"{exportFilePath}.tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(exportFilePath)!);

        WriteVerbose($"Temp file: {tmpFile}");
        WriteVerbose($"Output file: {exportFilePath}");

        try
        {
            using var objExporter = new ObjExporter(tmpFile);

            var meshIndex = 0;

            var exportList = facetGroupsToExport;
            if (Skip.HasValue)
            {
                exportList = exportList.Skip(Skip.Value);
            }
            if (Take.HasValue)
            {
                exportList = exportList.Take(Take.Value);
            }

            foreach (var facetGroup in exportList)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    continue;
                }

                var mesh = TessellatorBridge.Tessellate(facetGroup, 5.0f);

                if (mesh == null)
                {
                    continue;
                }

                objExporter.StartGroup(meshIndex.ToString(CultureInfo.InvariantCulture));
                objExporter.WriteMesh(mesh);
                meshIndex++;
            }
        }
        catch (Exception e)
        {
            File.Delete(tmpFile);
            ThrowTerminatingError(new ErrorRecord(e, "UnknownError", ErrorCategory.NotSpecified, null));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            File.Delete(tmpFile);
        }
        else
        {
            if (File.Exists(exportFilePath))
            {
                File.Delete(exportFilePath);
            }
            File.Move(tmpFile, exportFilePath);
            WriteObject(new FileInfo(exportFilePath));
        }
    }

    private static IEnumerable<RvmFacetGroup> RecursivelyIterateFacetGroups(RvmNode root)
    {
        foreach (var child in root.Children)
        {
            switch (child)
            {
                case RvmNode node:
                    foreach (var facetGroup in RecursivelyIterateFacetGroups(node))
                        yield return facetGroup;
                    break;
                case RvmFacetGroup facetGroup:
                    yield return facetGroup;
                    break;
            }
        }
    }
}