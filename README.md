# AtroxCondoSuite.Runtime.Api

Serverless runtime API for executing SQL Server stored procedures via HTTP and SQS/SNS.

## What it does
- Receives HTTP requests and executes stored procedures.
- Enforces execution policies (whitelist) from `security.StoredProcedureExecutionPolicies`.
- Writes execution audit using database-side procedures.
- Supports external cache (Valkey/Redis/ElastiCache) for policy and parameter metadata.

## Solution layout
- `AtroxCondoSuite.Runtime.Api/` Web API (Lambda-ready).
- `AtroxCondoSuite.Runtime.Api.Application/` Application services.
- `AtroxCondoSuite.Runtime.Api.Application.Contracts/` Request/response contracts.
- `AtroxCondoSuite.Runtime.Api.Domain/` Domain models.
- `AtroxCondoSuite.Runtime.Api.DataAccess/` SQL Server access.
- `AtroxCondoSuite.Runtime.Api.DataAccess.Contracts/` Data access interfaces.
- `AtroxCondoSuite.Runtime.Api.CrossCutting/` Configuration, metrics, logging.
- `AtroxCondoSuite.Runtime.Api.Tests/` Unit tests.

## Run locally
```bash
dotnet run --project "AtroxCondoSuite.Runtime.Api/AtroxCondoSuite.Runtime.Api.csproj" --urls "http://localhost:5000"
```

## Required HTTP headers
These are required by API Gateway and the runtime:
- `x-transaction-id` (GUID)
- `x-session-id` (GUID)
- `x-channel`
- `x-user-id`
- `x-client-ip`

## Sample request
See `requests/curl_execute_audit_LogExecutionStart.sh`.

## Configuration highlights
`AtroxCondoSuite.Runtime.Api/appsettings.json`
- `RdsSqlServer`: SQL Server connection.
- `StoredProcedureSecurity`: whitelist source and cache TTL.
- `ExternalCache`: `None`, `Redis`, or `ElastiCache`.
- `Observability`: metrics and logging flags.

## Caching behavior
- Whitelist decisions and stored procedure parameter metadata are cached on first use (lazy).
- If cache is unavailable, the runtime continues without it.

## Deployment (SAM)
See the `Deploy/` folder:
- `lambda.yaml`: one Lambda per schema.
- `api-gateway.yaml`: HTTP API routes per schema.

## Notes
- Stored procedures follow `@i_` for input, `@o_` for output, and `@i_debug` for debug prints.
- Debug prints are emitted only when `@i_debug = 1`.
