# AtroxCondoSuite.Runtime.Api

Runtime serverless para ejecutar Stored Procedures de SQL Server vía HTTP y SQS/SNS.

## Qué hace
- Recibe solicitudes HTTP y ejecuta procedimientos almacenados.
- Aplica políticas de ejecución (whitelist) desde `security.StoredProcedureExecutionPolicies`.
- Registra auditoría usando procedimientos del esquema `audit`.
- Soporta cache externo (Valkey/Redis/ElastiCache) para políticas y metadatos.

## Estructura de solución
- `AtroxCondoSuite.Runtime.Api/` Web API (lista para Lambda).
- `AtroxCondoSuite.Runtime.Api.Application/` Servicios de aplicación.
- `AtroxCondoSuite.Runtime.Api.Application.Contracts/` Contratos de request/response.
- `AtroxCondoSuite.Runtime.Api.Domain/` Modelos de dominio.
- `AtroxCondoSuite.Runtime.Api.DataAccess/` Acceso a SQL Server.
- `AtroxCondoSuite.Runtime.Api.DataAccess.Contracts/` Interfaces de data access.
- `AtroxCondoSuite.Runtime.Api.CrossCutting/` Configuración, métricas, logging.
- `AtroxCondoSuite.Runtime.Api.Tests/` Pruebas unitarias.

## Ejecutar localmente
```bash
dotnet run --project "AtroxCondoSuite.Runtime.Api/AtroxCondoSuite.Runtime.Api.csproj" --urls "http://localhost:5000"
```

## Headers obligatorios
Requeridos por API Gateway y por el runtime:
- `x-transaction-id` (GUID)
- `x-session-id` (GUID)
- `x-channel`
- `x-user-id`
- `x-client-ip`

## Configuración clave
`AtroxCondoSuite.Runtime.Api/appsettings.json`
- `RdsSqlServer`: conexión a SQL Server.
- `StoredProcedureSecurity`: fuente de whitelist y TTL de cache.
- `ExternalCache`: `None`, `Redis` o `ElastiCache`.
- `Observability`: métricas y logging.

## Comportamiento de cache
- Las decisiones de whitelist y los parámetros del SP se cachean al primer uso (lazy).
- Si el cache falla, el runtime continúa sin cache.

