using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.UserClient;
using OpenDCP.Framework.Hosting.CustomAttributes;
using Microsoft.AspNetCore.Mvc;
using OpenDCP.Core.Core.Extensions;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Controller for managing user client configurations.
    /// </summary>
    /// <remarks>
    /// Provides endpoints to create, update, retrieve, and delete user client configurations.
    /// User clients represent entities or configurations that a user can use to access DCP data
    /// in third-party tools, software, or scripts. All operations are performed in the context
    /// of the currently authenticated user.
    /// </remarks>
    [Route("api/v2/user-client/")]
    public class UserClientController : BaseController
    {
        private readonly IUserClientService _userClientService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserClientController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="userClientService">The service for managing user clients.</param>
        public UserClientController(IConfiguration configuration, IUserClientService userClientService) : base(configuration)
        {
            _userClientService = userClientService;
        }

        /// <summary>
        /// Retrieves a list of all user clients associated with the current user.
        /// </summary>
        /// <remarks>
        /// This endpoint fetches the complete list of user client configurations for the authenticated user. 
        /// The user clients represent configurations or entities that the user has access to or is associated with,
        /// the user can use these clients to access data from DCP in third party tools/software/scripts
        /// </remarks>
        /// <returns>
        /// Returns a list of <see cref="UserClientConfigurationDTO"/> objects containing details of user clients.
        /// </returns>
        /// <response code="200">The list of user clients was successfully retrieved.</response>
        [HttpGet]
        [Route("list")]
        [Produces(typeof(List<UserClientConfigurationDTO>))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> GetAllUserClients()
        {
            var user = GetUserContext();
            var result = await _userClientService.GetAllUserClientsAsync(user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }


        /// <summary>
        /// Retrieves a list of all user clients associated with the current user.
        /// </summary>
        /// <remarks>
        /// This endpoint fetches the complete list of user client configurations for the authenticated user. 
        /// The user clients represent configurations or entities that the user has access to or is associated with,
        /// the user can use these clients to access data from DCP in third party tools/software/scripts
        /// </remarks>
        /// <returns>
        /// Returns a list of <see cref="UserClientConfigurationDTO"/> objects containing details of user clients.
        /// </returns>
        /// <response code="200">The list of user clients was successfully retrieved.</response>
        [HttpGet]
        [Route("{clientId}/get")]
        [Produces(typeof(UserClientConfigurationDTO))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> GetUserClient(int clientId)
        {
            var user = GetUserContext();
            var result = await _userClientService.GetUserClientAsync(clientId, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }

        /// <summary>
        /// Creates a new user client configuration for the current user.
        /// </summary>
        /// <remarks>
        /// This endpoint allows the creation of a new user client configuration by providing the necessary details in the request body. 
        /// The user client configuration includes various settings and preferences that are associated with a user.
        /// </remarks>
        /// <param name="model">The user client configuration data to create.</param>
        /// <returns>
        /// Returns a <see cref="UserClientConfigurationDTO"/> object that represents the newly created user client configuration.
        /// </returns>
        /// <response code="200">The user client was successfully created.</response>
        [HttpPost]
        [ActionName("CreateUserClient")]
        [Route("create")]
        [Produces(typeof(UserClientConfigurationDTO))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> CreateUserClient([FromBody] UserClientUserConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _userClientService.CreateUserClientAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }

        /// <summary>
        /// Updates an existing user client configuration.
        /// </summary>
        /// <remarks>
        /// This endpoint allows the update of an existing user client configuration by providing the updated details in the request body.
        /// The provided configuration data will replace the existing configuration for the specified user client.
        /// This can be used to update permissions, the state etc. of the client.
        /// </remarks>
        /// <param name="clientId">The Id of the user client to be updated.</param>
        /// <param name="model">The updated user client configuration data.</param>
        /// <returns>
        /// Returns the updated <see cref="UserClientConfigurationDTO"/> object representing the user client configuration after the update.
        /// </returns>
        /// <response code="200">The user client configuration was successfully updated.</response>
        [HttpPut]
        [ActionName("UpdateUserClient")]
        [Route("{clientId}/update")]
        [Produces(typeof(UserClientConfigurationDTO))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> UpdateUserClient(int clientId, [FromBody] UserClientUserConfigurationDTO model)
        {
            var user = GetUserContext();
            model.Id = clientId;
            var result = await _userClientService.UpdateUserClientAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }

        /// <summary>
        /// Updates the status of a user client.
        /// </summary>
        /// <remarks>
        /// This endpoint allows updating the status of a user client by providing the `clientId` and the new status value.
        /// The status of the user client will be updated based on the provided parameters.
        /// </remarks>
        /// <param name="clientId">The unique identifier of the user client whose status needs to be updated.</param>
        /// <param name="status">The new status to set for the user client. The status value is a byte where different values represent different states.</param>
        /// <returns>
        /// Returns a status of OK if the update is successful.
        /// </returns>
        /// <response code="200">The user client status was successfully updated.</response>
        [HttpPut]
        [ActionName("SetUserClientStatus")]
        [Route("{clientId}/status")]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> SetUserClientStatus([FromRoute] int clientId, [FromQuery] UserClientStatus status)
        {
            var user = GetUserContext();
            await _userClientService.SetUserClientStatusAsync(clientId, status, user);
            return Ok();
        }

        /// <summary>
        /// Deletes a user client based on the specified client ID.
        /// </summary>
        /// <remarks>
        /// This endpoint allows for the deletion of a user client by providing the `clientId` of the user client to be deleted.
        /// Once the client is deleted, all associated data will be removed, and the client will no longer exist in the system.
        /// </remarks>
        /// <param name="clientId">The unique identifier of the user client to be deleted.</param>
        /// <returns>
        /// Returns a status of OK if the deletion is successful.
        /// </returns>
        /// <response code="200">The user client was successfully deleted.</response>
        [HttpDelete]
        [ActionName("DeleteUserClient")]
        [Route("{clientId}/delete")]
        [Produces(typeof(UserClientConfigurationDTO))]
        public async Task<IActionResult> DeleteUserClient(int clientId)
        {
            var user = GetUserContext();
            await _userClientService.DeleteUserClientAsync(clientId, user);
            return this.Ok();
        }
    }
}
