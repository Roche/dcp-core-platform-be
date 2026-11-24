using OpenDCP.Framework.Messaging.Message.MessageDelivery;
using System.Collections.Generic;

namespace OpenDCP.Core.Core.Extensions
{
    public static class UserMessageAttachmentExtensions
    {
        public static (List<string> errors, List<string> warnings) Validate(this UserMessageAttachmentDTO? attachment)
        {
            // Initialize tuple with new lists
            var result = (errors: new List<string>(), warnings: new List<string>());

            if (attachment == null)
            {
                result.errors.Add("Attachment is null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(attachment.FileName)
                && string.IsNullOrWhiteSpace(attachment.DataUrl))
            {
                result.warnings.Add("Attachment has no FileName. Some channels may require it.");
            }

            if ((attachment.Data == null || attachment.Data.Length == 0)
                && string.IsNullOrWhiteSpace(attachment.DataUrl))
            {
                result.errors.Add("Attachment must contain either Data or DataUrl.");
            }

            if (attachment.Data != null && attachment.Data.Length > 0
                && string.IsNullOrWhiteSpace(attachment.MediaType))
            {
                result.errors.Add("MediaType is required when Data is provided.");
            }

            if (attachment.IsInline && string.IsNullOrWhiteSpace(attachment.ReferenceId))
            {
                result.errors.Add("Inline attachments must specify a ReferenceId.");
            }

            return result;
        }


        public static IEnumerable<(List<string> errors, List<string> warnings)> ValidateAll(this IEnumerable<UserMessageAttachmentDTO> attachments)
        {
            foreach (var attachment in attachments)
            {
                yield return attachment.Validate();
            }
        }
    }
}
