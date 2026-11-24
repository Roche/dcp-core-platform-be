using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Framework.Hosting.CustomAttributes;
using OpenDCP.Framework.ShareLink.Models;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Provides operations for managing share links and share link aliases.
    /// </summary>
    /// <remarks>
    /// This controller allows users and the system to create, update, query, and delete share links and their aliases.
    /// Endpoints differentiate between system-generated and user-generated share links.
    /// </remarks>
    [Route("api/v2/share-link/")]
    public class ShareLinkController : BaseController
    {
        private readonly IShareLinkService _shareLinkService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShareLinkController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="shareLinkService">The service used for managing share links.</param>
        public ShareLinkController(IConfiguration configuration, IShareLinkService shareLinkService) : base(configuration)
        {
            _shareLinkService = shareLinkService;
        }

        /// <summary>
        /// Creates a system-generated share link with the specified configuration.
        /// </summary>
        /// <param name="model">The configuration data for the share link to be created.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the result of the share link creation operation.
        /// </returns>
        /// <remarks>
        /// This endpoint allows the system to generate a new share link with the provided configuration. 
        /// </remarks>
        [HttpPost]
        [ActionName("CreateSystemGeneratedSharedLink")]
        [Route("system/create")]
        [Produces(typeof(ShareLinkDTO))]
        public async Task<IActionResult> CreateSystemShareLink([FromBody] ShareLinkConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.CreateSystemShareLinkAsync(new List<ShareLinkConfigurationDTO> { model }, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result.FirstOrDefault());
        }

        /// <summary>
        /// Creates a system-generated share link with the specified configuration.
        /// </summary>
        /// <param name="model">The configuration data for the share link to be created.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the result of the share link creation operation.
        /// </returns>
        /// <remarks>
        /// This endpoint allows the system to generate a new share link with the provided configuration. 
        /// </remarks>
        [HttpPost]
        [ActionName("BulkCreateSystemGeneratedSharedLink")]
        [Route("system/bulk/create")]
        [Produces(typeof(List<ShareLinkDTO>))]
        public async Task<IActionResult> CreateSystemShareLink([FromBody] List<ShareLinkConfigurationDTO> model)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.CreateSystemShareLinkAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Creates a user-generated share link for the current user with the specified configuration.
        /// </summary>
        /// <param name="model">The configuration data for the share link to be created.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the result of the share link creation operation.
        /// </returns>
        /// <remarks>
        /// This endpoint allows the system to generate a new share link with the provided configuration. 
        /// The created share link will be associated with the current user.
        /// </remarks>
        [HttpPost]
        [ActionName("CreateUserGeneratedSharedLink")]
        [Route("user/create")]
        [Produces(typeof(ShareLinkDTO))]
        public async Task<IActionResult> CreateUserShareLink([FromBody] ShareLinkConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.CreateUserShareLinkAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Creates a new alias for a share link with the specified configuration.
        /// </summary>
        /// <param name="model">The configuration data for the share link alias to be created.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the result of the share link alias creation operation, 
        /// including the created alias data in the response.
        /// </returns>
        /// <remarks>
        /// This endpoint allows the creation of a new alias for an existing share link. The alias is associated 
        /// with the current user, and the created alias is returned as part of the response.
        /// </remarks>
        [HttpPost]
        [ActionName("CreateSharedLinkAlias")]
        [Route("alias/create")]
        [Produces(typeof(ShareLinkAliasDTO))]
        public async Task<IActionResult> CreateShareLinkAlias([FromBody] ShareLinkAliasConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.CreateShareLinkAliasAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all share links associated with the specified site
        /// </summary>
        /// <param name="siteId">The ID of the site for which to retrieve share links.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a list of <see cref="ShareLinkListItemDTO"/> objects representing the user's share links.
        /// </returns>
        /// <remarks>
        /// This endpoint provides a list of share links created by the user for the specified site.
        /// </remarks>
        [HttpGet]
        [ActionName("GetAllShareLinks")]
        [Route("user/list")]
        [Produces(typeof(List<ShareLinkListItemDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetAllUserShareLinks(int siteId)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.GetUserShareLinksAsync(siteId, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }

        /// <summary>
        /// Retrieves all share links alias associated with the specified site
        /// </summary>
        /// <param name="siteId">The ID of the site for which to retrieve share links alias.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a list of <see cref="ShareLinkAliasDTO"/> objects representing the user's share links.
        /// </returns>
        /// <remarks>
        /// This endpoint provides a list of share links alias created by the user for the specified site.
        /// </remarks>
        [HttpGet]
        [ActionName("GetAllSharedLinksAlias")]
        [Route("alias/query")]
        [Produces(typeof(List<ShareLinkAliasDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetAllSharedLinksAlias(int siteId)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.SearchLinkAliasAsync(new List<int?> { siteId }, null, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }

        /// <summary>
        /// Searches for share links based on the specified query parameters for the current user.
        /// </summary>
        /// <param name="model">The query model containing the search criteria for the share links.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a list of <see cref="ShareLinkDTO"/> objects that match the query criteria.
        /// </returns>
        /// <remarks>
        /// This endpoint allows users to search for share links based on various query parameters provided in the request.
        /// </remarks>
        [HttpGet]
        [ActionName("GetShareLink")]
        [Route("user/query")]
        [Produces(typeof(List<ShareLinkDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> SearchShareLink([FromQuery] ShareLinkQueryDTO model)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.SearchShareLinkAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }
        /// <summary>
        /// Searches for share links based on the specified query parameters for the current user.
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="alias">The query model containing the search criteria for the share link alias.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a list of <see cref="ShareLinkListItemDTO"/> objects that match the query criteria.
        /// </returns>
        /// <remarks>
        /// This endpoint allows users to search for share links alias based on alias name provided in the request.
        /// </remarks>
        [HttpGet]
        [ActionName("GetAlias")]
        [Route("user/alias/query")]
        [Produces(typeof(ShareLinkListItemDTO))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> SearchAlias(string alias, int? siteId = null)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.SearchAliasAsync(alias, user, siteId);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }

        /// <summary>
        /// Updates the share link associated with the specified GUID.
        /// </summary>
        /// <param name="model">The updated configuration data for the share link.</param>
        /// <param name="guid">The GUID of the share link to be updated.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the result of the share link update operation, 
        /// including the updated share link data in the response.
        /// </returns>
        /// <remarks>
        /// This endpoint allows the update of an existing share link for a user. The share link is identified 
        /// by its GUID, and the updated configuration data is provided in the request body. The response contains 
        /// the updated share link information.
        /// </remarks>
        [HttpPut]
        [ActionName("UpdateUserShareLink")]
        [Route("user/{guid}/update")]
        [Produces(typeof(ShareLinkDTO))]
        public async Task<IActionResult> UpdateUserShareLink([FromBody] ShareLinkConfigurationUpdateDTO model, Guid guid)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.UpdateUserShareLinkAsync(model, guid, user);
            return this.Ok(result);
        }

        /// <summary>
        /// Updates the alias of an existing share link alias identified by the specified GUID.
        /// </summary>
        /// <param name="model">The updated alias configuration for the share link.</param>
        /// <param name="id">The ID of the share link alias to be updated.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the result of the share link alias update operation, 
        /// including the updated alias data in the response.
        /// </returns>
        /// <remarks>
        /// This endpoint allows the update of an existing alias for a share link. The alias is identified by its GUID,
        /// and the updated configuration data is provided in the request body. The response contains the updated alias information.
        /// </remarks>
        [HttpPut]
        [ActionName("UpdateShareLinkAlias")]
        [Route("alias/{id}/update")]
        [Produces(typeof(ShareLinkAliasDTO))]
        public async Task<IActionResult> UpdateShareLinkAlias([FromBody] ShareLinkAliasConfigurationDTO model, int id)
        {
            var user = GetUserContext();
            var result = await _shareLinkService.UpdateShareLinkAliasAsync(model, id, user);
            return this.Ok(result);
        }

        /// <summary>
        /// Deletes a share link associated with the specified GUID.
        /// </summary>
        /// <param name="guid">The unique identifier of the share link to delete.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
        /// <remarks>
        /// This endpoint is used to remove an existing share link by its GUID.
        /// </remarks>
        [HttpDelete]
        [ActionName("DeleteShareLink")]
        [Route("user/{guid}/delete")]
        public async Task<IActionResult> DeleteShareLink(Guid guid)
        {
            var user = GetUserContext();
            await _shareLinkService.DeleteShareLinkAsync(guid, user);
            return this.Ok();
        }

        /// <summary>
        /// Deletes a share link alias associated with the specified GUID.
        /// </summary>
        /// <param name="id">The unique identifier of the share link alias to delete.</param>
        /// <param name="guid">The unique identifier of the share link to delete.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
        /// <remarks>
        /// This endpoint is used to remove an existing share link alias by its GUID.
        /// </remarks>
        [HttpDelete]
        [ActionName("DeleteShareLinkAlias")]
        [Route("alias/{id}/{guid}/delete")]
        public async Task<IActionResult> DeleteShareLinkAlias(int id, Guid guid)
        {
            var user = GetUserContext();
            await _shareLinkService.DeleteShareLinkAliasAsync(id, guid, user);
            return this.Ok();
        }

    }
}
