# ITP-Quotation → POTracker

The quotations list offers **Send to POTracker** for `Accepted` and `POReceived` quotations. Enter the confirmed customer PO number. Admin and Estimator roles can export; Viewer cannot. The backend sends the commercial quotation items to `POST /api/purchase-orders/from-quotation` using `X-Quotation-Api-Key`, then stores the returned purchase order ID, PO number, and exported source revision.

Configure the quotation backend using environment variables:

```
POTracker__BaseUrl=http://localhost:5001
POTracker__ApiKey=<shared secret>
POTracker__CustomerCodes__<quotation customer ID>=CUST-<quotation customer ID>
```

The quotation workspace combines active ITP-Quotation process/vendor-rate options with POTracker's Vendor and process mastersheet options. POTracker options are read through `GET /api/quotation-integration/mastersheet/process-options`, protected by the same shared API key. The workspace provides separate process and in-house/outsource selectors. In-house work does not require a vendor; outsource work displays the selected process's vendor choices and requires one. Adding a process displays it in the cost rows, while the description records its type and vendor when applicable. Existing cost rows are preserved, and the amount remains editable.

Configure the POTracker backend with the same secret and matching customer codes:

```
QuotationIntegration__ApiKey=<same shared secret>
QuotationIntegration__CustomerMappings__CUST-<quotation customer ID>=<existing POTracker purchaser ID>
```

Never store the key in frontend code or commit it. For separate machines use HTTPS and the actual server address. `localhost` only works when both backends run on the same machine. Customer IDs in the two databases are not assumed to match.

Apply the new quotation database migration using `dotnet ef database update --project backend/ITPQuotation.Api` before restarting that backend. Restart the updated POTracker backend to apply its existing startup migration.

ITP-Quotation uses revision 0 for its initial offer; the wire contract sends `source revision + 1`. Quantities must have at most two decimal places. Unit prices and resulting line totals use two decimal places, rounded away from zero. Tax is excluded. Empty material descriptions must be corrected before export.

A repeated send with identical data returns the same POTracker order ID. Changed data for an already imported revision or an existing PO number returns a conflict requiring reconciliation. Transport failures/timeouts can be retried with the same PO number. Existing production rows are never replaced. Two-way status synchronization is not part of this export.

Live acceptance check: configure a real customer mapping, accept a quotation, enter its real confirmed PO number, send it, verify its material rows and purchaser in POTracker, then send it again and confirm the same order ID is returned. Local automated tests use isolated data and do not create business orders.
