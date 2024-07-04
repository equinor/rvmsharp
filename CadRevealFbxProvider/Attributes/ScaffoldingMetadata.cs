namespace CadRevealFbxProvider.Attributes;

using System.Collections.Generic;

public class ScaffoldingMetadata
{
    public string? WorkOrder { get; set; }
    public string? BuildOperationNumber { get; set; }
    public string? DismantleOperationNumber { get; set; }
    public string? TotalWeight { get; set; }
    public string? ScaffType { get; set; }
    public string? ScaffTagNumber { get; set; }

    public const string WorkOrderFieldName = "Scaffolding_WorkOrder_WorkOrderNumber";
    public const string BuildOpFieldName = "Scaffolding_WorkOrder_BuildOperationNumber";
    public const string DismantleOpFieldName = "Scaffolding_WorkOrder_DismantleOperationNumber";
    public const string TotalWeightFieldName = "Scaffolding_TotalWeight";
    public const string TagNumberFieldName = "Scaffolding_TagNumber";
    public const string TypeFieldName = "Scaffolding_Type";

    public static readonly string[] ModelAttributesPerPart =
    {
        "Work order",
        "Scaff build Operation number",
        "Dismantle Operation number",
        "Scaff type",
        "Scaff tag number"
    };

    public static readonly int NumberOfModelAttributes = Enum.GetNames(typeof(AttributeEnum)).Length;

    public enum AttributeEnum
    {
        WorkOrderId,
        BuildOperationId,
        DismantleOperationId,
        TotalWeight,
        ScaffType,
        ScaffTagNumber
    }

    public static readonly Dictionary<string, AttributeEnum> ColumnToAttributeMap = new Dictionary<
        string,
        AttributeEnum
    >
    {
        { "Work order", AttributeEnum.WorkOrderId },
        { "Scaff build Operation number", AttributeEnum.BuildOperationId },
        { "Dismantle Operation number", AttributeEnum.DismantleOperationId },
        { "Grand total", AttributeEnum.TotalWeight },
        { "Scaff type", AttributeEnum.ScaffType },
        { "Scaff tag number", AttributeEnum.ScaffTagNumber }
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

        bool updateKey = IsKeyToBeUpdated(key, value);
        var mappedKey = ColumnToAttributeMap[key];
        if (updateKey)
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
                case AttributeEnum.ScaffTagNumber:
                    GuardForInvalidValues(value, ScaffTagNumber);
                    ScaffTagNumber = value;
                    break;
                case AttributeEnum.ScaffType:
                    GuardForInvalidValues(value, ScaffType);
                    ScaffType = value;
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
            string.IsNullOrEmpty(WorkOrder)
//            || string.IsNullOrEmpty(BuildOperationNumber)
//            || string.IsNullOrEmpty(DismantleOperationNumber)
            || string.IsNullOrEmpty(TotalWeight)
//            || string.IsNullOrEmpty(ScaffTagNumber)
//            || string.IsNullOrEmpty(ScaffType)
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

            bool forceAdd = IsKeyMarkedAsForceAdd(modelAttribute);
            var value = targetDict.TryGetValue(modelAttribute, out string? existingValue) ? existingValue : null;
            if (string.IsNullOrEmpty(value) && !forceAdd)
                return false;
        }

        return true;
    }

    public void TryWriteToGenericMetadataDict(Dictionary<string, string> targetDict)
    {
        if (!HasExpectedValues())
            throw new Exception("Cannot write metadata: invalid content");

        // the if above ensures that the fields and not null
        targetDict.Add(WorkOrderFieldName, WorkOrder!);
        targetDict.Add(BuildOpFieldName, BuildOperationNumber!);
        targetDict.Add(DismantleOpFieldName, DismantleOperationNumber!);
        targetDict.Add(TotalWeightFieldName, TotalWeight!);
        targetDict.Add(TagNumberFieldName, ScaffTagNumber!);
        targetDict.Add(TypeFieldName, ScaffType!);
    }


    private string? GetValue(string key)
    {
        var mappedKey = ColumnToAttributeMap[key];
        switch (mappedKey)
        {
            case AttributeEnum.WorkOrderId: return WorkOrder;
            case AttributeEnum.BuildOperationId: return BuildOperationNumber;
            case AttributeEnum.DismantleOperationId: return DismantleOperationNumber;
            case AttributeEnum.TotalWeight: return TotalWeight;
            // case AttributeEnum.ScaffTagNumber: return ScaffTagNumber;
            // case AttributeEnum.ScaffType: return ScaffType;
            // case AttributeEnum.JobPack: return JobPack;
            // case AttributeEnum.ProjectNumber: return ProjectNumber;
            // case AttributeEnum.PlannedBuildDate: return PlannedBuildDate;
            // case AttributeEnum.CompletionDate: return CompletionDate;
            // case AttributeEnum.DismantleDate: return DismantleDate;
            // case AttributeEnum.Area: return Area;
            // case AttributeEnum.Discipline: return Discipline;
            // case AttributeEnum.Purpose: return Purpose;
        }

        return null;
    }

    private static bool IsNewValueEmptyButHasExistedBefore(string? newValue, string? existingValue)
    {
        return string.IsNullOrWhiteSpace(newValue) && !string.IsNullOrWhiteSpace(existingValue);
    }
    private bool IsKeyToBeUpdated(string key, string value)
    {
        // If a key is marked to be forcefully added
        // * and if all calls for a specific key have null value => key will never be updated and remain null (i.e., ignore)
        // * and if all calls for a specific key have empty value => key will never be updated and remain null (i.e., ignore)
        // * and if one or more calls for a specific key have non-empty value => key will be updated with the non-empty value, BUT WILL BE REPLACED BY EMPTY/NULL VALUES TO FOLLOW
        // * and if one or more calls for a specific key have empty or null value => key will be updated with the last non-empty value
        // If a key is marked NOT to be forcefully added
        // * and if all calls for a specific key have null value => key will never be updated and remain null (i.e., ignore)
        // * and if all calls for a specific key have empty value => key will never be updated and remain null (i.e., ignore)
        // * and if one or more calls for a specific key have non-empty value => key will be updated with the non-empty value, BUT WILL BE REPLACED BY EMPTY/NULL VALUES TO FOLLOW
        // * and if one or more calls for a specific key have empty or null value => key will be updated with the last non-empty value

        bool forceUpdate = false;
        if (IsKeyMarkedAsForceAdd(key))
        {
            string? existingValue = GetValue(key);
            if (existingValue != null)
            {
                forceUpdate = !IsNewValueEmptyButHasExistedBefore(value, existingValue);
            }
        }

        return !string.IsNullOrWhiteSpace(value) || forceUpdate;
    }

    private static bool IsKeyMarkedAsForceAdd(string key)
    {
        var mappedKey = ColumnToAttributeMap[key];

        bool forceAdd = false;
        switch (mappedKey)
        {
            case AttributeEnum.BuildOperationId: forceAdd = true; break;
            case AttributeEnum.DismantleOperationId: forceAdd = true; break;
            case AttributeEnum.ScaffType: forceAdd = true; break;
            case AttributeEnum.ScaffTagNumber: forceAdd = true; break;
        }

        return forceAdd;
    }
}
