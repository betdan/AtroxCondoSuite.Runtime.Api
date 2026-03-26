namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Observability
{
    using System.Diagnostics;
    using Prometheus;

    public sealed class AtroxCondoSuiteDiagnostics
    {
        public const string ActivitySourceName = "AtroxCondoSuite.Runtime.Api";

        public AtroxCondoSuiteDiagnostics()
        {
            ActivitySource = new ActivitySource(ActivitySourceName);
            DatabaseCallsTotal = Metrics.CreateCounter("atrox_condosuite_database_calls_total", "Total stored procedure calls executed.");
            DatabaseFailuresTotal = Metrics.CreateCounter("atrox_condosuite_database_failures_total", "Total stored procedure execution failures.");
            StoredProcedureResultSets = Metrics.CreateHistogram("atrox_condosuite_resultsets_count", "Number of result sets returned by stored procedure execution.");
        }

        public ActivitySource ActivitySource { get; }
        public Counter DatabaseCallsTotal { get; }
        public Counter DatabaseFailuresTotal { get; }
        public Histogram StoredProcedureResultSets { get; }
    }
}

