using Microsoft.AspNetCore.Mvc;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Framework.Security.Models;
using UserQueryDTO = OpenDCP.Core.Models.Models.User.UserQueryDTO;

#pragma warning disable CS1591
#pragma warning disable CS1572
#pragma warning disable CS1573

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class ADUsersLegacyController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ILegacyService _legacy;
        public ADUsersLegacyController(IConfiguration configuration, IUserService userService, ILegacyService legacyService) : base(configuration)
        {
            _userService = userService;
            _legacy = legacyService;
        }

        /// <summary>
        /// Return users details by user ids - currently used by Microservice
        /// </summary>
        /// <param name="model">Users details parameters</param>
        /// <returns>Updated user detail</returns>
        [HttpPost]
        [ActionName("UserDetailsByUserIds")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/ADUsers/UserDetailsByUserIds")]
        [Route("ADUsers/UserDetailsByUserIds")]
        public async Task<IActionResult> UserDetailsByUserIds([FromBody] List<UserInfoDTO> legacyModel)
        {
            var user = GetUserContext();

            var model = new UserQueryDTO();

            if (legacyModel.Any(x => x.UserID > 0))
            {
                model.UserId = legacyModel
                    .Where(x => x.UserID > 0)
                    .Select(x => x.UserID)
                    .ToList();
            }

            if (legacyModel.Any(x => !string.IsNullOrEmpty(x.Email)))
            {
                model.Email = legacyModel
                    .Where(x => !string.IsNullOrEmpty(x.Email))
                    .Select(x => x.Email)
                    .ToList();
            }

            var result = await _userService.QueryUsersAsync(model, user);

            return this.Ok(await _legacy.MapUsersDetails(result));
        }
        /// <summary>
        /// Return users details by user ids - currently used by Microservice
        /// </summary>
        /// <param name="model">Users details parameters</param>
        /// <returns>Updated user detail</returns>
        [HttpGet]
        [ActionName("UserDetailsBySiteId")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/ADUsers/UserDetailsBySiteId")]
        [Route("ADUsers/UserDetailsBySiteId")]
        public async Task<IActionResult> UserDetailsBySiteId(int siteId, string module = "")
        {
            var user = GetUserContext();

            var model = new UserQueryDTO
            {
                SiteId = new List<int> { siteId },
            };

            var result = await _userService.QueryUsersAsync(model, user);

            return this.Ok(await _legacy.MapUsersDetails(result, true));
        }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS1572
#pragma warning restore CS1573
