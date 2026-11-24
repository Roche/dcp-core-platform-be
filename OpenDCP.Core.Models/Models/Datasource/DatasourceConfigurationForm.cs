namespace OpenDCP.Core.Models.Models.Datasource
{
    /// <summary>
    /// Represents a user-editable form for configuring a datasource, including its type, name, and validation status.
    /// </summary>
    public class DatasourceConfigurationForm
    {
        /// <summary>
        /// Gets or sets the unique identifier of the datasource configuration.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the datasource configuration has been validated.
        /// </summary>
        public bool IsValidated { get; set; }

        /// <summary>
        /// Gets or sets the type of the datasource connection.
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the datasource configuration.
        /// </summary>
        public required string Name { get; set; }
    }

}
