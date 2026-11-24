using System.ComponentModel;

namespace OpenDCP.Core.Models.Constants
{
    /// <summary>
    /// Defines the types of security-related actions that can occur within the system.
    /// </summary>
    public enum SecurityAction : byte
    {
        /// <summary>
        /// User login action.
        /// </summary>
        [Description("Login")]
        Login = 1,

        /// <summary>
        /// User logout action.
        /// </summary>
        [Description("Logout")]
        Logout = 2,

        /// <summary>
        /// Failed login attempt.
        /// </summary>
        [Description("Login fail")]
        LoginFail = 3,

        /// <summary>
        /// Successful REST token operation.
        /// </summary>
        [Description("Rest token successfully")]
        RestTokenSuccessfully = 4,

        /// <summary>
        /// Failed REST token request.
        /// </summary>
        [Description("Rest token request fail")]
        RestTokenRequestFail = 5,

        /// <summary>
        /// Successful Refresh token operation.
        /// </summary>
        [Description("Refresh token successfully")]
        RefreshTokenSuccessfully = 6,

        /// <summary>
        /// Failed refresh token request.
        /// </summary>
        [Description("Refresh token request fail")]
        RefreshTokenRequestFail = 7,
    }

}
