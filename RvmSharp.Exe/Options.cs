namespace RvmSharp.Exe;

using System.Collections.Generic;
using CommandLine;

internal class Options
{
    [Option('i', "input", Required = true, HelpText = "Input file or folder, can specify multiple items")]
    public IEnumerable<string> Inputs { get; init; } = [];

    [Option("input-list", Required = false, HelpText = "Text file naming one further input file or folder per line, for input sets too long for a command line.")]
    public string? InputList { get; init; }

    [Option('f', "filter", Required = false, HelpText = "Regex filter to match files in input folder")]
    public string? Filter { get; init; }

    [Option('o', "output", Required = true, HelpText = "Output folder")]
    public string Output { get; init; } = string.Empty;

    [Option('t', "tolerance", Default = 0.1f, Required = false, HelpText = "Tessellation tolerance")]
    public float Tolerance { get; init; }

    [Option('x', "exclude-attribute", Required = false, HelpText = "Exclude nodes (and all children) whose attribute value matches the pattern. Format: 'AttributeKey=ValueRegex' (value is case-insensitive regex). Specify multiple values space-separated after the flag. Use '|' in the regex to match multiple values for the same key, e.g. 'Description=STRU-ANODE|STRU-SOFTVOLUME'. Requires a paired .txt attributes file.")]
    public IEnumerable<string> ExcludeAttributes { get; init; } = [];

    [Option("tag-naming", Default = false, Required = false, HelpText = "Name exported OBJ objects as 'Tag/ExtraInfo' (spaces replaced with dashes). Uses the nearest ancestor's Tag attribute as the base name. Requires a paired .txt attributes file.")]
    public bool TagNaming { get; init; }

    [Option("native-instances-output", Required = false, HelpText = "Optional JSON sidecar containing local-space reusable templates and their original RVM placement transforms.")]
    public string? NativeInstancesOutput { get; init; }

    [Option("native-inventory-output", Required = false, HelpText = "Optional JSON sidecar containing native primitive/template counts, dimensions, and tessellated export-size estimates.")]
    public string? NativeInventoryOutput { get; init; }

    [Option("native-instances-csv-output", Required = false, HelpText = "Optional compact CSV sidecar containing native instance transforms and metadata. Replaces --native-instances-output when selected by the runner.")]
    public string? NativeInstancesCsvOutput { get; init; }

    [Option("native-inventory-csv-output", Required = false, HelpText = "Optional compact CSV sidecar containing native template and kind inventory records. Replaces --native-inventory-output when selected by the runner.")]
    public string? NativeInventoryCsvOutput { get; init; }

    [Option("native-remains-obj", Required = false, HelpText = "Optional OBJ containing only single-use native geometry placements.")]
    public string? NativeRemainsObj { get; init; }

    [Option("native-unique-obj", Required = false, HelpText = "Optional OBJ containing each reused native local-space geometry template once.")]
    public string? NativeUniqueObj { get; init; }

    [Option("native-objects-per-file", Default = 500, Required = false, HelpText = "Maximum objects per native remains or unique OBJ part.")]
    public int NativeObjectsPerFile { get; init; } = 500;

    [Option("native-template-usage-csv", Required = false, HelpText = "Optional CSV companion containing per-template reuse counts and tessellated size estimates.")]
    public string? NativeTemplateUsageCsv { get; init; }

    [Option("native-metadata-output", Required = false, HelpText = "Write deferred node/object metadata to JSONL and compact instance CSV. Requires CSV output and one source RVM.")]
    public string? NativeMetadataOutput { get; init; }

    [Option("native-run-id", Required = false, HelpText = "Shared export run UUID; generated for standalone compact exports if omitted.")]
    public string? NativeRunId { get; init; }

    [Option("native-source-id", Required = false, HelpText = "Unique source name within the export run; defaults to the input RVM filename without its extension.")]
    public string? NativeSourceId { get; init; }

    [Option("native-reuse-probe", Default = false, Required = false, HelpText = "Measure facet-group reuse across all inputs for several match modes and tolerances, write the JSON report to --output and exit without exporting.")]
    public bool NativeReuseProbe { get; init; }

    [Option("native-facet-catalog-build", Default = false, Required = false, HelpText = "Build a model-wide facet-group template catalog from all inputs into the --output folder and exit without exporting. Run once over every source before the per-file exports that use --native-facet-catalog.")]
    public bool NativeFacetCatalogBuild { get; init; }

    [Option("native-facet-tolerance", Default = NativeFacetCatalog.DefaultTolerance, Required = false, HelpText = "Largest distance in metres between a catalog template and any facet group placed with it. Used with --native-facet-catalog-build.")]
    public double NativeFacetTolerance { get; init; } = NativeFacetCatalog.DefaultTolerance;

    [Option("native-facet-catalog", Required = false, HelpText = "Folder written by --native-facet-catalog-build. Facet groups take their template id, geometry and placement from the catalog, so equal parts share one template across files.")]
    public string? NativeFacetCatalogPath { get; init; }

    [Option("native-reference-obj", Required = false, HelpText = "Also write every exported source primitive tessellated straight from the RVM (no catalog, no primitive replacement), in world space and named by its instance id, for comparison with the processed output. Split into partNN files by --native-objects-per-file.")]
    public string? NativeReferenceObj { get; init; }

    [Option("native-remains-in-unique", Default = false, Required = false, HelpText = "Write single-use templates to --native-unique-obj in local space, like reused ones, instead of placed geometry in --native-remains-obj. Requires --native-facet-catalog and --native-unique-obj.")]
    public bool NativeRemainsInUnique { get; init; }

    [Option("native-primitive-replacement", Default = false, Required = false, HelpText = "With --native-facet-catalog-build: replace catalog templates that are boxes, round bars or rectilinear section profiles by unit cube and cylinder placements, when exact within the tolerances below. Section profiles are read from the paired .txt attributes.")]
    public bool NativePrimitiveReplacement { get; init; }

    [Option("native-primitive-unhinted", Default = false, Required = false, HelpText = "Also split prisms outside section nodes into boxes, and accept any profile shape under a section node.")]
    public bool NativePrimitiveUnhinted { get; init; }

    [Option("native-primitive-max-volume-error", Default = 0.12, Required = false, HelpText = "Largest relative volume difference between a replaced template and its primitives.")]
    public double NativePrimitiveMaxVolumeError { get; init; } = 0.12;

    [Option("native-primitive-min-profile-iou", Default = 0.82, Required = false, HelpText = "Smallest overlap (intersection over union) between a section profile and its boxes.")]
    public double NativePrimitiveMinProfileIoU { get; init; } = 0.82;

    [Option("native-primitive-fillet-share", Default = 0.15, Required = false, HelpText = "Largest share of a profile's outline that fillets and chamfers may take before it counts as non-rectilinear.")]
    public double NativePrimitiveFilletShare { get; init; } = 0.15;

    [Option("native-primitive-cube-max-vertices", Default = 4096, Required = false, HelpText = "Safety limit on the vertex count of a template tested as one box.")]
    public int NativePrimitiveCubeMaxVertices { get; init; } = 4096;

    [Option("native-primitive-max-gap", Default = 0.005, Required = false, HelpText = "Largest distance in metres by which a replacement may stick out of the source surface. Rejects squared-off mitres, filled notches and filled holes.")]
    public double NativePrimitiveMaxGap { get; init; } = 0.005;

    [Option("native-primitive-max-missing-gap", Default = 0.02, Required = false, HelpText = "Largest distance in metres from the source surface to its replacement. Rejects filled-in rounded cut-outs; root fillets pass.")]
    public double NativePrimitiveMaxMissingGap { get; init; } = 0.02;

    [Option("native-primitive-box-max-parts", Default = 5, Required = false, HelpText = "Most unit cubes an exact box decomposition may use: templates whose faces all lie in three perpendicular directions (L-panels, notched plates, stacked blocks, channels) are split into boxes with no change in shape. 0 disables it.")]
    public int NativePrimitiveBoxMaxParts { get; init; } = 5;

    [Option("native-primitive-near-box-max-missing", Default = 0.08, Required = false, HelpText = "Largest share of a box's volume the source may lack (grooves, snipes) when it passes the gap check.")]
    public double NativePrimitiveNearBoxMaxMissing { get; init; } = 0.08;

    [Option("native-primitive-snap-box-max-parts", Default = 5, Required = false, HelpText = "Most unit cubes a snap-box decomposition may use, tried only on templates every other fit keeps: faces within --native-primitive-snap-box-max-angle of three perpendicular directions are snapped to box faces, and small features (fillets, chamfers, snipes, small holes) are squared off or filled within the snap-box gap. 0 disables it.")]
    public int NativePrimitiveSnapBoxMaxParts { get; init; } = 5;

    [Option("native-primitive-snap-box-max-angle", Default = 2.0, Required = false, HelpText = "Largest tilt in degrees of a face that the snap-box decomposition snaps to a box face.")]
    public double NativePrimitiveSnapBoxMaxAngle { get; init; } = 2.0;

    [Option("native-primitive-snap-box-max-gap", Default = 0.05, Required = false, HelpText = "Upper limit in metres on the snap-box gap, which is 5% of the template's largest dimension and at least --native-primitive-max-gap.")]
    public double NativePrimitiveSnapBoxMaxGap { get; init; } = 0.05;
}
