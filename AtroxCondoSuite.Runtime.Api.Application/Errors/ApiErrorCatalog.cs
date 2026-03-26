namespace AtroxCondoSuite.Runtime.Api.Application.Errors
{
    public static class ApiErrorCatalog
    {
        public static readonly ApiErrorDefinition StoredProcedureNotAuthorized =
            new("SP_NOT_AUTHORIZED", "Stored procedure execution is not authorized.");

        public static readonly ApiErrorDefinition InvalidRequest =
            new("INVALID_REQUEST", "The request payload is invalid.");
    }
}

