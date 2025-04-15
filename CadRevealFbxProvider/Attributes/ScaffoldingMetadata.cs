namespace CadRevealFbxProvider.Attributes;

using System.Collections.Generic;
using System.Numerics;

public class ScaffoldingMetadata
{
    public string? WorkOrder { get; set; }
    public string? BuildOperationNumber { get; set; }
    public string? DismantleOperationNumber { get; set; }
    public string? ProjectNumber { get; set; }
    public string? TotalVolume { get; set; }
    public string? TotalWeight { get; set; }
    public string? TotalWeightCalculated { get; set; }
    public bool TempScaffoldingFlag { get; set; }

    private const string WorkOrderFieldName = "Scaffolding_WorkOrder_WorkOrderNumber";
    private const string BuildOpFieldName = "Scaffolding_WorkOrder_BuildOperationNumber";
    private const string DismantleOpFieldName = "Scaffolding_WorkOrder_DismantleOperationNumber";
    private const string TotalVolumeFieldName = "Scaffolding_TotalVolume";
    private const string TotalWeightFieldName = "Scaffolding_TotalWeight";
    private const string TotalWeightCalculatedFieldName = "Scaffolding_TotalWeightCalc";
    private const string TempFlagCalculatedFieldName = "Scaffolding_IsTemporary";

    public static readonly string[] MandatoryModelAttributesFromParts_NonTempScaff =
    {
        "Work order",
        "Scaff build Operation number",
        "Dismantle Operation number"
    };

    public static readonly string[] MandatoryModelAttributesFromParts_TempScaff = { "Project number" };

    public static readonly string[] MandatoryModelAttributes = { "Total weight" };

    public static readonly int NumberOfModelAttributes = Enum.GetNames(typeof(AttributeEnum)).Length;

    private enum AttributeEnum
    {
        WorkOrderId,
        BuildOperationId,
        DismantleOperationId,
        ProjectNumber,
        TotalVolume,
        TotalWeight,
        TotalWeightCalculated
    }

    private static readonly Dictionary<string, AttributeEnum> ColumnToAttributeMap = new Dictionary<
        string,
        AttributeEnum
    >
    {
        { "Work order", AttributeEnum.WorkOrderId },
        { "Scaff build Operation number", AttributeEnum.BuildOperationId },
        { "Dismantle Operation number", AttributeEnum.DismantleOperationId },
        { "Project number", AttributeEnum.ProjectNumber },
        { "Size (m\u00b3)", AttributeEnum.TotalVolume },
        { "Grand total", AttributeEnum.TotalWeight },
        { "Grand total calculated", AttributeEnum.TotalWeightCalculated }
    };

    private static void GuardForInvalidValues(string? newValue, string? existingValue)
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
            _ => false
        };
    }

    private static string? MakeStringEmptyIfNonDuplicateButSkipEmpty(string? newValue, string? existingValue)
    {
        if (newValue == "")
            return existingValue;
        if (existingValue == null)
            return newValue;

        if (newValue != existingValue)
            Console.WriteLine(
                $"Warning: variable attribute values found where it should not: ({existingValue},{newValue})"
            );

        return (newValue != existingValue) ? "" : newValue;
    }

    public bool TryAddValue(string key, string? value)
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
                    WorkOrder = MakeStringEmptyIfNonDuplicateButSkipEmpty(value, WorkOrder);
                    break;
                case AttributeEnum.BuildOperationId:
                    BuildOperationNumber = MakeStringEmptyIfNonDuplicateButSkipEmpty(value, BuildOperationNumber);
                    break;
                case AttributeEnum.DismantleOperationId:
                    DismantleOperationNumber = MakeStringEmptyIfNonDuplicateButSkipEmpty(
                        value,
                        DismantleOperationNumber
                    );
                    break;
                case AttributeEnum.ProjectNumber:
                    ProjectNumber = MakeStringEmptyIfNonDuplicateButSkipEmpty(value, ProjectNumber);
                    break;
                case AttributeEnum.TotalVolume:
                    TotalVolume = MakeStringEmptyIfNonDuplicateButSkipEmpty(value, TotalVolume);
                    break;
                case AttributeEnum.TotalWeight:
                    GuardForInvalidValues(value, TotalWeight);
                    TotalWeight = value;
                    break;
                case AttributeEnum.TotalWeightCalculated:
                    GuardForInvalidValues(value, TotalWeightCalculated);
                    TotalWeightCalculated = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        return false; // Did not add any value
    }

    public bool ModelMetadataHasExpectedValues(bool tempScaffFlag = false)
    {
        if (tempScaffFlag)
        {
            if (string.IsNullOrEmpty(ProjectNumber))
                return false;
        }
        // work-order scaffs
        else if (
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

    public static bool PartMetadataHasExpectedValues(Dictionary<string, string> targetDict, bool tempScaffFlag = false)
    {
        var obligatoryAttributes =
            (tempScaffFlag)
                ? MandatoryModelAttributesFromParts_TempScaff
                : MandatoryModelAttributesFromParts_NonTempScaff;

        foreach (var modelAttribute in obligatoryAttributes)
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
        if (!ModelMetadataHasExpectedValues(TempScaffoldingFlag))
            throw new Exception("Cannot write metadata: invalid content");

        // The if above ensures that the fields are not null
        targetDict.Add(WorkOrderFieldName, WorkOrder!);
        targetDict.Add(BuildOpFieldName, BuildOperationNumber!);
        targetDict.Add(DismantleOpFieldName, DismantleOperationNumber!);
        targetDict.Add(TotalVolumeFieldName, TotalVolume!);
        targetDict.Add(TotalWeightFieldName, TotalWeight!);
        targetDict.Add(TotalWeightCalculatedFieldName, TotalWeightCalculated!);
        targetDict.Add(TempFlagCalculatedFieldName, TempScaffoldingFlag.ToString());
    }
}
