param([string]$BaseUrl = 'http://localhost:5263')
$ErrorActionPreference = 'Stop'
function Assert-BadRequest([string]$Path, [string]$Body) {
    try {
        Invoke-WebRequest -Uri ($BaseUrl + $Path) -Method Post -ContentType 'application/json' -Body $Body -UseBasicParsing | Out-Null
        throw "Expected HTTP 400 for $Path"
    } catch {
        if ($null -eq $_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne 400) { throw }
        Write-Output "PASS: $Path rejects invalid input"
    }
}
Assert-BadRequest '/api/customers' '{"customerName":"   "}'
Assert-BadRequest '/api/customers' '{"customerName":"Example","email":"invalid"}'
Assert-BadRequest '/api/rfqs' '{"rfqNumber":"","customerId":0}'
Assert-BadRequest '/api/rfqs' '{"rfqNumber":"RFQ-1","customerId":1,"status":"Unknown"}'
Assert-BadRequest '/api/rfqs/1/items' '{"materialNo":"MAT-1","quantity":0}'
Assert-BadRequest '/api/rfqs/1/items' '{"materialNo":" ","quantity":1,"unit":""}'
