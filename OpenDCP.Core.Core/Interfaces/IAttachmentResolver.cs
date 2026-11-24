using OpenDCP.Framework.Messaging.Message.MessageDelivery;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IAttachmentResolver<TAttachment>
    {
        Task<List<TAttachment>> ResolveAsync(
            IEnumerable<UserMessageAttachmentDTO> attachments,
            ILogger logger);
    }
}
