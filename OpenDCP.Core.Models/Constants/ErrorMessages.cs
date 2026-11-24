namespace OpenDCP.Core.Models.Constants
{

    /// <summary>
    /// Contains common error message strings used throughout the application.
    /// </summary>
    public static class ErrorMessages
    {
        // General
        /// <summary>
        /// Indicates a Single Sign-On error.
        /// </summary>
        public const string SSOError = "Single Sign On Error";

        /// <summary>
        /// Indicates a missing or invalid encryption key.
        /// </summary>
        public const string EncryptionKeyInvalid = "Missing or invalid encryption key";

        /// <summary>
        /// Indicates invalid search parameter combinations.
        /// </summary>
        public const string UnallowedSearchParameterCardinality =
            "The search allows only 1-many combination not many-many combinations of search parameters";

        // Permissions
        /// <summary>
        /// Action requires Global admin permission.
        /// </summary>
        public const string GlobalAdminPermissionRequired = "The action requires Global admin permission";

        /// <summary>
        /// Action requires a change reason.
        /// </summary>
        public const string ChangeReasonRequired = "The action requires a change reason to proceed";

        /// <summary>
        /// Indicates permission is denied.
        /// </summary>
        public const string PermissionDenied = "Permission denied";

        /// <summary>
        /// The user is unknown.
        /// </summary>
        public const string UnkownUser = "The user is not known";

        /// <summary>
        /// The user does not have access to the site.
        /// </summary>
        public const string NoUserAccess = "The user does not have access to this site";

        /// <summary>
        /// The provided refresh token is invalid.
        /// </summary>
        public const string InvalidRefreshToken = "The provided refresh token is not valid";

        // Clients
        /// <summary>
        /// The user client is unknown.
        /// </summary>
        public const string UnkownUserClient = "The user client is not known";

        /// <summary>
        /// The system client is unknown.
        /// </summary>
        public const string UnkownClient = "The system client is not known";

        /// <summary>
        /// The client is invalid.
        /// </summary>
        public const string InvalidClient = "The client is invalid";

        /// <summary>
        /// The certificate is invalid.
        /// </summary>
        public const string InvalidCertificate = "The certificate is invalid";

        // Administration
        /// <summary>
        /// The site is unknown.
        /// </summary>
        public const string UnkownSite = "The site is not known";

        /// <summary>
        /// The module is unknown.
        /// </summary>
        public const string UnkownModule = "The module is not known";

        /// <summary>
        /// Cannot delete a site with active users.
        /// </summary>
        public const string DeleteSiteWithUsers = "Can not delete site with active users";

        /// <summary>
        /// Start and end time are required.
        /// </summary>
        public const string StartAndEndTimeRequired = "Start and end time are required";

        // Banners
        /// <summary>
        /// The user info banner is unknown.
        /// </summary>
        public const string UnkownBanner = "The user info banner is not known";

        // Datasource
        /// <summary>
        /// The data source is unknown.
        /// </summary>
        public const string UnkownDatasource = "The data source is not known";

        /// <summary>
        /// The provided webId is invalid.
        /// </summary>
        public const string InvalidWebId = "The provided webId is not valid";

        /// <summary>
        /// The connection type is unknown.
        /// </summary>
        public const string UnkownConnectionType = "The connection type is not known";

        /// <summary>
        /// A mandatory field of the connection string is missing.
        /// </summary>
        public const string MissingRequiredConnectionStringParameter = "A mandatory field of the connection string is missing";

        /// <summary>
        /// The connection string is invalid.
        /// </summary>
        public const string InvalidConnectionString = "The connection string is invalid";

        /// <summary>
        /// The value type cannot be processed by the system.
        /// </summary>
        public const string UnknownValueType = "The Value Type can not be processed by the system";

        /// <summary>
        /// The batch ID is invalid.
        /// </summary>
        public const string InvalidBatchWebId = "Invalid batch id";

        /// <summary>
        /// The web ID length is invalid.
        /// </summary>
        public const string NotValidWebIdLength = "Not valid web id length";

        /// <summary>
        /// Device web ID is required.
        /// </summary>
        public const string DeviceWebIdRequired = "Device web id is required";

        /// <summary>
        /// Event frame web ID is required.
        /// </summary>
        public const string EventFrameWebIdRequired = "Event frame web id is required";

        /// <summary>
        /// Sensor web IDs are required.
        /// </summary>
        public const string SensorWebIdsRequired = "Sensor web ids are required";

        /// <summary>
        /// Times parameter is required.
        /// </summary>
        public const string TimesRequired = "Times is required";

        /// <summary>
        /// Filter expressions are not supported.
        /// </summary>
        public const string FilterExpressionNotSupported = "Filter expression is not supported";

        /// <summary>
        /// The retrieval mode is invalid.
        /// </summary>
        public const string InvalidRetrievalMode = "Invalid retrieval mode";

        /// <summary>
        /// Invalid global filter link format.
        /// </summary>
        public const string InvalidGlobalFilterLinkFormat = "Invalid global filter link format";

        /// <summary>
        /// Event frame IDs are required.
        /// </summary>
        public const string EventFrameIdsRequired = "Event frame ids are required";

        /// <summary>
        /// Connection Bridge keys mappings are required.
        /// </summary>
        public const string ConnectionBridgeKeyMappingsRequired = "Connection Bridge keys mappings are required";

        /// <summary>
        /// Event frame attributes are required.
        /// </summary>
        public const string EventFrameAttributesRequired = "Event frame attributes are required";

        /// <summary>
        /// The event frame type is unknown.
        /// </summary>
        public const string UnknownEventFrameType = "Unknown event frame type";

        /// <summary>
        /// The group type is unknown.
        /// </summary>
        public const string UnknownGroupType = "Unknown group type";

        /// <summary>
        /// The sensor filter type defined in OSISoftPI datasource for this site is unknown.
        /// </summary>
        public const string UnknownSensorFilterType = "Unknown sensor filter type defined in OSISoftPI data source for this site";

        // DataType mapping
        /// <summary>
        /// The data types for the module are not defined.
        /// </summary>
        public const string UnknownRequiredDataType = "The data types for the module are not defined";

        /// <summary>
        /// A mapping for a required data type is missing.
        /// </summary>
        public const string MissingRequiredDataType = "A mapping for an required data Type is mising";

        /// <summary>
        /// The provided multi datasource mapping is not supported.
        /// </summary>
        public const string InvalidMultiDataTypeMapping = "The provided multi datasource mapping is not supported";

        /// <summary>
        /// The datasource does not provide the datatype it's mapped to.
        /// </summary>
        public const string InvalidDatasourceDataTypeMapping = "The datasource does not provide the datatype it's mapped to";

        // Message delivery channel
        /// <summary>
        /// The provided messaging channel is not supported.
        /// </summary>
        public const string InvalidMessageDeliveryChannel = "The provided messaging channel is not supported";

        // Site
        /// <summary>
        /// The site name already exists.
        /// </summary>
        public const string SiteNameExist = "Site name already exist";

        /// <summary>
        /// Site IDs parameter is required.
        /// </summary>
        public const string SiteIdsRequired = "Site ids parameter is required";

        /// <summary>
        /// The site does not exist.
        /// </summary>
        public const string SiteDontExist = "Site doesn`t exist";

        // ShareLink
        /// <summary>
        /// Some elements could not be found.
        /// </summary>
        public const string SomeElementsCouldNotBeFound = "Some elements could not be found";

        /// <summary>
        /// The global filter link cannot be found.
        /// </summary>
        public const string CannotFindGlobalFilterLink = "Link not found";

        /// <summary>
        /// The share link alias cannot be found.
        /// </summary>
        public const string ShareLinkAliasNotFound = "Share link alias not found";

        /// <summary>
        /// Name is required for the share link alias.
        /// </summary>
        public const string ShareLinkAliasNameRequired = "Name is required";

        /// <summary>
        /// Alias name must be unique for the selected site.
        /// </summary>
        public const string AliasNameNeedUniqueForGivenSite = "Alias name need to be unique for selected site";

        /// <summary>
        /// Title and Link are required for a global filter link.
        /// </summary>
        public const string GlobalFilterLinkRequiredParameters = "Title and Link are required";

        /// <summary>
        /// Invalid or missing configuration - for a key
        /// </summary>
        public const string MissingConfiguration = "Missing configuration";

        /// <summary>
        /// Invalid or missing configuration - for a key
        /// </summary>
        public const string SessionExpired = "Session Expired";

        /// <summary>
        /// Invalid connection bridges
        /// </summary>
        public const string InvalidConnectionBridges = "Invalid Connection Bridges. Please ensure all props are valid and the connection bridges are unique";

        /// <summary>
        /// Missing dashboard tab name
        /// </summary>
        public const string DashboardTabRequiredParameter = "Dashboard tab name is required";

        /// <summary>
        /// Missing dashboard tab entity
        /// </summary>
        public const string DashboardTabNotFound = "Dashboard tab not found";

        /// <summary>
        /// Missing dashboard export entity
        /// </summary>
        public const string DashboardExportNotFound = "Dashboard export not found";

        /// <summary>
        /// Can not change notification.
        /// </summary>
        public const string CannotChangeDashboardTabLayout = "Only admin and owner of the dashboard tab can edit it";

        /// <summary>
        /// Search cardinality product problem
        /// </summary>
        public const string SearchCardinalityProductProblem = "Too many filter parameters. Please narrow your search to either a single Site with multiple Ids, or multiple Sites with a single Id.";

        // Module
        /// <summary>
        /// The Module already exists.
        /// </summary>
        public const string ModuleAlreadyExist = "Module already exist";

        // Datasource
        /// <summary>
        /// The Datasource already exists.
        /// </summary>
        public const string DatasourceAlreadyExist = "Datasource already exist";
    }
}
