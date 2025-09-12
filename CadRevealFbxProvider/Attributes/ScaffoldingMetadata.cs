namespace CadRevealFbxProvider.Attributes;

using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    public string? NameSuffix { get; set; }

    private const string WorkOrderFieldName = "Scaffolding_WorkOrder_WorkOrderNumber";
    private const string BuildOpFieldName = "Scaffolding_WorkOrder_BuildOperationNumber";
    private const string DismantleOpFieldName = "Scaffolding_WorkOrder_DismantleOperationNumber";
    private const string TotalVolumeFieldName = "Scaffolding_TotalVolume";
    private const string TotalWeightFieldName = "Scaffolding_TotalWeight";
    private const string TotalWeightCalculatedFieldName = "Scaffolding_TotalWeightCalc";
    private const string TempFlagFieldName = "Scaffolding_IsTemporary";
    private const string SuffixFieldName = "Scaffolding_NameSuffix";

    private static readonly string[] MandatoryModelAttributesFromPartsNonTempScaff =
    [
        "Work order",
        "Scaff build Operation number",
        "Dismantle Operation number",
    ];

    // TODO: requires revisiting (AB#295585)
    // it seemed that "Project number" was mandatory for temp scaffs, but maybe it is not.
    private static readonly string[] MandatoryModelAttributesFromPartsTempScaff = [];

    public static readonly int NumberOfModelAttributes =
        Enum.GetNames(typeof(AttributeEnum)).Length
        + 1 /*file suffix*/
    ;
    public static readonly int NumberOfMandatoryModelAttributesFromPartsNonTempScaff =
        MandatoryModelAttributesFromPartsNonTempScaff.Length;

    private enum AttributeEnum
    {
        WorkOrderId,
        BuildOperationId,
        DismantleOperationId,
        ProjectNumber,
        TotalVolume,
        TotalWeight,
        TotalWeightCalculated,
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
        { "Grand total calculated", AttributeEnum.TotalWeightCalculated },
    };

    private static readonly Dictionary<string, string> ColumnToClassPropertyMap = new Dictionary<string, string>
    {
        { "Work order", "WorkOrder" },
        { "Scaff build Operation number", "BuildOperationNumber" },
        { "Dismantle Operation number", "DismantleOperationNumber" },
        { "Project number", "ProjectNumber" },
        { "Size (m\u00b3)", "TotalVolume" },
        { "Grand total", "TotalWeight" },
        { "Grand total calculated", "TotalWeightCalculated" },
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
            _ => false,
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

    public void GetSuffixFromFilename(string filename)
    {
        NameSuffix = "";

        var match = Regex.Match(filename, @"-\d+-(.+)$");
        if (match.Success)
        {
            NameSuffix = match.Groups[1].Value;
            Console.WriteLine($"Processed work order scaffolding CSV file: {filename} with suffix: {NameSuffix}");
        }
        else
        {
            Console.WriteLine($"Processed work order scaffolding CSV file: {filename} with no suffix.");
        }
    }

    public void ThrowIfWorkOrderFromFilenameInvalid(string filename)
    {
        // this function is only called for work order scaffolding files
        // so calling this for temp scaffs must be by mistake, -> throw an exception
        if (TempScaffoldingFlag)
        {
            throw new Exception(
                "Scaffolding metadata implies we expect a temporary scaffolding file, but this method is only for work order scaffolding files."
            );
        }

        var match = Regex.Match(filename, @"-(\d+)(?:-|$)");
        if (match.Success)
        {
            string workOrderFromFilename = match.Groups[1].Value;
            Console.WriteLine(
                $"Processed work order scaffolding CSV file: {filename} with work order number: {workOrderFromFilename}"
            );

            // this is also handling of work order scaffs only
            // the filename MUST contain a work order number, and it MUST match the one in the metadata
            if (string.IsNullOrEmpty(workOrderFromFilename) || workOrderFromFilename != WorkOrder)
            {
                throw new ScaffoldingFilenameException(
                    $"Scaffolding metadata work order {WorkOrder} does not match the work order from filename {workOrderFromFilename}"
                );
            }
        }
        else
        {
            throw new ScaffoldingFilenameException(
                $"Scaffolding CSV file {filename} does not contain a correctly-formatted work order number in the filename."
            );
        }
    }

    public void ThrowIfModelMetadataInvalid(bool tempScaffFlag = false)
    {
        string missingFields = string.Empty;
        bool success = true;

        // total weight is mandatory for both temp and work order scaffs
        if (string.IsNullOrEmpty(TotalWeight))
        {
            missingFields += "\"Grand total\", ";
            success = false;
        }

        if (tempScaffFlag)
        {
            // TODO: requires revisiting
            // commenting out this temporarily (?) until it is clear if we should have a mandatory field in temp scaff attributes
            //if (string.IsNullOrEmpty(ProjectNumber))
            //    return false;

            if (!success)
                throw new ScaffoldingMetadataMissingFieldException(
                    $"Temp scaffolding metadata is missing a mandatory field: {missingFields.TrimEnd(',', ' ')}."
                );
        }
        // work-order scaffs
        else
        {
            foreach (var attr in MandatoryModelAttributesFromPartsNonTempScaff)
            {
                var propertyName = ColumnToClassPropertyMap[attr];
                var prop = typeof(ScaffoldingMetadata).GetProperty(propertyName);
                var valueOfProperty = prop?.GetValue(this)?.ToString();

                if (string.IsNullOrEmpty(valueOfProperty))
                {
                    missingFields += $"\"{attr}\", ";
                    success = false;
                }
            }

            if (!success)
                throw new ScaffoldingMetadataMissingFieldException(
                    $"Scaffolding metadata is missing a mandatory field(s): {missingFields.TrimEnd(',', ' ')}."
                );
        }
    }

    public static bool PartMetadataHasExpectedValues(Dictionary<string, string> targetDict, bool tempScaffFlag = false)
    {
        var mandatoryAttributes =
            (tempScaffFlag)
                ? MandatoryModelAttributesFromPartsTempScaff
                : MandatoryModelAttributesFromPartsNonTempScaff;

        foreach (var modelAttribute in mandatoryAttributes)
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
        ThrowIfModelMetadataInvalid(TempScaffoldingFlag);

        // The if above ensures that the fields are not null
        targetDict.Add(WorkOrderFieldName, WorkOrder!);
        targetDict.Add(BuildOpFieldName, BuildOperationNumber!);
        targetDict.Add(DismantleOpFieldName, DismantleOperationNumber!);
        targetDict.Add(TotalVolumeFieldName, TotalVolume!);
        targetDict.Add(TotalWeightFieldName, TotalWeight!);
        targetDict.Add(TotalWeightCalculatedFieldName, TotalWeightCalculated!);
        targetDict.Add(TempFlagFieldName, TempScaffoldingFlag ? "true" : "false");
        targetDict.Add(SuffixFieldName, NameSuffix ?? "");
    }
}
