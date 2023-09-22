namespace CadRevealFbxProvider.Attributes;

using System.Collections.Generic;

public class ScaffoldingMetadata
{
    public string WorkOrder { get; set; }
    public string BuildOperationNumber { get; set; }
    public string DismantleOperationNumber { get; set; }

    public const string TotalWeightFieldName = "Scaffolding_TotalWeight";
    public string TotalWeight { get; set; }

    public static readonly string[] ModelAttributes =
    {
        "Work order", "Scaff build Operation number", "Dismantle Operation number"
    };

    public enum AttributeEnum
    {
        WorkOrderId,
        BuildOperationId,
        DismantleOperationId,
        TotalWeight
    }

    public static readonly Dictionary<string, AttributeEnum> ColumnToAttributeMap = new Dictionary<string, AttributeEnum>
    {
        { "Work order", AttributeEnum.WorkOrderId },
        { "Scaff build Operation number",  AttributeEnum.BuildOperationId },
        { "Dismantle Operation number",  AttributeEnum.DismantleOperationId },
        { "Grand total",  AttributeEnum.TotalWeight }
    };


    public void GuardForInvalidValues(string newValue, string existingValue)
    {
        if (newValue == existingValue) return;

        if (!string.IsNullOrWhiteSpace(existingValue))
            throw new Exception(
                "We already had a value for the key, but got a different one now. This is unexpected. Values was: " +
                newValue + " and " + existingValue);
    }

    public bool TryAddValue(string key, string value)
    {
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
        if (string.IsNullOrEmpty(WorkOrder) || string.IsNullOrEmpty(BuildOperationNumber) ||
            string.IsNullOrEmpty(DismantleOperationNumber) || string.IsNullOrEmpty(TotalWeight))
        {
            return false;
        }

        return true;
    }

    public void WriteToGenericMetadataDict(Dictionary<string,string> targetDict)
    {
        targetDict.Add("Scaffolding_WorkOrder_WorkOrderNumber", this.WorkOrder);
        targetDict.Add("Scaffolding_WorkOrder_BuildOperationNumber", this.BuildOperationNumber);
        targetDict.Add("Scaffolding_WorkOrder_DismantleOperationNumber", this.DismantleOperationNumber);
        targetDict.Add(ScaffoldingMetadata.TotalWeightFieldName, this.TotalWeight);
    }
}
