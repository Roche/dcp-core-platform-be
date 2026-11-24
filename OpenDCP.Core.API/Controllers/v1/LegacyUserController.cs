using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Legacy;
using OpenDCP.Core.Models.Legacy.Site;
using OpenDCP.Core.Models.Models.Messages;
using OpenDCP.Framework.Security.Models;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1591
#pragma warning disable CS1572
#pragma warning disable CS1573

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class LegacyUserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ILegacyService _legacy;
        public LegacyUserController(IConfiguration configuration, IUserService userService, ILegacyService legacy) : base(configuration)
        {
            _userService = userService;
            _legacy = legacy;   
        }

        /// <summary>
        /// Get current logged user email address
        /// </summary>
        /// <returns>Current logged user email address</returns>
        [HttpGet]
        [ActionName("CurrentUserEmailAddress")]
        [Produces(typeof(string))]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/ADUsers/CurrentUserEmailAddress")]
        [Route("ADUsers/CurrentUserEmailAddress")]
        public async Task<IActionResult> CurrentUserEmailAddress()
        {
            var user = GetUserContext();
            var result = await _userService.GetUserInfoAsync(user);
            return this.Ok($"\"{result.Email}\"");
        }

        /// <summary>
        /// Update AD user - only update over timeFormat and dateFormat
        /// </summary>
        /// <returns>Updated user detail</returns>
        [HttpPut]
        [ActionName("UpdateADUserTimeFormat")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/ADUsers/UpdateADUserTimeFormat")]
        [Route("ADUsers/UpdateADUserTimeFormat")]
        public async Task<IActionResult> UpdateADUserTimeFormat([FromBody]UserInfoDTO legacyModel)
        {
            var user = GetUserContext();

            var model = new Models.Models.User.UserSettingsDTO
            {
                DateFormat = legacyModel.DateFormat,
                TimeFormat = legacyModel.TimeFormat,
            };

            await _userService.UpdateUserSettingsAsync(model, user);
            return Ok(legacyModel);
        }

        /// <summary>
        /// Get logged user site
        /// </summary>
        /// <param name="includeModules">Optional parameter</param>
        /// <returns>Site details</returns>
        [HttpGet]
        [ActionName("GetUserSite")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/ADUsers/GetUserSite")]
        [Route("ADUsers/GetUserSite")]
        public async Task<IActionResult> GetUserSite(bool includeModules = true)
        {
            var user = GetUserContext();

            var result = await _legacy.GetUserCurrentSiteAsync(user, includeModules);

            return this.Ok(result);
        }


        /// <summary>
        /// Sets logged user site
        /// </summary>
        [HttpPut]
        [ActionName("SetUserSite")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/ADUsers/SetUserSite")]
        [Route("ADUsers/SetUserSite")]
        [Produces(typeof(List<LegacyActiveModule>))]
        public async Task<IActionResult> SetUserSite(int siteID)
        {
            var user = GetUserContext();
            var result = await _legacy.SetUserSite(siteID, user);
            return this.Ok(result);
        }

        /// <summary>
        /// Send feedback from DCP user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("SendUserFeedBack")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/FeedBack/SendUserFeedBack")]
        [Route("FeedBack/SendUserFeedBack")]
        public async Task<IActionResult> SendUserFeedBack([FromBody] LegacyUserFeedbackDTO model)
        {
            var user = GetUserContext();

            var mModel = new UserFeedbackDTO
            {
                Body = model.UserFeedback,
                SendCopyToMyself = model.SendCopyToMyself != null & string.Equals(model.SendCopyToMyself!.ToString(), "true", StringComparison.InvariantCultureIgnoreCase),
                Subject = model.Subject,
                SystemModule = model.SystemModule,
            };

            await _userService.SendUserFeedbackAsync(mModel, user);
            return Ok();
        }

    }
}

#pragma warning restore CS1591
#pragma warning restore CS1572
#pragma warning restore CS1573
