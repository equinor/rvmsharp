namespace CadRevealComposer.Operations;

using Newtonsoft.Json;

[JsonObject]
public class TagDataFromStid
{
    public required string TagNo { get; set; }
    public required string Description { get; set; }
    public required string TagStatus { get; set; }
    public required int TagCategory { get; set; }
    public required string TagCategoryDescription { get; set; }
    public required string? TagType { get; set; }
    public required string UpdatedDate { get; set; }
    public required string LocationCode { get; set; }
    public required string DisciplineCode { get; set; }
    public required string ContrCode { get; set; }
    public required string System { get; set; }
    public required string ProjectCode { get; set; }
    public required string PoNo { get; set; }
    public required string PlantNo { get; set; }
    public float? XCoordinate { get; set; }
    public float? YCoordinate { get; set; }
    public float? ZCoordinate { get; set; }
    public required AdditionalFields[] AdditionalFields { get; set; }
}

[JsonObject]
public class AdditionalFields
{
    public required string Type { get; set; }
    public required string Value { get; set; }
}
