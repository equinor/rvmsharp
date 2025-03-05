namespace CadRevealFbxProvider.Attributes;

using System.Collections.Generic;
using System.Numerics;

public class ScaffoldingMetadata
{
    public string? WorkOrder { get; set; }
    public string? BuildOperationNumber { get; set; }
    public string? DismantleOperationNumber { get; set; }
    public string? TotalVolume { get; set; }
    public string? TotalWeight { get; set; }

    private const string WorkOrderFieldName = "Scaffolding_WorkOrder_WorkOrderNumber";
    private const string BuildOpFieldName = "Scaffolding_WorkOrder_BuildOperationNumber";
    private const string DismantleOpFieldName = "Scaffolding_WorkOrder_DismantleOperationNumber";
    private const string TotalVolumeFieldName = "Scaffolding_TotalVolume";
    private const string TotalWeightFieldName = "Scaffolding_TotalWeight";

    public static readonly string[] ModelAttributesPerPart =
    {
        "Work order",
        //        "Scaff build Operation number",
        //        "Dismantle Operation number"
    };

    public static readonly int NumberOfModelAttributes = Enum.GetNames(typeof(AttributeEnum)).Length;

    private enum AttributeEnum
    {
        WorkOrderId,
        BuildOperationId,
        DismantleOperationId,
        TotalVolume,
        TotalWeight,
    }

    private static readonly Dictionary<string, AttributeEnum> ColumnToAttributeMap = new Dictionary<
        string,
        AttributeEnum
    >
    {
        { "Work order", AttributeEnum.WorkOrderId },
        { "Scaff build Operation number", AttributeEnum.BuildOperationId },
        { "Dismantle Operation number", AttributeEnum.DismantleOperationId },
        { "Size (m\u00b3)", AttributeEnum.TotalVolume },
        { "Grand total", AttributeEnum.TotalWeight },
    };

    private static void GuardForInvalidValues(string newValue, string? existingValue)
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

    private static bool OverrideNullOrWhiteSpaceCheck(AttributeEnum mappedKey)
    {
        return mappedKey switch
        {
            AttributeEnum.TotalVolume => true,
            AttributeEnum.BuildOperationId => true,
            AttributeEnum.DismantleOperationId => true,
            _ => false,
        };
    }

    private static string? MakeStringEmptyIfDuplicate(string newValue, string? existingValue)
    {
        if (newValue == "")
            return existingValue;
        if (existingValue == null)
            return newValue;
        return (newValue != existingValue) ? "" : newValue;
    }

    public bool TryAddValue(string key, string value)
    {
        if (!ColumnToAttributeMap.ContainsKey(key))
            return false;

        var mappedKey = ColumnToAttributeMap[key];
        if (!string.IsNullOrWhiteSpace(value) || OverrideNullOrWhiteSpaceCheck(mappedKey))
        {
            switch (mappedKey)
            {
                case AttributeEnum.WorkOrderId:
                    GuardForInvalidValues(value, WorkOrder);
                    WorkOrder = value;
                    break;
                case AttributeEnum.BuildOperationId:
                    BuildOperationNumber = MakeStringEmptyIfDuplicate(value, BuildOperationNumber);
                    break;
                case AttributeEnum.DismantleOperationId:
                    DismantleOperationNumber = MakeStringEmptyIfDuplicate(value, DismantleOperationNumber);
                    break;
                case AttributeEnum.TotalVolume:
                    TotalVolume = MakeStringEmptyIfDuplicate(value, TotalVolume);
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
            //            || string.IsNullOrEmpty(BuildOperationNumber)
            //            || string.IsNullOrEmpty(DismantleOperationNumber)
            || string.IsNullOrEmpty(TotalWeight)
        )
        {
            return false;
        }

        return true;
    }

    public static bool HasExpectedValuesFromAttributesPerPart(Dictionary<string, string> targetDict)
    {
        foreach (var modelAttribute in ModelAttributesPerPart)
        {
            if (!targetDict.ContainsKey(modelAttribute))
                return false;

            var value = targetDict.TryGetValue(modelAttribute, out string? existingValue) ? existingValue : null;
            if (string.IsNullOrEmpty(value))
                return false;
        }

        return true;
    }

    public void TryWriteToGenericMetadataDict(Dictionary<string, string> targetDict)
    {
        if (!HasExpectedValues())
            throw new Exception("Cannot write metadata: invalid content");

        // The if above ensures that the fields are not null
        targetDict.Add(WorkOrderFieldName, WorkOrder!);
        targetDict.Add(BuildOpFieldName, BuildOperationNumber!);
        targetDict.Add(DismantleOpFieldName, DismantleOperationNumber!);
        targetDict.Add(TotalVolumeFieldName, TotalVolume!);
        targetDict.Add(TotalWeightFieldName, TotalWeight!);
    }
}
