namespace CadRevealAutomation;

using CadRevealComposer;
using CadRevealComposer.Configuration;
using System.IO;
using System.Management.Automation;

[Cmdlet(VerbsData.Convert, "RvmToRevealModel")]
public class ConvertRvmToRevealModelCmdlet : Cmdlet
{
    [Parameter(Mandatory = true)]
    public DirectoryInfo InputDirectory { get; init; }

    [Parameter(Mandatory = true)]
    public DirectoryInfo OutputDirectory { get; init; }

    [Parameter(Mandatory = true), ValidateRange(0, long.MaxValue)]
    public long ProjectId { get; init; }

    [Parameter(Mandatory = true), ValidateRange(0, long.MaxValue)]
    public long ModelId { get; init; }

    [Parameter(Mandatory = true), ValidateRange(0, long.MaxValue)]
    public long RevisionId { get; init; }

    [Parameter]
    public SwitchParameter NoInstancing { get; init; }

    [Parameter]
    public SwitchParameter SingleSector { get; init; }

    [Parameter]
    public SwitchParameter Zones { get; init; }

    protected override void ProcessRecord()
    {
        var parameters =
            new ModelParameters(
                new ProjectId(ProjectId),
                new ModelId(ModelId),
                new RevisionId(RevisionId),
                new InstancingThreshold(300));
            
        var toolsParameters = new ComposerParameters(
            "mesh2ctm",
            NoInstancing.IsPresent,
            SingleSector.IsPresent,
            Zones.IsPresent);

        CadRevealComposerRunner.Process(InputDirectory, OutputDirectory, parameters, toolsParameters)
            .Wait();
    }
}