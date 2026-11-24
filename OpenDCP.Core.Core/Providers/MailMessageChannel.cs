using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Models.User;
using OpenDCP.Framework.AuditTrail.Constants;
using OpenDCP.Framework.Configuration.Model.Constants.MessageDelivery;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Exceptions;
using OpenDCP.Framework.HttpClient;
using OpenDCP.Framework.Messaging.Message.MessageDelivery;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace OpenDCP.Core.Core.Providers
{
    public class MailMessageChannel : IMessageChannel
    {
        private readonly IConfiguration _configuration;
        private readonly DCPCoreDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly IServiceDiscovery _serviceDiscovery;
        public MailMessageChannel(IConfiguration configuration, DCPCoreDbContext dbContext, ILoggerFactory factory, IServiceDiscovery serviceDiscovery)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = factory.CreateLogger<MailMessageChannel>();
            _serviceDiscovery = serviceDiscovery;
        }

        public async Task<List<UserMessageDTO>> Personalize(UserMessageDTO message)
        {
            var result = new List<UserMessageDTO>();
            var userInfos = await GetUserInformation(message.UserIds.Distinct().ToList());

            foreach (var userId in message.UserIds.Distinct())
            {
                var userInfo = userInfos.FirstOrDefault(u => u.Id == userId);

                var parameters = new Dictionary<string, string>
                {
                    {"USERDISPLAYNAME",  userInfo?.DisplayName ?? "Support Team"}
                };

                var personalizedMessage = new UserMessageDTO
                {
                    Subject = message.Subject,
                    Message = message.Message,
                    Format = message.Format,
                    MessageTemplate = message.MessageTemplate,
                    Attachments = new List<UserMessageAttachmentDTO>(message.Attachments),
                    UserIds = new List<int> { userId },
                    FormattedMessage = ReplaceTemplatePlaceholders(
                        message.FormattedMessage,
                        parameters
                    )
                };

                personalizedMessage.UserIds = new List<int> { userId };
                personalizedMessage.FormattedMessage = ReplaceTemplatePlaceholders(
                    message.FormattedMessage,
                    parameters
                );

                result.Add(personalizedMessage);
            }

            return result;
        }

        public async Task<UserMessageDTO> ApplyTemplate(UserMessageDTO message)
        {
            if (string.IsNullOrEmpty(message.FormattedMessage))
            {
                if (message.Message is null)
                    throw new ArgumentNullException(nameof(message));

                var logo = await GetLogoAsync();

                switch (message.Format)
                {
                    case MessageDeliveryMessageFormat.Text:
                        message.FormattedMessage = message.Message;
                        break;
                    case MessageDeliveryMessageFormat.Json:
                        message.FormattedMessage = message.Message;
                        break;
                    case MessageDeliveryMessageFormat.HTML:

                        var template = ReadTemplate(message.MessageTemplate);

                        UserMessageAttachmentDTO? logoAttachment = null;

                        if (template.Contains("DCPLOGO"))
                        {
                            logoAttachment = new UserMessageAttachmentDTO
                            {
                                Data = Convert.FromBase64String(logo),
                                FileName = "logo.png",
                                MediaType = Image.Png,
                                IsInline = true,
                                ReferenceId = "logoImage"
                            };

                            message.Attachments.Add(logoAttachment);
                        }

                        message.FormattedMessage = ReplaceTemplatePlaceholders(
                            template,
                            new Dictionary<string, string>
                            {
                                {"MESSAGEBODY",  message.Message},
                                {"DCPLOGO", "<img src=\"cid:logoImage\" alt=\"Logo\" style=\"max-width:200px;\" />"}
                            });
                        break;
                    default:
                        break;
                }
            }

            return message;
        }

        public Task<UserMessageDTO> FormatMessageAsync(UserMessageDTO message)
        {
            return Task.FromResult(message);
        }

        public async Task SendMessageAsync(UserMessageDTO message)
        {
            var addresses = await GetEmails(message.UserIds);

            using var mail = new MailMessage
            {
                Subject = message.Subject,
                From = new MailAddress(GetSender())
            };

            foreach (var address in addresses)
            {
                mail.To.Add(new MailAddress(address));
            }

            // Resolve attachments
            var resolver = new MailAttachmentResolver(_configuration);
            var resolvedAttachments = await resolver.ResolveAsync(message.Attachments, _logger);

            foreach (var att in resolvedAttachments)
            {
                if (att.ContentDisposition == null || !att.ContentDisposition.Inline)
                {
                    mail.Attachments.Add(att);
                }
            }

            if (message.Format == MessageDeliveryMessageFormat.HTML)
            {
                var htmlView = AlternateView.CreateAlternateViewFromString(
                    message.FormattedMessage,
                    Encoding.UTF8,
                    MediaTypeNames.Text.Html
                );

                foreach (var att in resolvedAttachments)
                {
                    if (att.ContentDisposition?.Inline == true && !string.IsNullOrEmpty(att.ContentId))
                    {
                        var linkedResource = new LinkedResource(att.ContentStream, att.ContentType.MediaType)
                        {
                            ContentId = att.ContentId.Trim('<', '>'),
                            TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                        };

                        htmlView.LinkedResources.Add(linkedResource);
                    }
                }

                mail.AlternateViews.Add(htmlView);
            }
            else
            {
                // Plain text fallback
                mail.Body = message.FormattedMessage;
            }

            using var client = GetConfiguredSmtpClient();

            await client.SendMailAsync(mail);
        }

        private async Task<List<UserListItemDTO>> GetUserInformation(List<int> userIds)
        {
            var userInfo = await _dbContext.User
                .Where(x => userIds.Contains(x.Id))
                .Select(x => new UserListItemDTO
                {
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    Id = x.Id,
                    UnixId = x.UnixId,
                })
                .AsNoTracking()
                .ToListAsync();

            return userInfo;
        }

        private async Task<List<string>> GetEmails(List<int> userIds)
        {
            var mails = await _dbContext.User
                .Where(x => userIds.Contains(x.Id))
                .Select(x => x.Email)
                .AsNoTracking()
                .ToListAsync();

            if (userIds.Contains((int)SystemActions.System))
            {
                var mailbox = _configuration["SupportInfo:Settings:Mailbox"];

                if (mailbox is null)
                    throw new DCPInternalServerErrorException("Missing mailbox configuration");

                mails.Add(mailbox);
            }

            mails.RemoveAll(s => string.IsNullOrWhiteSpace(s));

            return mails;
        }

        private string GetSender()
        {
            var messageChannels = _configuration
                .GetSection("MessageChannels")
                .GetChildren();

            var smtpChannel = messageChannels
                .FirstOrDefault(channel => channel.GetSection("Driver").Value == "smtp");

            if (smtpChannel == null)
            {
                throw new DCPInternalServerErrorException("No SMTP configuration not found");
            }

            var sender = smtpChannel["Settings:Sender"];

            if (sender == null)
            {
                throw new DCPInternalServerErrorException("Sender configuration missing in SMTP configuration");
            }

            return sender;

        }

        private SmtpClient GetConfiguredSmtpClient()
        {
            var messageChannels = _configuration
                .GetSection("MessageChannels")
                .GetChildren();

            var smtpChannel = messageChannels
                .FirstOrDefault(channel => channel.GetSection("Driver").Value == "smtp");

            if (smtpChannel == null)
            {
                throw new DCPInternalServerErrorException("No SMTP configuration not found");
            }

            if (!smtpChannel.Equals(default(JsonElement)))
            {
                var host = smtpChannel["Settings:Host"];
                var portString = smtpChannel["Settings:Port"];

                if (string.IsNullOrWhiteSpace(host))
                    throw new DCPInternalServerErrorException("SMTP host is not configured.");

                if (!int.TryParse(portString, out var port))
                    throw new DCPInternalServerErrorException($"Invalid SMTP port configuration: '{portString}'.");

                SmtpClient client = new SmtpClient
                {
                    Host = host,
                    Port = port,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    EnableSsl = true,
                };

                return client;
            }
            else
            {
                throw new DCPInternalServerErrorException("No smtp configuration found");
            }
        }
        private async Task<string> GetLogoAsync()
        {
            string env = _configuration["Host:EnvironmentSlug"] ?? "dev";

            List<string> list = new List<string> { "backend", "core", };

            var platformUrl = (await _serviceDiscovery.QueryActiveServices(list, env))
                .Select((RegisteredService x) => $"https://{x.Host}")
                .FirstOrDefault();

            var httpClient = new DCPHttpClient(_configuration);

            string logo = string.Empty;

            try
            {
                var httpClientResult = await httpClient.GetAsync(platformUrl + "/app-framework/assets/images/dcp-logo.png");

                if (!httpClientResult.IsSuccessStatusCode)
                {
                    var responseMessage =
                        $"Response status code does not indicate success: " +
                        $"{(int)httpClientResult.StatusCode} ({httpClientResult.StatusCode}).";

                    var content = await httpClientResult.Content.ReadAsStringAsync();
                    _logger.LogError("Request to DCP logo failed: {ResponseMessage} {Environment}{Content}",
                        responseMessage,
                        Environment.NewLine,
                        content
                    );
                }
                else
                {
                    logo = Convert.ToBase64String(await httpClientResult.Content.ReadAsByteArrayAsync());
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Request to DCP logo failed :" + ex.Message + ", InnerException ex:" + ex.GetOriginalException()?.Message + ", stackTrace :" + ex.StackTrace);
            }

            return logo;
        }

        private static string ReadTemplate(string templateName)
        {
            string templateFilePath = Path.Combine(AppContext.BaseDirectory, "Templates", templateName);

            try
            {
                if (!File.Exists(templateFilePath))
                {
                    throw new FileNotFoundException($"The template file '{templateFilePath}' was not found.");
                }

                string fileContents = File.ReadAllText(templateFilePath);

                return fileContents;
            }
            catch (FileNotFoundException ex)
            {
                throw new DCPInternalServerErrorException(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new DCPForbiddenException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new DCPInternalServerErrorException(ex.Message);
            }
        }

        private static string ReplaceTemplatePlaceholders(string skeleton, Dictionary<string, string> parameters)
        {
            foreach (var param in parameters)
            {
                skeleton = skeleton.Replace($"{{{{{param.Key}}}}}", param.Value);
            }
            return skeleton;
        }
    }
}
