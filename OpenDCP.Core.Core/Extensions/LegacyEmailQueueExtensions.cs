using OpenDCP.Core.Models.Legacy.Common;
using OpenDCP.Framework.Messaging.Message.MessageDelivery;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenDCP.Core.Core.Extensions
{
    public static class LegacyEmailQueueExtensions
    {
        public static List<UserMessageAttachmentDTO> ToUserMessageAttachments(this List<LegacyAttachment> legacyAttachments)
        {
            var result = new List<UserMessageAttachmentDTO>();

            if (legacyAttachments == null || legacyAttachments.Count == 0)
                return result;

            foreach (var legacy in legacyAttachments.Where(a => a != null))
            {
                byte[]? data = null;

                if (!string.IsNullOrWhiteSpace(legacy.Data))
                {
                    try
                    {
                        data = Convert.FromBase64String(legacy.Data);
                    }
                    catch
                    {
                        data = null;
                    }
                }

                var dto = new UserMessageAttachmentDTO
                {
                    FileName = legacy.FileName ?? Guid.NewGuid().ToString(),
                    MediaType = string.IsNullOrWhiteSpace(legacy.MediaType)
                        ? "application/octet-stream"
                        : legacy.MediaType,
                    Data = data,
                    IsInline = legacy.IsEmbedded,
                    ReferenceId = legacy.ContentId.ToString()
                };

                result.Add(dto);
            }

            return result;
        }
    }
}
