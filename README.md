# ITP Quotation

Quotation costing system with an ASP.NET Core 10 API, Entity Framework Core,
SQL Server, and an Angular Material frontend.

## Run

Configure `ConnectionStrings:DefaultConnection` using local configuration or .NET user secrets. From the repository root:

```powershell
dotnet restore ITPQuotation.slnx
dotnet ef database update --project backend/ITPQuotation.Api
dotnet run --project backend/ITPQuotation.Api --launch-profile http
```

The HTTP profile listens on http://localhost:5263. Development OpenAPI is available at `/openapi/v1.json`.

## Frontend

The Angular application is in `frontend/itp-quotation-ui`. It uses the
development proxy to send `/api` requests to the backend on port 5263.

```powershell
cd frontend/itp-quotation-ui
npm install
npm start
```

The development UI is available at http://localhost:4200. Production build and
unit-test commands are:

```powershell
npm run build
npm test -- --watch=false
```

The relationship migration requires existing RFQs to reference valid customers and existing items to reference valid RFQs. Correct any orphan records before applying it. Migrations are not applied automatically at startup.

## Endpoints

| Resource | Collection | Single record |
| --- | --- | --- |
| Customers | GET, POST `/api/customers` | GET, PUT, DELETE `/api/customers/{id}` |
| RFQs | GET, POST `/api/rfqs` | GET, PUT, DELETE `/api/rfqs/{id}` |
| RFQ items | GET, POST `/api/rfqs/{rfqId}/items` | GET, PUT, DELETE `/api/rfqs/{rfqId}/items/{id}` |
| RFQ PDF import | POST `/api/rfq-imports/extract` | POST `/api/rfq-imports/confirm` |
| Materials | GET, POST `/api/materials` | GET `/api/materials/{materialNo}`; PUT, DELETE `/api/materials/{id}` |
| Material routing | GET, POST `/api/materials/{id}/routing` | PUT, DELETE `/api/materials/{id}/routing/{routingId}` |
| Metals | GET, POST `/api/metals` | GET, PUT `/api/metals/{id}` |
| Processes | GET, POST `/api/processes` | GET, PUT `/api/processes/{id}` |
| Vendors | GET, POST `/api/vendors` | GET, PUT `/api/vendors/{id}` |
| Vendor process rates | GET, POST `/api/vendor-process-rates` | GET `/api/vendor-process-rates/{id}` |
| RFQ item master lookup | - | GET `/api/rfqs/{rfqId}/items/{itemId}/master` |
| Metal weight calculation | POST `/api/calculations/metal-weight` | - |
| Raw-material cost calculation | POST `/api/calculations/raw-material-cost` | - |
| In-house process cost | POST `/api/calculations/process-cost/in-house` | - |
| Outsource process cost | POST `/api/calculations/process-cost/outsource` | - |
| Cost sheets | GET, POST `/api/cost-sheets` | GET, PUT, DELETE `/api/cost-sheets/{id}` |
| Cost sheet by RFQ item | GET `/api/cost-sheets/by-rfq-item/{rfqItemId}` | - |
| Quotations | GET, POST `/api/quotations` | GET, PUT `/api/quotations/{id}` |
| Quotation revisions | GET `/api/quotations/{id}/revisions` | GET `/api/quotations/{id}/revisions/{revision}` |

Filter RFQs with `?customerId=1`. Customer PUT retains the existing requirement that the body ID matches the URL ID. RFQ and item writes use request DTOs; IDs and creation timestamps are server controlled. Item IDs are scoped to their parent RFQ.

RFQ PDF import accepts a text-based PDF up to 10 MB and returns extracted text,
header suggestions, line-item suggestions, and review warnings. Extraction is
read-only and never creates database records. After a user corrects the
suggestions, `/api/rfq-imports/confirm` creates the RFQ and its items in one
save. Scanned image-only PDFs require OCR before import.

RFQ statuses: Draft (default), Submitted, Closed, Cancelled. Quantity must be positive, fit decimal(18,3), and have at most three decimal places. Customer deletion returns 409 when RFQs exist. RFQ deletion cascades to its items after the migration is applied.

Material numbers are unique business identifiers. Use `?search=` and `?isActive=` to filter materials, and `?isActive=` for metals, processes, and vendors. Routing sequences are unique per material and always returned in sequence order. Outsourced routes require a vendor; in-house routes do not accept one. Master relationships use restrictive deletes so referenced data cannot be removed accidentally.

The RFQ item master lookup is read-only. It returns `masterExists: false` when the item material number is not in the Mastersheet and never overwrites the RFQ item.

The metal-weight calculator supports `RoundBar`, `SquareBar`, `Plate`, `FlatBar`, `RoundTube`, `SquareTube`, `RectangularTube`, and `HexBar` in `mm`, `cm`, `m`, or `inch`. Modes are `LengthToWeight` and `WeightToLength`. Hex bar size is always across flats. Round tubes accept either an inner diameter or a wall thickness, but not both. Calculations normalize to SI units and return area in m2, volume in m3, and weight in kg.

The raw-material cost calculator returns total raw weight, gross material cost, optional scrap recovery, and net material cost without rounding intermediate values. Scrap recovery cannot exceed gross material cost.

In-house process times are expressed in hours. The process-cost calculator combines setup time, quantity-based cycle time, machine rate, and optional labour and tooling costs. Outsource costing supports `PerPiece`, `PerKg`, `PerHour`, and `Fixed`; each variable rate requires its matching quantity, weight, or hours.

Each RFQ item can have one editable cost sheet. Cost lines are ordered by sequence and use controlled categories covering raw material, manufacturing, outsource, tooling, inspection, packing, transport, and other costs. Every response includes all category totals, manufacturing cost, overhead, cost after overhead, profit, selling price, and unit selling price. Cost lines cascade with their sheet, and sheets cascade when their RFQ item is deleted.

Creating a quotation produces immutable revision 0. Each PUT is an explicit revision: it increments the revision number, refreshes current item snapshots from the selected RFQ items and cost sheets, and preserves every older revision unchanged. Snapshots include commercial terms, material rate, density, raw dimensions, weights, process/vendor rates, the complete costing breakdown, overhead, profit, and offered unit/total prices. A quoted RFQ cannot be deleted.

Quotation statuses are `Draft`, `Ready`, `Sent`, `UnderReview`, `Negotiation`, `Accepted`, `Rejected`, `Lost`, `Expired`, and `POReceived`. Filter quotations with `?customerId=` or `?status=`.

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

Customer, RFQ, Mastersheet management, RFQ PDF extraction with confirmed
Read & Fix, engineering calculators, process costing, persistent cost sheets,
quotations, immutable quotation revisions, and the Angular costing workspace.
Login, OCR, quotation PDF generation, reports, settings, and production
deployment configuration are not included yet. The PDF path model property is
reserved and is not writable through the RFQ request DTO.
