namespace AtroxCondoSuite.Runtime.Api.DataAccess.Contracts.Connections
{
    using Microsoft.Data.SqlClient;
    using System.Data;

    public interface ISqlServerDatabase : IDisposable
    {
        Task<(List<DataTable> resultSets, Dictionary<string, object> outputParameters, List<string> printMessages, int returnValue, Dictionary<int, string> rErrors)> ExecuteStoredProcedureAsync(string databaseName, string procedureName, Dictionary<string, object> inputParams = null, CancellationToken cancellationToken = default);

        Task<SqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default);
    }
}

