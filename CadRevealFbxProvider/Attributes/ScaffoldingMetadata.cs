namespace CadRevealFbxProvider.Attributes;

using System.Collections.Generic;

public class ScaffoldingMetadata
{
    public static readonly string[] ModelAttributes =
    {
        "Work order",
        "Scaff build Operation number",
        "Dismantle Operation number"
    };

    public static readonly Dictionary<string, string> AttributeMap = new Dictionary<string, string>
    {
        { "Work order", "workOrderId" },
        { "Scaff build Operation number", "buildOperationId" },
        { "Dismantle Operation number", "dismantleOperationId" },
        { "Grand total", "totalWeight" }
    };
}
