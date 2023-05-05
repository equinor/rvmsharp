namespace CadRevealComposer.SurfaceUnits;

using CsvHelper.Configuration.Attributes;

public class SurfaceUnit
{
    [Name("Surface Unit 1")] 
    public string SurfaceUnit1 { get; set; } = null!;
    [Name("Surface Unit 2")] 
    public string SurfaceUnit2 { get; set; } = null!;
    [Name("Surface Unit 3")] 
    public string SurfaceUnit3 { get; set; } = null!;
    [Name("Surface Unit 4"), Optional] 
    public string SurfaceUnit4 { get; set; } = null!;
    [Name("Surface Unit 5"), Optional] 
    public string SurfaceUnit5 { get; set; } = null!;
    [Name("Surface Unit 6"), Optional] 
    public string SurfaceUnit6 { get; set; } = null!;
    [Name("Surface Unit 7"), Optional] 
    public string SurfaceUnit7 { get; set; } = null!;
    [Name("Surface Unit 8"), Optional] 
    public string SurfaceUnit8 { get; set; } = null!;
    [Name("Surface Unit 9"), Optional] 
    public string SurfaceUnit9 { get; set; } = null!;
    [Name("Surface Unit 10"), Optional] 
    public string SurfaceUnit10 { get; set; } = null!;
    [Name("E3D Reference")] 
    public string E3DReference { get; set; } = null!;
}
