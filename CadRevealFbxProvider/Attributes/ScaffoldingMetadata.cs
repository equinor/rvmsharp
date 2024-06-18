namespace CadRevealFbxProvider.Attributes;

using System.Collections.Generic;

public class ScaffoldingMetadata
{
    public string? WorkOrder { get; set; }
    public string? BuildOperationNumber { get; set; }
    public string? DismantleOperationNumber { get; set; }
    public string? TotalWeight { get; set; }

    public const string WorkOrderFieldName = "Scaffolding_WorkOrder_WorkOrderNumber";
    public const string BuildOpFieldName = "Scaffolding_WorkOrder_BuildOperationNumber";
    public const string DismantleOpFieldName = "Scaffolding_WorkOrder_DismantleOperationNumber";
    public const string TotalWeightFieldName = "Scaffolding_TotalWeight";

    public static readonly string[] ModelAttributesPerPart = ["Work order"];

    public static readonly int NumberOfModelAttributes = Enum.GetNames(typeof(AttributeEnum)).Length;

    public enum AttributeEnum
    {
        WorkOrderId,
        BuildOperationId,
        DismantleOperationId,
        TotalWeight
    }

    public static readonly Dictionary<string, AttributeEnum> ColumnToAttributeMap = new Dictionary<
        string,
        AttributeEnum
    >
    {
        { "Work order", AttributeEnum.WorkOrderId },
        { "Scaff build Operation number", AttributeEnum.BuildOperationId },
        { "Dismantle Operation number", AttributeEnum.DismantleOperationId },
        { "Grand total", AttributeEnum.TotalWeight }
    };

    public void GuardForInvalidValues(string newValue, string? existingValue)
    {
        if (newValue == existingValue)
            return;

        if (!string.IsNullOrWhiteSpace(existingValue))
            throw new Exception(
                "We already had a value for the key, but got a different one now. This is unexpected. Values was: "
                    + newValue
                    + " and "
                    + existingValue
            );
    }

    public bool TryAddValue(string key, string value)
    {
        if (!ColumnToAttributeMap.ContainsKey(key))
            return false;

        var mappedKey = ColumnToAttributeMap[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            switch (mappedKey)
            {
                case AttributeEnum.WorkOrderId:
                    GuardForInvalidValues(value, WorkOrder);
                    WorkOrder = value;
                    break;
                case AttributeEnum.BuildOperationId:
                    GuardForInvalidValues(value, BuildOperationNumber);
                    BuildOperationNumber = value;
                    break;
                case AttributeEnum.DismantleOperationId:
                    GuardForInvalidValues(value, DismantleOperationNumber);
                    DismantleOperationNumber = value;
                    break;
                case AttributeEnum.TotalWeight:
                    GuardForInvalidValues(value, TotalWeight);
                    TotalWeight = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        return false; // Did not add any value
    }

    public bool HasExpectedValues()
    {
        if (
            // Do not include PlannedBuildDate, CompletionDate, and DismantleDate here, since these may be allowed empty
            string.IsNullOrEmpty(WorkOrder)
            || string.IsNullOrEmpty(BuildOperationNumber)
            || string.IsNullOrEmpty(DismantleOperationNumber)
            || string.IsNullOrEmpty(TotalWeight)
        )
        {
            return false;
        }

        return true;
    }

    // public static bool HasExpectedValuesFromAttributesPerPart(Dictionary<string, string> targetDict)
    // {
    //     foreach (var modelAttribute in ModelAttributesPerPart)
    //     {
    //         if (!targetDict.ContainsKey(modelAttribute))
    //             return false;
    //
    //         var value = targetDict.TryGetValue(modelAttribute, out string? existingValue) ? existingValue : null;
    //         if (string.IsNullOrEmpty(value))
    //             return false;
    //     }
    //
    //     return true;
    // }

    public void TryWriteToGenericMetadataDict(Dictionary<string, string> targetDict)
    {

        // the if above ensures that the fields and not null
        targetDict.Add(WorkOrderFieldName, WorkOrder ?? "-1");
        targetDict.Add(BuildOpFieldName, BuildOperationNumber ?? "-1");
        targetDict.Add(DismantleOpFieldName, DismantleOperationNumber ?? "-1");
        targetDict.Add(TotalWeightFieldName, TotalWeight?? "-1");
    }
}
