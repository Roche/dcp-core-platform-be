using OpenDCP.Framework.Configuration.Model.Errors;
using OpenDCP.Framework.Configuration.Model.Warnings;

namespace OpenDCP.Core.Models.Models.ConnectionBridge
{
    /// <summary>
    /// Represents the response of a translation operation, including any errors, warnings, and the resulting translations.
    /// </summary>
    public class TranslationResponse
    {
        /// <summary>
        /// Gets or sets the list of errors that occurred during the translation process.
        /// </summary>
        public List<DatasourceError> Errors { get; set; } = new List<DatasourceError>();

        /// <summary>
        /// Gets or sets the list of warnings encountered during the translation process.
        /// </summary>
        public List<DatasourceWarning> Warnings { get; set; } = new List<DatasourceWarning>();

        /// <summary>
        /// Gets or sets the dictionary of translated items.
        /// The key is the original identifier, and the value is a list of translated strings associated with that identifier.
        /// </summary>
        public Dictionary<string, List<string>> TranslationDictonary { get; set; } = new Dictionary<string, List<string>>();
    }
}
