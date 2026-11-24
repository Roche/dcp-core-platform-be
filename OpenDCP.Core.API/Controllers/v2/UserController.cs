using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.Messages;
using OpenDCP.Core.Models.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OpenDCP.Core.Core.Extensions;
using OpenDCP.Framework.Hosting.CustomAttributes;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Controller for managing user-related operations in the DCP system.
    /// </summary>
    [Route("api/v2/user/")]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="userService">The user service for handling user-related operations.</param>
        public UserController(IConfiguration configuration, IUserService userService) : base(configuration)
        {
            _userService = userService;
        }

        /// <summary>
        /// Send feedback from DCP user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("SendUserFeedBack")]
        [Route("feedback")]
        public async Task<IActionResult> SendUserFeedback([FromBody] UserFeedbackDTO model)
        {
            var user = GetUserContext();
            await _userService.SendUserFeedbackAsync(model, user);
            return Ok();
        }

        /// <summary>
        /// Retrieves detailed information about the currently authenticated user.
        /// </summary>
        /// <remarks>
        /// This endpoint provides user details based on the authenticated user's context. 
        /// The response includes information such as user ID, name, email, and other relevant attributes.
        /// </remarks>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing a <see cref="UserDTO"/> object 
        /// with details about the currently authenticated user.
        /// </returns>
        /// <response code="200">Returns the user information.</response>
        /// <response code="401">The user is not authenticated.</response>
        [HttpGet]
        [Route("info")]
        [Produces(typeof(UserDTO))]
        public async Task<IActionResult> GetUserInfo()
        {
            var user = GetUserContext();
            var result = await _userService.GetUserInfoAsync(user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Updates the settings for the currently authenticated user.
        /// </summary>
        /// <remarks>
        /// This endpoint allows the authenticated user to modify their personal settings. 
        /// The settings are specified in the request body as a <see cref="UserSettingsDTO"/> object.
        /// </remarks>
        /// <param name="model">
        /// A <see cref="UserSettingsDTO"/> object containing the user's updated settings.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the outcome of the operation.
        /// </returns>
        /// <response code="200">The user settings were successfully updated.</response>
        [HttpPut]
        [Route("settings")]
        public async Task<IActionResult> UpdateUserSettings([FromBody] UserSettingsDTO model)
        {
            var user = GetUserContext();
            await _userService.UpdateUserSettingsAsync(model, user);
            return Ok();
        }

        /// <summary>
        /// Updates the currently authenticated production site.
        /// </summary>
        /// <remarks>
        /// This endpoint allows the authenticated user to to switch their production site 
        /// The site is specified as an integer parameter in the query string.
        /// </remarks>
        /// <param name="siteId">
        /// The unique identifier of the site to be set as the user's preferred site.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the outcome of the operation.
        /// </returns>
        /// <response code="200">The user's preferred site was successfully updated.</response>
        [HttpPut]
        [Route("site/set")]
        public async Task<IActionResult> UpdateUserSite(int siteId)
        {
            var user = GetUserContext();
            await _userService.SetUserSiteAsync(siteId, user);
            return Ok();
        }

        /// <summary>
        /// Retrieves a list of users associated with a specified production site.
        /// </summary>
        /// <remarks>
        /// This endpoint returns a list of users who are associated with the given production site. 
        /// The site is specified by its unique identifier provided as a query parameter.
        /// </remarks>
        /// <param name="siteId">
        /// The unique identifier of the site for which the users are to be retrieved.
        /// </param>
        /// <returns>
        /// Returns a list of users associated with the specified site as a collection of <see cref="UserListItemDTO"/> objects.
        /// </returns>
        /// <response code="200">The list of users associated with the site was successfully retrieved.</response>
        /// <response code="400">The request is invalid, e.g., if the siteId is not provided or invalid.</response>
        /// <response code="401">The user is not authenticated.</response>
        [HttpGet]
        [Route("list")]
        [Produces(typeof(List<UserListItemDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetUsers([BindRequired] int siteId)
        {
            var user = GetUserContext();
            var result = await _userService.GetAllUsersPerSiteAsync(siteId, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Queries users based on specified criteria.
        /// </summary>
        /// <remarks>
        /// This endpoint allows the retrieval of users filtered by various criteria specified in the query parameters. 
        /// The criteria are provided as properties of the <see cref="UserQueryDTO"/> object.
        /// </remarks>
        /// <param name="model">
        /// A <see cref="UserQueryDTO"/> object containing the filtering criteria for the query.
        /// </param>
        /// <returns>
        /// Returns a filtered list of users as a collection of <see cref="UserListItemDTO"/> objects.
        /// </returns>
        /// <response code="200">The filtered list of users was successfully retrieved.</response>
        /// 
        [HttpGet]
        [Route("query")]
        [Produces(typeof(List<UserListItemDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> QueryUsers([FromQuery] UserQueryDTO model)
        {
            var user = GetUserContext();
            var result = await _userService.QueryUsersAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }
    }
}
