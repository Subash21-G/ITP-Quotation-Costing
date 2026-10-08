param(
    [string]$BaseUrl = 'http://localhost:5263',
    [string]$Username = $(if ($env:ITP_TEST_USERNAME) { $env:ITP_TEST_USERNAME } else { 'phase0-admin' }),
    [string]$Password = $(if ($env:ITP_TEST_PASSWORD) { $env:ITP_TEST_PASSWORD } else { 'Phase0-Test-Password-2026!' })
)
$ErrorActionPreference = 'Stop'
$headers = @{}
$response = Invoke-WebRequest -Uri ($BaseUrl + '/api/auth/login') -Method Post -ContentType 'application/json' -Body (@{ username = $Username; password = $Password } | ConvertTo-Json -Compress) -UseBasicParsing -TimeoutSec 15
$headers.Authorization = 'Bearer ' + (($response.Content | ConvertFrom-Json).accessToken)
function Assert-BadRequest([string]$Path, [string]$Body) {
    try {
        Invoke-WebRequest -Uri ($BaseUrl + $Path) -Method Post -ContentType 'application/json' -Headers $headers -Body $Body -UseBasicParsing | Out-Null
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
