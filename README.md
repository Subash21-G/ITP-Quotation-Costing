# ITP Quotation backend

ASP.NET Core 10 API using Entity Framework Core and SQL Server.

## Run

Configure `ConnectionStrings:DefaultConnection` using local configuration or .NET user secrets. From the repository root:

```powershell
dotnet restore ITPQuotation.slnx
dotnet ef database update --project backend/ITPQuotation.Api
dotnet run --project backend/ITPQuotation.Api --launch-profile http
```

The HTTP profile listens on http://localhost:5263. Development OpenAPI is available at `/openapi/v1.json`.

The relationship migration requires existing RFQs to reference valid customers and existing items to reference valid RFQs. Correct any orphan records before applying it. Migrations are not applied automatically at startup.

## Endpoints

| Resource | Collection | Single record |
| --- | --- | --- |
| Customers | GET, POST `/api/customers` | GET, PUT, DELETE `/api/customers/{id}` |
| RFQs | GET, POST `/api/rfqs` | GET, PUT, DELETE `/api/rfqs/{id}` |
| RFQ items | GET, POST `/api/rfqs/{rfqId}/items` | GET, PUT, DELETE `/api/rfqs/{rfqId}/items/{id}` |
| Materials | GET, POST `/api/materials` | GET `/api/materials/{materialNo}`; PUT, DELETE `/api/materials/{id}` |
| Material routing | GET, POST `/api/materials/{id}/routing` | PUT, DELETE `/api/materials/{id}/routing/{routingId}` |
| Metals | GET, POST `/api/metals` | GET, PUT `/api/metals/{id}` |
| Processes | GET, POST `/api/processes` | GET, PUT `/api/processes/{id}` |
| Vendors | GET, POST `/api/vendors` | GET, PUT `/api/vendors/{id}` |
| Vendor process rates | GET, POST `/api/vendor-process-rates` | GET `/api/vendor-process-rates/{id}` |
| RFQ item master lookup | - | GET `/api/rfqs/{rfqId}/items/{itemId}/master` |

Filter RFQs with `?customerId=1`. Customer PUT retains the existing requirement that the body ID matches the URL ID. RFQ and item writes use request DTOs; IDs and creation timestamps are server controlled. Item IDs are scoped to their parent RFQ.

RFQ statuses: Draft (default), Submitted, Closed, Cancelled. Quantity must be positive, fit decimal(18,3), and have at most three decimal places. Customer deletion returns 409 when RFQs exist. RFQ deletion cascades to its items after the migration is applied.

Material numbers are unique business identifiers. Use `?search=` and `?isActive=` to filter materials, and `?isActive=` for metals, processes, and vendors. Routing sequences are unique per material and always returned in sequence order. Outsourced routes require a vendor; in-house routes do not accept one. Master relationships use restrictive deletes so referenced data cannot be removed accidentally.

The RFQ item master lookup is read-only. It returns `masterExists: false` when the item material number is not in the Mastersheet and never overwrites the RFQ item.

## Verification

```powershell
dotnet build ITPQuotation.slnx
dotnet test ITPQuotation.slnx
dotnet ef migrations has-pending-model-changes --project backend/ITPQuotation.Api
powershell -File scripts/Test-ApiValidation.ps1
powershell -File scripts/Test-Mastersheet.ps1
```

The xUnit project uses an isolated EF Core in-memory database and an in-process API host. Run the API before the PowerShell validation scripts. `Test-ApiValidation.ps1` checks rejected inputs. `Test-Mastersheet.ps1` exercises the Phase 1 APIs against the configured LocalDB database and removes its temporary records; it requires `sqlcmd` for final cleanup. Use a disposable database for verification before deployment.

## Current scope

Customer, RFQ, and Mastersheet management backend. No frontend, login, metal-weight calculation, pricing workflow, PDF upload/processing, or production deployment configuration is included yet. The PDF path model property is reserved and is not writable through the RFQ request DTO.
