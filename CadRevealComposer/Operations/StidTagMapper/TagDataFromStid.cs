namespace CadRevealComposer.Operations;

using Newtonsoft.Json;

[JsonObject]
public class TagDataFromStid
{
    public string TagNo { get; set; }
    public string Description { get; set; }
    public string TagStatus { get; set; }
    public int TagCategory { get; set; }
    public string TagCategoryDescription { get; set; }
    public string TagType { get; set; }
    public string UpdatedDate { get; set; }
    public string LocationCode { get; set; }
    public string DisciplineCode { get; set; }
    public string ContrCode { get; set; }
    public string System { get; set; }
    public string ProjectCode { get; set; }
    public string PoNo { get; set; }
    public string PlantNo { get; set; }
    public float? XCoordinate { get; set; }
    public float? YCoordinate { get; set; }
    public float? ZCoordinate { get; set; }
    public AdditionalFields[] AdditionalFields { get; set; }
}

[JsonObject]
public class AdditionalFields
{
    public string Type { get; set; }
    public string Value { get; set; }
}
