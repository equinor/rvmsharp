namespace CadRevealFbxProvider.Attributes
{
    /// <summary>
    /// Document metadata from STID with additional computed fields relevant for Echo
    /// This is a copy of the record with the same name from EchoModelDistribution project, which exports the json file
    /// </summary>
    public record StidScaffoldingDocumentEchoDto
    {
        /// <summary>
        /// The plant code associated with the document
        /// Three letters in lower case, e.g. "abc"
        /// </summary>
        public required string PlantCode { get; set; }

        /// <summary>
        /// The installation code (JSV, JCA, TON, TROA, etc.)
        /// </summary>
        public required string InstCode { get; init; }

        /// <summary>
        /// The document number (document ID)
        /// </summary>
        public required string DocNo { get; init; }

        /// <summary>
        /// The document type (document type, like SCAFF)
        /// </summary>
        public required string DocType { get; init; }

        /// <summary>
        /// Title of the document
        /// </summary>
        public required string DocTitle { get; init; }

        /// <summary>
        /// The document classification code
        /// </summary>
        public required string DocClass { get; init; }

        /// <summary>
        /// Optional remark on the document
        /// </summary>
        /// <example>
        /// "Validation failed due to missing work order reference". Or similar...
        /// </example>
        public string? Remark { get; init; }

        public required string RevNo { get; set; }

        /// <summary>
        /// Additional fields, e.g., validation, work order
        /// </summary>
        public required StidFileEchoDto[] Files { get; init; } = [];

        /// <summary>
        /// WorkOrderId without zero padding.
        /// </summary>
        public required string WorkOrderId { get; set; } = string.Empty;

        /// <summary>
        /// Similar to the "TEMP" from the old scaffolding system.
        /// When a scaffolding does not have a work order assigned we assume it is in planning phase and simplify some of the guards and checks on the scaffolding files.
        /// </summary>
        public required bool HasWorkOrderAssigned { get; set; } = false;

        /// <summary>
        /// This is a simplified status to easily check if the document has passed the validation checks.
        /// </summary>
        public required bool HasBeenValidatedSuccessfully { get; set; } = false;

        public required string PlantDocNo { get; set; }
    }
}
