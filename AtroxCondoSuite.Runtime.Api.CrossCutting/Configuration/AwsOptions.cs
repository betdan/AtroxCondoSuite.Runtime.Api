namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration
{
    public sealed class AwsOptions
    {
        public const string SectionName = "AWS";

        public string Region { get; set; } = "us-east-2";
        public string Profile { get; set; } = string.Empty;
        public bool UseEnvironmentCredentials { get; set; } = true;
    }
}

