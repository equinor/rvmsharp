namespace CadRevealFbxProvider.Attributes
{
    /// <summary>
    /// Document metadata from STID with additional computed fields relevant for Echo
    /// This is a subset of the fields from the record with the same name from EchoModelDistribution project, which exports the json file
    /// We keep only fields that are relevant for:
    /// - cross checking the work order id,
    /// - determining if the scaffolding has status TEMP and
    /// - adding name suffix (document title can be used as a human-readable name)
    /// </summary>
    public record StidScaffoldingDocumentEchoDto
    {
        /// <summary>
        /// Title of the document, here used as suffix after the algorithmically created document name
        /// </summary>
        public required string DocTitle { get; init; }

        /// <summary>
        /// WorkOrderId without zero padding.
        /// </summary>
        public required string? WorkOrderId { get; set; }

        /// <summary>
        /// Similar to the "TEMP" from the old scaffolding system.
        /// When a scaffolding does not have a work order assigned we assume it is in planning phase and simplify some of the guards and checks on the scaffolding files.
        /// </summary>
        public required bool HasWorkOrderAssigned { get; set; } = false;
    }
}
