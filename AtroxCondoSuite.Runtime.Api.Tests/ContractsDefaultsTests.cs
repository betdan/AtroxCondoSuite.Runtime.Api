namespace AtroxCondoSuite.Runtime.Api.Tests
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Requests;
    using Xunit;

    public sealed class ContractsDefaultsTests
    {
        [Fact]
        public void ExecuteStoredProcedureRequest_ShouldHaveDefaultCollections()
        {
            var request = new ExecuteStoredProcedureRequest();

            Assert.NotNull(request.InputParameters);
            Assert.Empty(request.InputParameters);
            Assert.Equal(string.Empty, request.TenantId);
            Assert.Equal(string.Empty, request.DatabaseName);
            Assert.Equal(string.Empty, request.ProcedureName);
        }
    }
}
