using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Core.Providers;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Framework.Configuration.Model.Constants.MessageDelivery;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace OpenDCP.Core.Core.Factories
{
    public class MessageChannelFactory
    {
        private readonly IConfiguration _configuration;
        private readonly DCPCoreDbContext _dbContext;
        private readonly ILoggerFactory _factory;
        private readonly IServiceDiscovery _serviceDiscovery;
        public MessageChannelFactory(IConfiguration configuration, DCPCoreDbContext dbContext, ILoggerFactory factory, IServiceDiscovery serviceDiscovery)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _factory = factory;
            _serviceDiscovery = serviceDiscovery;
        }

        public IMessageChannel Load(MessageDeliveryChannelType type)
        {
            switch (type)
            {
                case MessageDeliveryChannelType.Mail:
                    return new MailMessageChannel(_configuration, _dbContext, _factory, _serviceDiscovery);
                default:
                    throw new DCPBadRequestException(ErrorMessages.InvalidMessageDeliveryChannel);
            }
        }
    }
}
