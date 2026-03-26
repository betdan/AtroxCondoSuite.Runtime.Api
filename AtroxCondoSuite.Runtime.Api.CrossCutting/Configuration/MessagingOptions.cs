namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration
{
    public sealed class MessagingOptions
    {
        public const string SectionName = "Messaging";

        public SqsOptions Sqs { get; set; } = new();
        public SnsOptions Sns { get; set; } = new();
    }

    public sealed class SqsOptions
    {
        public string QueueUrl { get; set; } = string.Empty;
        public int MaxMessagesPerBatch { get; set; } = 10;
    }

    public sealed class SnsOptions
    {
        public string TopicArn { get; set; } = string.Empty;
        public string SubjectPrefix { get; set; } = "AtroxCondoSuite";
    }
}

