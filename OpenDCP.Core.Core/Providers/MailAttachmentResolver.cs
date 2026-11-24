using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Framework.HttpClient;
using OpenDCP.Framework.Messaging.Message.MessageDelivery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Providers
{
    public class MailAttachmentResolver : IAttachmentResolver<Attachment>
    {
        private readonly IConfiguration _configuration;

        public MailAttachmentResolver(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<Attachment>> ResolveAsync(
            IEnumerable<UserMessageAttachmentDTO> attachments,
            ILogger logger)
        {
            var result = new List<Attachment>();

            foreach (var attachment in attachments)
            {
                var validation = attachment.Validate();

                if (validation.errors.Count > 0)
                {
                    logger.LogError("Skipping invalid attachment: {Validation}", validation);
                    continue;
                }

                if (validation.warnings.Count > 0)
                {
                    logger.LogWarning("Attachment warnings: {Validation}", string.Join(',', validation.warnings));
                }

                // Inline (embedded)
                if (attachment.IsInline)
                {
                    var inline = await CreateAttachmentAsync(attachment, inlineMode: true, logger);
                    if (inline != null) result.Add(inline);
                }
                else
                {
                    var fileAttachment = await CreateAttachmentAsync(attachment, inlineMode: false, logger);
                    if (fileAttachment != null) result.Add(fileAttachment);
                }
            }

            return result;
        }

        private async Task<Attachment?> CreateAttachmentAsync(
            UserMessageAttachmentDTO dto,
            bool inlineMode,
            ILogger logger)
        {
            try
            {
                byte[]? data = dto.Data;

                if ((data == null || data.Length == 0) && !string.IsNullOrWhiteSpace(dto.DataUrl))
                {
                    var httpClient = new DCPHttpClient(_configuration);
                    var httpClientResult = await httpClient.GetAsync(dto.DataUrl);

                    if (!httpClientResult.IsSuccessStatusCode)
                    {
                        var responseMessage =
                            $"Response status code does not indicate success: " +
                            $"{(int)httpClientResult.StatusCode} ({httpClientResult.StatusCode}).";

                        var content = await httpClientResult.Content.ReadAsStringAsync();
                        logger.LogError("Attachment fetch failed: {ResponseMessage} {Environment}{Content}",
                            responseMessage,
                            Environment.NewLine,
                            content
                        );
                        return null;
                    }

                    data = await httpClientResult.Content.ReadAsByteArrayAsync();
                }

                if (data == null || data.Length == 0) return null;

                var ms = new MemoryStream(data);

                var attachment = new Attachment(ms, dto.FileName ?? "attachment", dto.MediaType ?? "application/octet-stream");

                if (inlineMode)
                {
                    attachment.ContentId = dto.ReferenceId;
                    attachment.ContentType.Name = dto.FileName;
                    attachment.ContentDisposition!.Inline = true;
                    attachment.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                }

                return attachment;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Attachment creation failed: {Message}",
                    ex.Message
                );

                return null;
            }
        }
    }
}
