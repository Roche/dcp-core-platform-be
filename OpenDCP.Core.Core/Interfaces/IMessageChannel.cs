using OpenDCP.Framework.Messaging.Message.MessageDelivery;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IMessageChannel
    {
        public Task SendMessageAsync(UserMessageDTO message);
        public Task<UserMessageDTO> FormatMessageAsync(UserMessageDTO message);
        public Task<List<UserMessageDTO>> Personalize(UserMessageDTO message);
        public Task<UserMessageDTO> ApplyTemplate(UserMessageDTO message);
    }
}
