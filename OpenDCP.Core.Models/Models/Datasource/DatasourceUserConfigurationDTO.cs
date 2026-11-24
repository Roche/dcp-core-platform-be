using OpenDCP.Framework.FormBuilder.Models;

namespace OpenDCP.Core.Models.Models.Datasource
{
    /// <summary>
    /// Represents the configuration of a datasource for a user, including its type, validation status, name, and connection details.
    /// </summary>
    public class DatasourceUserConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the optional unique identifier of the datasource configuration.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the optional site identifier of the module configuration, if null it's a global module configuration.
        /// </summary>
        public int? SiteId { get; set; }

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

        /// <summary>
        /// Gets or sets the collection of connection string parameters or form values for the datasource.
        /// </summary>
        public List<FormValue> ConnectionString { get; set; } = new List<FormValue>();

        //ToDo add required when new FE version is deployed
        /// <summary>
        /// Gets or sets the reason for any changes made to the site configuration.
        /// </summary>
        public string ChangeReason { get; set; } = string.Empty;
    }

}
