namespace AtroxCondoSuite.Runtime.Api.Infrastructure.Aws.Sns
{
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleNotificationService.Model;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Messaging;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Text.Json;

    public sealed class SnsNotificationPublisher(
        IAmazonSimpleNotificationService snsClient,
        IOptions<MessagingOptions> messagingOptions,
        ILogger<SnsNotificationPublisher> logger) : IAtroxCondoSuiteNotificationPublisher
    {
        private readonly IAmazonSimpleNotificationService _snsClient = snsClient ?? throw new ArgumentNullException(nameof(snsClient));
        private readonly MessagingOptions _messagingOptions = messagingOptions?.Value ?? throw new ArgumentNullException(nameof(messagingOptions));
        private readonly ILogger<SnsNotificationPublisher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task PublishAsync(ServiceResult payload, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_messagingOptions.Sns.TopicArn))
            {
                _logger.LogWarning("SNS TopicArn is not configured. Notification publish skipped.");
                return;
            }

            await _snsClient.PublishAsync(new PublishRequest
            {
                TopicArn = _messagingOptions.Sns.TopicArn,
                Subject = $"{_messagingOptions.Sns.SubjectPrefix}-execution-result",
                Message = JsonSerializer.Serialize(payload)
            }, cancellationToken);

            _logger.LogInformation("Published execution result to SNS topic {TopicArn}", _messagingOptions.Sns.TopicArn);
        }
    }
}

