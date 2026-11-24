using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Core.Services;
using OpenDCP.Core.Models.Models.Banner;
using OpenDCP.Framework.Hosting.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Provides endpoints for managing user information banners.
    /// </summary>
    /// <remarks>
    /// This controller exposes endpoints for retrieving, creating, updating,
    /// enabling/disabling, and deleting banners that are displayed to users.
    /// </remarks>
    [Route("api/v2/banner/")]
    public class BannerController : BaseController
    {
        private readonly IBannerService _bannerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BannerController"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration settings.</param>
        /// <param name="bannerService">Service responsible for banner management.</param>
        public BannerController(IConfiguration configuration, IBannerService bannerService) : base(configuration)
        {
            _bannerService = bannerService;
        }

        /// <summary>
        /// Retrieves a list of all banners including the configuration properties.
        /// </summary>
        /// <remarks>
        /// This endpoint returns a comprehensive list of all banners configured, 
        /// regardless of their active status. This is used by the admin to configure the banners.
        /// </remarks>
        /// <returns>
        /// A list of <see cref="BannerConfigurationDTO"/> objects representing all banners.
        /// </returns>
        /// <response code="200">Returns the list of all banners.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an unexpected error occurs on the server.</response>
        [HttpGet]
        [ActionName("GetAllUserInfoBanner")]
        [Route("list/all")]
        [Produces(typeof(List<BannerConfigurationDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetAllUserInfoBanner()
        {
            var user = GetUserContext();
            var result = await _bannerService.GetAllBannersAsync(user);
            return this.Ok(result);
        }

        /// <summary>
        /// Retrieves a list of active banners
        /// </summary>
        /// <remarks>
        /// This endpoint returns a list of banners that are active
        /// </remarks>
        /// <returns>
        /// A list of <see cref="BannerDisplayDTO"/> objects representing active banners.
        /// </returns>
        /// <response code="200">Returns the list of active banners.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an unexpected error occurs on the server.</response>
        [HttpGet]
        [AllowAnonymous]
        [ActionName("GetActiveUserInfoBanner")]
        [Route("list/active")]
        [Produces(typeof(List<BannerDisplayDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetActiveUserInfoBanner()
        {
            var result = await _bannerService.GetActiveBannersAsync();
            return this.Ok(result);
        }

        /// <summary>
        /// Creates a new user information banner.
        /// </summary>
        /// <param name="model">The banner configuration model containing the details of the banner to create.</param>
        /// <returns>The created banner configuration.</returns>
        /// <remarks>
        /// This endpoint allows creating a new user information banner.
        /// The `model` parameter includes the configuration details such as the title, message, and other properties.
        /// The operation requires user context and uses the banner service to perform the creation.
        /// </remarks>
        /// <response code="200">Returns the created banner configuration.</response>
        /// <response code="400">If the input model is invalid.</response>
        [HttpPost]
        [ActionName("CreateUserInfoBanner")]
        [Route("create")]
        [Produces(typeof(BannerConfigurationDTO))]
        public async Task<IActionResult> CreateUserInfoBanner([FromBody] BannerConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _bannerService.CreateBannerAsync(model, user);
            return Ok(result);
        }

        /// <summary>
        /// Updates the configuration of a banner.
        /// </summary>
        /// <param name="bannerId">The unique identifier of the banner to update.</param>
        /// <param name="model">The banner configuration model containing updated details.</param>
        /// <returns>The updated banner configuration.</returns>
        /// <remarks>
        /// This endpoint updates the configuration of a specific user information banner.
        /// The `model` parameter contains the updated configuration details, which include the `bannerId` set automatically by the method.
        /// The operation requires user context and uses the banner service to apply the changes.
        /// </remarks>
        /// <response code="200">Returns the updated banner configuration.</response>
        /// <response code="400">If the input model is invalid.</response>
        /// <response code="404">If the specified banner does not exist.</response>
        [HttpPut]
        [ActionName("UpdateUserInfoBanner")]
        [Route("{bannerId}/update")]
        [Produces(typeof(BannerConfigurationDTO))]
        public async Task<IActionResult> UpdateUserInfoBanner(int bannerId,[FromBody] BannerConfigurationDTO model)
        {
            var user = GetUserContext();
            model.Id = bannerId;
            var result = await _bannerService.UpdateBannerAsync(model, user);
            return this.Ok(result);
        }

        /// <summary>
        /// Updates the status of a banner.
        /// </summary>
        /// <param name="bannerId">The unique identifier of the banner to update.</param>
        /// <param name="enabled">A boolean indicating whether the banner should be enabled or disabled.</param>
        /// <returns>A status indicating whether the update was successful.</returns>
        /// <remarks>
        /// This endpoint allows updating the enabled/disabled status of a specific user information banner.
        /// The operation requires user context and uses the banner service to perform the update.
        /// </remarks>
        [HttpPut]
        [ActionName("SetUserInfoBannerStatus")]
        [Route("{bannerId}/status")]
        public async Task<IActionResult> SetUserInfoBannerStatus(int bannerId, bool enabled)
        {
            var user = GetUserContext();
            await _bannerService.SetBannerStatusAsync(bannerId, enabled, user);
            return this.Ok();
        }

        /// <summary>
        /// Deletes a banner by its Id.
        /// </summary>
        /// <param name="bannerId">The unique identifier of the banner to delete.</param>
        /// <returns>A status indicating whether the deletion was successful.</returns>
        /// <remarks>
        /// This endpoint deletes a user information banner specified by its unique ID.
        /// The operation requires user context and invokes the banner service to perform the deletion.
        /// </remarks>
        [HttpDelete]
        [ActionName("DeleteUserInfoBanner")]
        [Route("{bannerId}/delete")]
        public async Task<IActionResult> DeleteUserInfoBanner(int bannerId)
        {
            var user = GetUserContext();
            await _bannerService.DeleteBannerAsync(bannerId, user);
            return this.Ok();
        }
    }
}
