using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Legacy;
using OpenDCP.Core.Models.Legacy.Site;
using OpenDCP.Framework.ShareLink.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

#pragma warning disable CS1591
#pragma warning disable CS1572
#pragma warning disable CS1573

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class SharedLinkLegacyController : BaseController
    {
        private readonly IShareLinkService _shareLinkService;
        private readonly ILegacyService _legacyService;
        public SharedLinkLegacyController(
            IConfiguration configuration,
            IShareLinkService shareLinkService,
            ILegacyService legacyService) : base(configuration)
        {
            _shareLinkService = shareLinkService;
            _legacyService = legacyService;
        }

        /// <summary>
        /// Get shared per given GUID
        /// </summary>
        /// <param name="guid">guid</param>
        /// <returns>Builded webIds used into particular global filter</returns>
        [HttpGet]
        [ActionName("GetSharedLink")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/GetSharedLink")]
        [Route("SharedLink/GetSharedLink")]
        public async Task<IActionResult> GetSharedLink(Guid guid)
        {
            var user = GetUserContext();

            var model = new ShareLinkQueryDTO
            {
                Guid = new List<Guid>
                {
                    guid
                }
            };

            var result = await _shareLinkService.SearchShareLinkAsync(model, user);

            if (result.FirstOrDefault() is null)
            {
                return Ok();

            }
            else
            {
                return Ok(result[0].Link);
            }
        }

        /// <summary>
        /// Create shared link with current global filter selected values
        /// </summary>
        /// <param name="model">Required - Link and Title</param>
        /// <returns>Global filter link guid(generated)</returns>
        [HttpPost]
        [ActionName("CreateSharedLink")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/CreateSharedLink")]
        [Route("SharedLink/CreateSharedLink")]
        public async Task<IActionResult> CreateSharedLink([FromBody] LegacySharedLinkDTO legacyModel)
        {
            var user = GetUserContext();

            var model = new ShareLinkConfigurationDTO
            {
                Link = legacyModel.Link,
                Title = legacyModel.Title,
                SiteId = legacyModel.Link?["siteFilter"]?.GetValue<int?>(),
            };

            var result = await _shareLinkService.CreateUserShareLinkAsync(model, user);

            legacyModel.Guid = result.Guid.ToString();
            legacyModel.UserIDCreated = result.LastModifiedBy;
            legacyModel.DateCreated = result.LastModifiedAt;

            return Ok(legacyModel);
        }

        /// <summary>
        /// Update shared link with current global filter selected values
        /// </summary>
        /// <param name="model">Required - Link and Title and GUID</param>
        /// <returns>Updated model</returns>
        [HttpPut]
        [ActionName("UpdateSharedLink")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/UpdateSharedLink")]
        [Route("SharedLink/UpdateSharedLink")]
        public async Task<IActionResult> UpdateSharedLink([FromBody] LegacySharedLinkDTO legacyModel)
        {
            var user = GetUserContext();

            var model = new ShareLinkConfigurationUpdateDTO
            {
                Title = legacyModel.Title,
                LinkGUID = Guid.Parse(legacyModel.Guid ?? throw new NullReferenceException("Missing GUID for shared link"))
            };

            var result = await _shareLinkService.UpdateUserShareLinkAsync(model, model.LinkGUID, user);

            legacyModel.Guid = result.Guid.ToString();
            legacyModel.UserIDUpdated = result.LastModifiedBy;
            legacyModel.DateUpdated = result.LastModifiedAt;

            return Ok(legacyModel);
        }

        /// <summary>
        /// Delete shared link entity
        /// </summary>
        /// <param name="guid">guid</param>
        /// <returns>Status code</returns>
        [HttpDelete]
        [ActionName("DeleteSharedLink")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/DeleteSharedLink")]
        [Route("SharedLink/DeleteSharedLink")]
        public async Task<IActionResult> DeleteSharedLink(string guid)
        {
            var user = GetUserContext();

            var guidParsed = Guid.Parse(guid ?? throw new NullReferenceException("Missing GUID for shared link"));

            await _shareLinkService.DeleteShareLinkAsync(guidParsed, user);

            return Ok();
        }

        /// <summary>
        /// Create system shared links when model notifications are triggered - currently used by Microservice
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Status code</returns>
        [HttpPost]
        [ActionName("CreateSystemGeneratedSharedLink")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SystemGeneratedSharedLink/CreateSystemGeneratedSharedLink")]
        [Route("SystemGeneratedSharedLink/CreateSystemGeneratedSharedLink")]
        public async Task<IActionResult> CreateSystemGeneratedSharedLink([FromBody] List<LegacySystemGeneratedShareLinkDTO> legacyModel)
        {
            var user = GetUserContext();

            var model = legacyModel.Select(x => new ShareLinkConfigurationDTO
            {
                Link = JsonObject.Parse(x.JLink ?? "{}").AsObject(),
                LinkGUID = x.LinkGUID,
                SiteId = x.SiteID,
            }).ToList();

            await _shareLinkService.CreateSystemShareLinkAsync(model, user);
            return this.Ok();
        }

        /// <summary>
        /// Get shared per given GUID
        /// </summary>
        /// <param name="guid">guid</param>
        /// <returns>Builded webIds used into particular global filter</returns>
        [HttpGet]
        [ActionName("GetAllSharedLinks")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/GetAllSharedLinks")]
        [Route("SharedLink/GetAllSharedLinks")]
        public async Task<IActionResult> GetAllSharedLinks()
        {
            var user = GetUserContext();
            var site = await _legacyService.GetUserCurrentSiteAsync(user, false);
            var result = await _shareLinkService.GetLegacyUserShareLinksAsync(site.SiteId, user);

            return Ok(result);
        }

        /// <summary>
        /// Get all shared links alias for current logged user
        /// </summary>
        /// <returns>List with links</returns>
        [HttpGet]
        [ActionName("GetAllSharedLinksAlias")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/GetAllSharedLinksAlias")]
        [Route("SharedLink/GetAllSharedLinksAlias")]
        public async Task<IActionResult> GetAllSharedLinksAlias()
        {
            var user = GetUserContext();
            var site = await _legacyService.GetUserCurrentSiteAsync(user, false);
            var result = await _shareLinkService.SearchLinkAliasAsync([site.SiteId], [user.UserId], user);

            var legacyResult = result.Select(x =>
                new LegacySharedLinkAliasDTO
                {
                    ID = x.Id,
                    Name = x.Name,
                    SharedLinkGuid = x.ShareLinkGuid,
                    UserIDCreated = x.OwnerUserId,
                    UserIDUpdated = x.LastModifiedBy,
                    DateUpdated = x.LastModifiedAt
                }).ToList();

            return Ok(legacyResult);
        }

        /// <summary>
        /// Create shared alias
        /// </summary>
        /// <param name="model">All properties are required</param>
        /// <returns>Global filter link guid(generated)</returns>
        [HttpPost]
        [ActionName("CreateShareLinkAlias")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/CreateShareLinkAlias")]
        [Route("SharedLink/CreateShareLinkAlias")]
        public async Task<IActionResult> CreateShareLinkAlias([FromBody] LegacySharedLinkAliasDTO legacyModel)
        {
            var user = GetUserContext();

            var model = new ShareLinkAliasConfigurationDTO
            {
                Name = legacyModel.Name!,
                ShareLinkGuid = legacyModel.SharedLinkGuid
            };

            var result = await _shareLinkService.CreateShareLinkAliasAsync(model, user);

            legacyModel.ID = result.Id;
            legacyModel.UserIDCreated = result.OwnerUserId;
            legacyModel.DateCreated = result.LastModifiedAt;

            return Ok(legacyModel);
        }

        /// <summary>
        /// Update shared alias
        /// </summary>
        /// <param name="model">All properties are required</param>
        /// <returns>Updated model</returns>
        [HttpPut]
        [ActionName("UpdateShareLinkAlias")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/UpdateShareLinkAlias")]
        [Route("SharedLink/UpdateShareLinkAlias")]
        public async Task<IActionResult> UpdateShareLinkAlias([FromBody] LegacySharedLinkAliasDTO legacyModel)
        {
            var user = GetUserContext();

            var model = new ShareLinkAliasConfigurationDTO
            {
                Name = legacyModel.Name!,
                ShareLinkGuid = legacyModel.SharedLinkGuid
            };

            var result = await _shareLinkService.UpdateShareLinkAliasAsync(model, legacyModel.ID!.Value, user);

            legacyModel.UserIDUpdated = result.LastModifiedBy;
            legacyModel.DateUpdated = result.LastModifiedAt;

            return Ok(legacyModel);
        }

        /// <summary>
        /// Delete shared link entity
        /// </summary>
        /// <param name="id">Share link alias id</param>
        /// <param name="sharedLinkId">Shared link Id</param>
        /// <returns>Status code</returns>
        [HttpDelete]
        [ActionName("DeleteShareLinkAlias")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/DeleteShareLinkAlias")]
        [Route("SharedLink/DeleteShareLinkAlias")]
        public async Task<IActionResult> DeleteShareLinkAlias(int id, Guid sharedLinkId)
        {
            var user = GetUserContext();

            await _shareLinkService.DeleteShareLinkAliasAsync(id, sharedLinkId, user);

            return Ok();
        }

        /// <summary>
        /// Get shared per aliasName
        /// </summary>
        /// <param name="aliasName">Alias name</param>
        /// <returns>Builded webIds used into particular global filter</returns>
        [HttpGet]
        [ActionName("GetShareLinkByAlias")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/SharedLink/GetShareLinkByAlias")]
        [Route("SharedLink/GetShareLinkByAlias")]
        public async Task<IActionResult> GetShareLinkByAlias(string aliasName)
        {
            var user = GetUserContext();

            var model = new ShareLinkQueryDTO
            {
                Alias = new List<string> { aliasName }
            };

            var result = await _shareLinkService.SearchShareLinkAsync(model, user);

            var shareLink = result.FirstOrDefault() ?? throw new NullReferenceException("Missing shared link");

            var legacyModel = new LegacySharedLinkDTO
            {
                Link = shareLink.Link,
                Guid = shareLink.Guid.ToString(),
                Title = shareLink.Title,
                ShareLinkAliasDetails = shareLink.Aliases.Select(a =>
                new LegacySharedLinkAliasDTO
                {
                    ID = a.Id,
                    Name = a.Alias,
                    SharedLinkGuid = shareLink.Guid
                }).ToList(),
                UserIDUpdated = shareLink.LastModifiedBy,
                DateUpdated = shareLink.LastModifiedAt
            };

            return this.Ok(legacyModel);
        }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS1572
#pragma warning restore CS1573