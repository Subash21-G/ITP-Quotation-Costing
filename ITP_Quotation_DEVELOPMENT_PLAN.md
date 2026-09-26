# ITP Quotation Costing System - Development Plan

Project Path:
`D:\Project\ITP-Quotation`

Backend:
`backend\ITPQuotation.Api`

Technology:
- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core 10
- SQL Server
- OpenAPI
- Future Angular frontend

---

# 1. CURRENT COMPLETED WORK

Already completed:

- ASP.NET Core 10 backend project
- SQL Server + EF Core
- Customer model/API
- RFQ model/API
- RFQ Item model/API
- Customer -> RFQs relationship
- RFQ -> RFQ Items relationship
- Full CRUD for Customers, RFQs, RFQ Items
- RFQ filter by customer
- Required field validation
- Email validation
- Quantity validation
- Decimal precision validation
- Field length validation
- RFQ status validation
- Customer delete safeguard when RFQs exist
- RFQ delete cascades to RFQ Items
- Protected IDs, creation dates, PDF paths
- OpenAPI endpoint documentation
- PowerShell smoke-test script
- README documentation
- Build currently succeeds with 0 errors and 0 warnings
- Existing EF model has no pending changes

Do not recreate or remove these features.

---

# 2. FINAL BUSINESS FLOW

Customer RFQ
-> RFQ Item
-> Material Number
-> Check Mastersheet
-> Load Material Master
-> Raw Material Shape + Size
-> Metal Weight Calculator
-> Raw Material Weight
-> Material Rate
-> Raw Material Cost
-> Manufacturing Process Route
-> In-house + Outsource Cost
-> Additional Costs
-> Overhead
-> Profit
-> Final Unit Price
-> Quotation Revision
-> Quotation PDF
-> Customer Acceptance
-> PO Received
-> Future POTracker Integration

---

# 3. DEVELOPMENT RULES

The coding agent must:

1. Inspect existing code before editing.
2. Do not create a new project.
3. Do not recreate the database.
4. Do not delete working migrations.
5. Do not rewrite working CRUD without a reason.
6. Build after every phase.
7. Run tests after every phase.
8. Use new EF migrations for new schema changes.
9. Never modify an already-applied migration.
10. Use deterministic C# formulas for engineering calculations.
11. Use `decimal` for money.
12. Keep controllers thin.
13. Put business logic in services/calculators.
14. Keep DTOs separate from EF entities where appropriate.
15. Do one phase at a time.
16. Stop after each phase and report results.
17. Do not claim success unless build/tests/migration were actually run.

---

# 4. PHASE 0 - VERIFY CURRENT PROJECT

## Goal
Verify the existing backend before adding Mastersheet features.

## Tasks
- Inspect Program.cs, ApplicationDbContext, Models, DTOs, Controllers, Validators, Migrations, README.md, smoke-test script
- Run `dotnet build`
- Run `dotnet ef migrations list`
- Run `dotnet ef database update`
- Start the API
- Test Customer CRUD
- Test RFQ CRUD
- Test RFQ Item CRUD
- Verify Customer delete safeguard
- Verify RFQ cascade delete
- Verify RFQ filter by customer
- Verify validation responses

## Stop Condition
Stop after verification and report:
- Build result
- Migration result
- CRUD result
- Problems found
- Fixes applied
- Next phase recommendation

---

# 5. PHASE 1 - MASTERSHEET FOUNDATION

## Goal
Add reusable engineering/manufacturing master data similar to the POTracker Mastersheet.

RFQ = transactional data.
Mastersheet = reusable material/manufacturing data.
Material Number is the main business identifier.

## 5.1 MaterialMaster
Create `Models/MaterialMaster.cs`

Suggested fields:
- Id
- MaterialNo
- ShortDescription
- Grade
- DrawingCode
- DrawingVersion
- FinishSize
- MetalMaterialId
- RawMaterialShape
- RawMaterialSize
- Notes
- IsActive
- CreatedDate
- UpdatedDate

Rules:
- MaterialNo required
- MaterialNo unique
- MaterialNo indexed
- No duplicate MaterialNo

Example:
Material No: 11753667
Description: INTERM BEAR CAP M 325X253X20
Grade: SAE 1010/20
Drawing: SWD 10001238633 000 01
Drawing Version: 01
Finish Size: 325 x 253 x 20
Raw Material Size: 330 x 245 x 25

## 5.2 MetalMaterial / Density Master
Create `Models/MetalMaterial.cs`

Fields:
- Id
- Name
- Grade
- DensityKgM3
- DefaultRatePerKg
- IsActive
- CreatedDate
- UpdatedDate

Initial values can later include:
- Carbon Steel
- Stainless Steel
- Aluminium
- Copper
- Brass
- Bronze
- Cast Iron
- Nickel

Rules:
- Density > 0
- Rate >= 0
- Do not hard-code density values across calculators

## 5.3 ProcessMaster
Create `Models/ProcessMaster.cs`

Fields:
- Id
- ProcessName
- Description
- DefaultProcessType
- DefaultMachineRate
- IsActive
- CreatedDate
- UpdatedDate

Process types:
- InHouse
- Outsource

Typical processes:
- Cutting
- Turning
- CNC
- Milling
- VTL
- Drilling
- Tapping
- Cylindrical Grinding
- Surface Grinding
- Wire Cutting
- EDM / Sparking
- Hardening
- Heat Treatment
- Welding
- Painting
- Inspection
- Packing

## 5.4 Vendor
Create `Models/Vendor.cs`

Fields:
- Id
- VendorName
- ContactPerson
- Phone
- Email
- Address
- IsActive
- CreatedDate
- UpdatedDate

## 5.5 VendorProcessRate
Create `Models/VendorProcessRate.cs`

Fields:
- Id
- VendorId
- ProcessId
- RateType
- Rate
- EffectiveFrom
- EffectiveTo
- Notes

Rate types:
- PerPiece
- PerKg
- PerHour
- Fixed

## 5.6 MaterialRouting
Create `Models/MaterialRouting.cs`

Fields:
- Id
- MaterialMasterId
- Sequence
- ProcessId
- ProcessType
- VendorId
- MachineRate
- SetupTimeHours
- CycleTimeHours
- RatePerPiece
- Notes

Example:
Material 11753667
1. CNC - InHouse
2. Milling - InHouse
3. VTL - Outsource - Gokul Industries

Rules:
- Sequence > 0
- Sequence unique per MaterialMaster
- Outsource may require Vendor
- InHouse normally has no Vendor
- Always return routes ordered by Sequence

## 5.7 EF Core Configuration
Update `Data/ApplicationDbContext.cs`

Add DbSets:
- MaterialMasters
- MetalMaterials
- ProcessMasters
- Vendors
- VendorProcessRates
- MaterialRoutings

Configure:
- Foreign keys
- Unique constraints
- Indexes
- Decimal precision
- Delete behavior

Important indexes:
- MaterialMaster.MaterialNo UNIQUE
- MaterialRouting(MaterialMasterId, Sequence) UNIQUE
- Vendor.VendorName INDEX
- ProcessMaster.ProcessName INDEX

Prefer Restrict for important master/history relationships.

## 5.8 DTOs
Create DTOs for:
- MaterialMasterCreate
- MaterialMasterUpdate
- MaterialMasterResponse
- MetalMaterial
- ProcessMaster
- Vendor
- VendorProcessRate
- MaterialRouting

## 5.9 APIs
Suggested endpoints:
- GET /api/materials
- GET /api/materials/{materialNo}
- POST /api/materials
- PUT /api/materials/{id}
- DELETE /api/materials/{id}

- GET /api/materials/{id}/routing
- POST /api/materials/{id}/routing
- PUT /api/materials/{id}/routing/{routingId}
- DELETE /api/materials/{id}/routing/{routingId}

- GET /api/processes
- POST /api/processes
- PUT /api/processes/{id}

- GET /api/vendors
- POST /api/vendors
- PUT /api/vendors/{id}

- GET /api/metals
- POST /api/metals
- PUT /api/metals/{id}

- GET /api/vendor-process-rates
- POST /api/vendor-process-rates

## 5.10 RFQ -> Mastersheet Lookup
When RFQ Item contains MaterialNo:

RFQ Item
-> Material Number
-> Search MaterialMaster
-> Found?

If YES, return:
- MaterialNo
- Description
- Grade
- Drawing
- Drawing Version
- Finish Size
- Raw Material Shape
- Raw Material Size
- Metal
- Density
- Process Route
- Vendor info

If NO:
Return `MasterExists = false`

Do not overwrite RFQ fields automatically.

## 5.11 Migration
After Phase 1:
- `dotnet build`
- `dotnet ef migrations add AddMastersheetFoundation`
- Review migration
- `dotnet ef database update`
- Run smoke tests

## Stop Condition
Stop after Phase 1 and report:
- Files created
- Files modified
- Models added
- DTOs added
- Endpoints added
- Relationships added
- Indexes/constraints
- Migration name
- Migration result
- Build result
- Test result
- Errors/warnings
- Next step

---

# 6. PHASE 2 - C# AUTOMATED TEST PROJECT

Create `tests/ITPQuotation.Api.Tests`

Use xUnit.

Add tests for:
- Material Number uniqueness
- Routing sequence validation
- Metal density validation
- Vendor/process relationships
- API validation rules where practical

Stop and report before continuing.

---

# 7. PHASE 3 - METAL WEIGHT CALCULATOR

Create:
- `Calculators/IMetalWeightCalculator.cs`
- `Calculators/MetalWeightCalculator.cs`

Initial shapes:
- RoundBar
- SquareBar
- Plate
- FlatBar
- RoundTube
- SquareTube
- RectangularTube
- HexBar

Future:
- Angle
- Channel
- I-Beam
- T-Bar
- Standard profiles

Units:
- mm
- cm
- m
- inch

Normalize internally to SI units.
Density unit: kg/m3

Modes:
- Length -> Weight
- Weight -> Length

Outputs:
- CrossSectionArea
- VolumePerPiece
- WeightPerPieceKg
- Quantity
- TotalWeightKg
- RequiredLength if applicable

Formulas:
Plate: Volume = Length x Width x Thickness
Round Bar: Area = PI x Diameter^2 / 4
Round Tube: Area = PI / 4 x (OD^2 - ID^2)
Square Bar: Area = Side^2
Square Tube: InnerWidth = OuterWidth - 2 x Thickness; InnerHeight = OuterHeight - 2 x Thickness; Area = OuterArea - InnerArea
Rectangular Tube: same outer-minus-inner approach
Hex Bar: make dimension convention explicit; never silently assume side vs across-flats

API:
`POST /api/calculations/metal-weight`

Validation:
- dimensions > 0
- density > 0
- quantity > 0
- valid tube wall thickness
- reject impossible geometry
- centralized unit conversion
- no AI for calculations

Tests:
- Plate
- Round Bar
- Round Tube
- Square Tube
- Rectangular Tube
- Hex Bar
- Unit conversion
- Invalid dimensions
- Invalid tube geometry

Stop and report before continuing.

---

# 8. PHASE 4 - RAW MATERIAL COST

Inputs:
- WeightPerPiece
- Quantity
- MaterialRatePerKg
- ScrapWeight optional
- ScrapRecoveryRate optional

Calculations:
TotalRawWeight = WeightPerPiece x Quantity
GrossMaterialCost = TotalRawWeight x MaterialRatePerKg
ScrapRecovery = ScrapWeight x ScrapRecoveryRate
NetMaterialCost = GrossMaterialCost - ScrapRecovery

Rules:
- Use decimal for money
- NetMaterialCost cannot be negative
- Do not repeatedly round intermediate values

Add API + unit tests.
Stop and report.

---

# 9. PHASE 5 - PROCESS COSTING

InHouse inputs:
- SetupTime
- CycleTime
- Quantity
- MachineRatePerHour
- LabourCost optional
- ToolCost optional

TotalProcessTime = SetupTime + (CycleTime x Quantity)
MachineCost = TotalProcessTime x MachineRatePerHour

Outsource rate types:
- PerPiece
- PerKg
- PerHour
- Fixed

Add APIs + unit tests.
Stop and report.

---

# 10. PHASE 6 - QUOTATION COST SHEET

Create:
- CostSheet
- CostSheetLine

Cost categories:
- Raw Material
- Cutting
- Turning
- CNC
- Milling
- VTL
- Drilling
- Grinding
- Wire Cutting
- Heat Treatment
- Welding
- Painting
- Outsource
- Tooling
- Inspection
- Packing
- Transport
- Other

Calculations:
ManufacturingCost = Sum of cost lines
OverheadAmount = ManufacturingCost x OverheadPercent
CostAfterOverhead = ManufacturingCost + OverheadAmount
ProfitAmount = CostAfterOverhead x ProfitPercent
SellingPrice = CostAfterOverhead + ProfitAmount
UnitSellingPrice = SellingPrice / Quantity

Always return complete breakdown.
Stop and report.

---

# 11. PHASE 7 - QUOTATION + REVISION

Create:
- Quotation
- QuotationItem
- QuotationRevision

Quotation fields:
- Id
- QuotationNumber
- CustomerId
- RfqId
- Revision
- QuotationDate
- ValidUntil
- PaymentTerms
- DeliveryTerms
- DeliveryTime
- FreightTerms
- TaxNotes
- Status
- CreatedDate
- UpdatedDate

Statuses:
- Draft
- Ready
- Sent
- UnderReview
- Negotiation
- Accepted
- Rejected
- Lost
- Expired
- POReceived

Critical revision rule:
Never change historical quotation costing when master rates change.

Snapshot:
- Material rate
- Density
- Dimensions
- Weight
- Process route
- Machine rates
- Vendor rates
- Cost breakdown
- Overhead
- Profit
- Unit price
- Commercial terms

Stop and report.

---

# 12. PHASE 8 - ANGULAR FRONTEND

Path:
`frontend\itp-quotation-ui`

Use:
- Angular
- TypeScript
- Angular Material
- SCSS
- Signals
- RxJS

Navigation:
- Dashboard
- New Quotation
- RFQs
- Quotations
- Masters
  - Materials
  - Metals/Density
  - Processes
  - Vendors
  - Rates
- Reports
- Settings

---

# 13. MASTERSHEET UI

Material Information:
- Material Number
- Short Description
- Grade
- Drawing
- Drawing Version
- Finish Size

Raw Material:
- Metal
- Shape
- Dimensions
- Density
- Calculated Weight

Manufacturing Route:
- Sequence
- Process
- Type
- Vendor
- Time
- Rate
- Cost

Actions:
- Load Material
- Save Material
- Add Process
- Delete Process
- Reorder Process

Add searchable Material Master table.

---

# 14. METAL CALCULATOR UI

Shape cards:
- Round Bar
- Square Bar
- Plate / Flat
- Round Tube
- Square Tube
- Rectangular Tube
- Hex Bar

Flow:
Select Shape -> Select Metal -> Enter Dimensions -> Enter Length / Weight -> Enter Quantity -> Calculate

Results:
- Weight / Piece
- Total Weight
- Rate / Kg
- Material Cost

Allow result to flow into quotation costing.

---

# 15. QUOTATION WORKSPACE UI

Main area:
- RFQ details
- Material details
- Metal Calculator
- Process Route
- Additional Costs

Sticky right summary:
- Material
- Machining
- Outsource
- Tooling
- Packing
- Transport
- Other
- Base Cost
- Overhead
- Actual Cost
- Profit
- Final Price
- Unit Price

Update values immediately when inputs change.

---

# 16. PHASE 9 - RFQ PDF + READ & FIX

Flow:
PDF Upload -> Text Extraction -> Field Parsing -> Read & Fix -> User Confirmation

Extract where possible:
- RFQ Number
- Customer
- Material Number
- Description
- Drawing
- Quantity
- Delivery Date
- Grade
- Dimensions

Never save extracted data automatically without user confirmation.

---

# 17. PHASE 10 - QUOTATION PDF

Use QuestPDF.

Include:
- Indhra Turning Point
- Quotation Number
- Revision
- Date
- Customer
- RFQ Number
- Material Number
- Description
- Drawing
- Quantity
- Unit Price
- Total Price
- Delivery Time
- Payment Terms
- Validity
- Freight
- Taxes
- Grand Total

---

# 18. PHASE 11 - AUTHENTICATION

Roles:
- Admin
- Estimator
- Viewer

Possible future roles:
- Production
- Purchase
- Accounts

---

# 19. PHASE 12 - POTRACKER INTEGRATION

Final flow:
Quotation Accepted -> Customer PO Received -> Match Customer/RFQ/Material/Quotation -> Create or Link PO -> POTracker

Quotation App responsibility:
- RFQ
- Costing
- Quotation

POTracker responsibility:
- Purchase Order
- Production
- Stock
- Invoice
- Dispatch

Reuse master data where appropriate:
- Customer
- Material
- Drawing
- Process
- Vendor
- Routing

---

# 20. EXPECTED FILES/FOLDERS THROUGH DEVELOPMENT

```text
backend/ITPQuotation.Api/
├── Controllers/
│   ├── CustomersController.cs
│   ├── RfqsController.cs
│   ├── RfqItemsController.cs
│   ├── MaterialsController.cs
│   ├── ProcessesController.cs
│   ├── VendorsController.cs
│   ├── MetalsController.cs
│   ├── VendorProcessRatesController.cs
│   ├── CalculationsController.cs
│   └── QuotationsController.cs
├── Models/
│   ├── Customer.cs
│   ├── Rfq.cs
│   ├── RfqItem.cs
│   ├── MaterialMaster.cs
│   ├── MetalMaterial.cs
│   ├── ProcessMaster.cs
│   ├── Vendor.cs
│   ├── VendorProcessRate.cs
│   ├── MaterialRouting.cs
│   ├── CostSheet.cs
│   ├── CostSheetLine.cs
│   ├── Quotation.cs
│   ├── QuotationItem.cs
│   └── QuotationRevision.cs
├── DTOs/
│   ├── Materials/
│   ├── Processes/
│   ├── Vendors/
│   ├── Calculations/
│   └── Quotations/
├── Services/
│   ├── MaterialService.cs
│   ├── ProcessService.cs
│   ├── VendorService.cs
│   ├── CostingService.cs
│   └── QuotationService.cs
├── Calculators/
│   ├── IMetalWeightCalculator.cs
│   ├── MetalWeightCalculator.cs
│   ├── RawMaterialCostCalculator.cs
│   ├── ProcessCostCalculator.cs
│   └── QuotationCalculator.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Validators/
├── Migrations/
├── Documents/
├── Common/
└── Program.cs
```

Tests:
`tests/ITPQuotation.Api.Tests/`

Frontend later:
`frontend/itp-quotation-ui/`

---

# 21. PROMPT TO USE WITH VS CODE CODING AGENT

## Phase 1 Prompt

Read `ITP_Quotation_DEVELOPMENT_PLAN.md`.

The project is:
`D:\Project\ITP-Quotation`

Execute PHASE 1 - MASTERSHEET FOUNDATION only.

Before editing:
1. Inspect the existing repository.
2. Verify the existing build.
3. Verify current migrations and database state.
4. Do not recreate or remove existing working features.

Implement:
- MaterialMaster
- MetalMaterial
- ProcessMaster
- Vendor
- VendorProcessRate
- MaterialRouting
- DTOs
- validation
- services
- controllers/endpoints
- ApplicationDbContext configuration
- indexes/constraints/relationships

Then:
1. Build.
2. Add xUnit tests where appropriate.
3. Create migration named `AddMastersheetFoundation`.
4. Review it.
5. Apply it to the development database.
6. Run tests and smoke tests.

STOP after Phase 1.

Report:
- files created
- files changed
- endpoints added
- migration result
- build result
- test result
- errors/warnings
- next recommended phase

Do not start the Metal Weight Calculator yet.

## Phase 3 Prompt

Read `ITP_Quotation_DEVELOPMENT_PLAN.md`.

Execute PHASE 3 - METAL WEIGHT CALCULATOR only.

Do not modify completed Mastersheet behavior unless required to integrate cleanly.

Implement the calculator, API, validation, centralized unit conversion, and xUnit tests.

Do not start Raw Material Costing.

Build, test, then stop and report.

---

# 22. CHECKPOINT RULE

After every phase, do not immediately start the next phase.

First confirm:
- Build passed
- Tests passed
- Migration applied if needed
- API verified
- No unexpected warnings/errors
- Existing functionality still works

Then continue.

---

# 23. FINAL DEVELOPMENT ORDER

1. Verify current backend
2. Mastersheet foundation
3. Automated test project
4. Metal Weight Calculator
5. Raw Material Cost
6. Process/Vendor Costing
7. Full Cost Sheet
8. Quotation + Revisioning
9. Angular frontend
10. RFQ PDF + Read & Fix
11. Quotation PDF
12. Authentication
13. Reports
14. POTracker integration
