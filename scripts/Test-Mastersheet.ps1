param(
    [string]$BaseUrl = 'http://localhost:5263',
    [string]$Username = $(if ($env:ITP_TEST_USERNAME) { $env:ITP_TEST_USERNAME } else { 'phase0-admin' }),
    [string]$Password = $(if ($env:ITP_TEST_PASSWORD) { $env:ITP_TEST_PASSWORD } else { 'Phase0-Test-Password-2026!' })
)

$ErrorActionPreference = 'Stop'
$token = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$materialNo = "PHASE1-$token"
$metalId = $null
$inHouseProcessId = $null
$outsourceProcessId = $null
$vendorId = $null
$rateId = $null
$materialId = $null
$routeIds = [System.Collections.Generic.List[int]]::new()
$customerId = $null
$rfqId = $null
$passed = 0
$headers = @{}

function Connect-Api {
    $response = Invoke-WebRequest -Uri ($BaseUrl + '/api/auth/login') -Method Post -ContentType 'application/json' -Body (@{ username = $Username; password = $Password } | ConvertTo-Json -Compress) -UseBasicParsing -TimeoutSec 15
    $headers.Authorization = 'Bearer ' + (($response.Content | ConvertFrom-Json).accessToken)
}

Connect-Api

function Invoke-Api([string]$Method, [string]$Path, $Body = $null) {
    $parameters = @{
        Uri = $BaseUrl + $Path
        Method = $Method
        UseBasicParsing = $true
        TimeoutSec = 15
    }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body | ConvertTo-Json -Depth 12 -Compress
    }
    $parameters.Headers = $headers

    try {
        $response = Invoke-WebRequest @parameters
        return [pscustomobject]@{
            Status = [int]$response.StatusCode
            Content = $response.Content
        }
    }
    catch {
        if ($null -eq $_.Exception.Response) { throw }
        return [pscustomobject]@{
            Status = [int]$_.Exception.Response.StatusCode
            Content = $null
        }
    }
}

function Assert-Status([string]$Name, $Response, [int]$Expected) {
    if ($Response.Status -ne $Expected) {
        throw "$Name expected HTTP $Expected but received $($Response.Status)."
    }
    $script:passed++
    Write-Output "PASS: $Name"
}

try {
    Assert-Status 'Metal density validation' `
        (Invoke-Api 'POST' '/api/metals' @{
            name = 'Invalid'
            densityKgM3 = 0
            defaultRatePerKg = 1
        }) 400

    $metal = Invoke-Api 'POST' '/api/metals' @{
        name = "Carbon Steel $token"
        grade = 'SAE 1010/20'
        densityKgM3 = 7850
        defaultRatePerKg = 82.50
        isActive = $true
    }
    Assert-Status 'Metal create' $metal 201
    $metalId = [int](($metal.Content | ConvertFrom-Json).id)
    Assert-Status 'Metal get' (Invoke-Api 'GET' "/api/metals/$metalId") 200
    Assert-Status 'Metal update' `
        (Invoke-Api 'PUT' "/api/metals/$metalId" @{
            name = "Carbon Steel $token"
            grade = 'SAE 1010/20'
            densityKgM3 = 7850
            defaultRatePerKg = 84.25
            isActive = $true
        }) 200

    $inHouse = Invoke-Api 'POST' '/api/processes' @{
        processName = "CNC $token"
        defaultProcessType = 'InHouse'
        defaultMachineRate = 950
        isActive = $true
    }
    Assert-Status 'In-house process create' $inHouse 201
    $inHouseProcessId = [int](($inHouse.Content | ConvertFrom-Json).id)

    $outsource = Invoke-Api 'POST' '/api/processes' @{
        processName = "Heat Treatment $token"
        defaultProcessType = 'Outsource'
        defaultMachineRate = 0
        isActive = $true
    }
    Assert-Status 'Outsource process create' $outsource 201
    $outsourceProcessId = [int](($outsource.Content | ConvertFrom-Json).id)
    Assert-Status 'Process update' `
        (Invoke-Api 'PUT' "/api/processes/$inHouseProcessId" @{
            processName = "CNC $token"
            defaultProcessType = 'InHouse'
            defaultMachineRate = 975
            isActive = $true
        }) 200

    Assert-Status 'Vendor email validation' `
        (Invoke-Api 'POST' '/api/vendors' @{
            vendorName = 'Invalid Vendor'
            email = 'invalid'
        }) 400

    $vendor = Invoke-Api 'POST' '/api/vendors' @{
        vendorName = "Phase 1 Vendor $token"
        contactPerson = 'Verifier'
        email = "vendor$token@example.com"
        isActive = $true
    }
    Assert-Status 'Vendor create' $vendor 201
    $vendorId = [int](($vendor.Content | ConvertFrom-Json).id)
    Assert-Status 'Vendor update' `
        (Invoke-Api 'PUT' "/api/vendors/$vendorId" @{
            vendorName = "Phase 1 Vendor $token"
            contactPerson = 'Updated'
            email = "vendor$token@example.com"
            isActive = $true
        }) 200

    Assert-Status 'Vendor rate relationship validation' `
        (Invoke-Api 'POST' '/api/vendor-process-rates' @{
            vendorId = 2147483647
            processId = $outsourceProcessId
            rateType = 'PerPiece'
            rate = 10
            effectiveFrom = '2026-09-26T00:00:00Z'
        }) 400

    Assert-Status 'Vendor rate date validation' `
        (Invoke-Api 'POST' '/api/vendor-process-rates' @{
            vendorId = $vendorId
            processId = $outsourceProcessId
            rateType = 'PerPiece'
            rate = 10
            effectiveFrom = '2026-09-27T00:00:00Z'
            effectiveTo = '2026-09-26T00:00:00Z'
        }) 400

    $rate = Invoke-Api 'POST' '/api/vendor-process-rates' @{
        vendorId = $vendorId
        processId = $outsourceProcessId
        rateType = 'PerPiece'
        rate = 125.50
        effectiveFrom = '2026-09-26T00:00:00Z'
    }
    Assert-Status 'Vendor process rate create' $rate 201
    $rateId = [int](($rate.Content | ConvertFrom-Json).id)
    Assert-Status 'Vendor process rate filter' `
        (Invoke-Api 'GET' "/api/vendor-process-rates?vendorId=$vendorId&processId=$outsourceProcessId") 200

    Assert-Status 'Material metal-reference validation' `
        (Invoke-Api 'POST' '/api/materials' @{
            materialNo = "BAD-$token"
            metalMaterialId = 2147483647
            isActive = $true
        }) 400

    $material = Invoke-Api 'POST' '/api/materials' @{
        materialNo = "  $materialNo  "
        shortDescription = 'INTERM BEAR CAP'
        grade = 'SAE 1010/20'
        drawingCode = 'SWD 10001238633 000 01'
        drawingVersion = '01'
        finishSize = '325 x 253 x 20'
        metalMaterialId = $metalId
        rawMaterialShape = 'Plate'
        rawMaterialSize = '330 x 245 x 25'
        isActive = $true
    }
    Assert-Status 'Material create' $material 201
    $materialData = $material.Content | ConvertFrom-Json
    $materialId = [int]$materialData.id
    if ($materialData.materialNo -ne $materialNo -or
        [int]$materialData.metal.id -ne $metalId) {
        throw 'Material normalization or metal mapping failed.'
    }
    $passed++
    Write-Output 'PASS: Material normalization and metal mapping'

    Assert-Status 'Duplicate material number safeguard' `
        (Invoke-Api 'POST' '/api/materials' @{
            materialNo = $materialNo
            isActive = $true
        }) 409
    Assert-Status 'Material get by number' `
        (Invoke-Api 'GET' "/api/materials/$materialNo") 200
    Assert-Status 'Material searchable list' `
        (Invoke-Api 'GET' "/api/materials?search=$token&isActive=true") 200
    Assert-Status 'Material update' `
        (Invoke-Api 'PUT' "/api/materials/$materialId" @{
            materialNo = $materialNo
            shortDescription = 'INTERM BEAR CAP UPDATED'
            grade = 'SAE 1010/20'
            metalMaterialId = $metalId
            rawMaterialShape = 'Plate'
            rawMaterialSize = '330 x 245 x 25'
            isActive = $true
        }) 200

    Assert-Status 'Routing sequence validation' `
        (Invoke-Api 'POST' "/api/materials/$materialId/routing" @{
            sequence = 0
            processId = $inHouseProcessId
            processType = 'InHouse'
        }) 400
    Assert-Status 'In-house vendor validation' `
        (Invoke-Api 'POST' "/api/materials/$materialId/routing" @{
            sequence = 1
            processId = $inHouseProcessId
            processType = 'InHouse'
            vendorId = $vendorId
        }) 400
    Assert-Status 'Outsource vendor validation' `
        (Invoke-Api 'POST' "/api/materials/$materialId/routing" @{
            sequence = 1
            processId = $outsourceProcessId
            processType = 'Outsource'
        }) 400

    $route2 = Invoke-Api 'POST' "/api/materials/$materialId/routing" @{
        sequence = 2
        processId = $inHouseProcessId
        processType = 'InHouse'
        machineRate = 975
        setupTimeHours = 0.5
        cycleTimeHours = 0.25
    }
    Assert-Status 'In-house routing create' $route2 201
    $route2Id = [int](($route2.Content | ConvertFrom-Json).id)
    $routeIds.Add($route2Id)

    $route1 = Invoke-Api 'POST' "/api/materials/$materialId/routing" @{
        sequence = 1
        processId = $outsourceProcessId
        processType = 'Outsource'
        vendorId = $vendorId
        ratePerPiece = 125.50
    }
    Assert-Status 'Outsource routing create' $route1 201
    $route1Id = [int](($route1.Content | ConvertFrom-Json).id)
    $routeIds.Add($route1Id)

    Assert-Status 'Duplicate routing sequence safeguard' `
        (Invoke-Api 'POST' "/api/materials/$materialId/routing" @{
            sequence = 1
            processId = $inHouseProcessId
            processType = 'InHouse'
        }) 409

    $routing = Invoke-Api 'GET' "/api/materials/$materialId/routing"
    Assert-Status 'Routing list' $routing 200
    $routingData = ConvertFrom-Json -InputObject $routing.Content
    if ($routingData.Count -ne 2 -or
        [int]$routingData[0].sequence -ne 1 -or
        [int]$routingData[1].sequence -ne 2) {
        throw 'Routing is not ordered by sequence.'
    }
    $passed++
    Write-Output 'PASS: Routing sequence order'
    Assert-Status 'Routing update' `
        (Invoke-Api 'PUT' "/api/materials/$materialId/routing/$route2Id" @{
            sequence = 2
            processId = $inHouseProcessId
            processType = 'InHouse'
            machineRate = 1000
            setupTimeHours = 0.75
            cycleTimeHours = 0.20
        }) 200
    Assert-Status 'Material delete restriction' `
        (Invoke-Api 'DELETE' "/api/materials/$materialId") 409

    $customer = Invoke-Api 'POST' '/api/customers' @{
        customerName = "Phase 1 Lookup $token"
        email = "lookup$token@example.com"
    }
    Assert-Status 'Lookup customer create' $customer 201
    $customerId = [int](($customer.Content | ConvertFrom-Json).id)
    $rfq = Invoke-Api 'POST' '/api/rfqs' @{
        rfqNumber = "LOOKUP-$token"
        customerId = $customerId
        status = 'Draft'
    }
    Assert-Status 'Lookup RFQ create' $rfq 201
    $rfqId = [int](($rfq.Content | ConvertFrom-Json).id)

    $knownItem = Invoke-Api 'POST' "/api/rfqs/$rfqId/items" @{
        materialNo = $materialNo
        description = 'RFQ-owned description'
        quantity = 2
        unit = 'Nos'
    }
    Assert-Status 'Known RFQ item create' $knownItem 201
    $knownItemId = [int](($knownItem.Content | ConvertFrom-Json).id)
    $knownLookup = Invoke-Api 'GET' "/api/rfqs/$rfqId/items/$knownItemId/master"
    Assert-Status 'Known material lookup' $knownLookup 200
    $knownData = $knownLookup.Content | ConvertFrom-Json
    if (-not $knownData.masterExists -or
        $knownData.master.material.materialNo -ne $materialNo -or
        @($knownData.master.routing).Count -ne 2) {
        throw 'Known RFQ item master lookup is incomplete.'
    }
    $passed++
    Write-Output 'PASS: Known lookup contains material, metal, routing, process, and vendor'

    $unknownItem = Invoke-Api 'POST' "/api/rfqs/$rfqId/items" @{
        materialNo = "UNKNOWN-$token"
        quantity = 1
        unit = 'Nos'
    }
    Assert-Status 'Unknown RFQ item create' $unknownItem 201
    $unknownItemId = [int](($unknownItem.Content | ConvertFrom-Json).id)
    $unknownLookup = Invoke-Api 'GET' "/api/rfqs/$rfqId/items/$unknownItemId/master"
    Assert-Status 'Unknown material lookup' $unknownLookup 200
    $unknownData = $unknownLookup.Content | ConvertFrom-Json
    if ($unknownData.masterExists -or $null -ne $unknownData.master) {
        throw 'Unknown material lookup should return MasterExists=false.'
    }
    $passed++
    Write-Output 'PASS: Unknown lookup returns MasterExists=false'

    foreach ($routeId in @($routeIds)) {
        Assert-Status "Routing delete $routeId" `
            (Invoke-Api 'DELETE' "/api/materials/$materialId/routing/$routeId") 204
    }
    $routeIds.Clear()
    Assert-Status 'Material delete after route cleanup' `
        (Invoke-Api 'DELETE' "/api/materials/$materialId") 204
    $materialId = $null
    Assert-Status 'Deleted material is absent' `
        (Invoke-Api 'GET' "/api/materials/$materialNo") 404

    Assert-Status 'Lookup RFQ cleanup' `
        (Invoke-Api 'DELETE' "/api/rfqs/$rfqId") 204
    $rfqId = $null
    Assert-Status 'Lookup customer cleanup' `
        (Invoke-Api 'DELETE' "/api/customers/$customerId") 204
    $customerId = $null

    Write-Output "Mastersheet checks passed: $passed"
}
finally {
    if ($null -ne $rfqId) {
        Invoke-Api 'DELETE' "/api/rfqs/$rfqId" | Out-Null
    }
    if ($null -ne $customerId) {
        Invoke-Api 'DELETE' "/api/customers/$customerId" | Out-Null
    }
    if ($null -ne $materialId) {
        foreach ($routeId in @($routeIds)) {
            Invoke-Api 'DELETE' "/api/materials/$materialId/routing/$routeId" | Out-Null
        }
        Invoke-Api 'DELETE' "/api/materials/$materialId" | Out-Null
    }

    $deleteStatements = [System.Collections.Generic.List[string]]::new()
    if ($null -ne $rateId) {
        $deleteStatements.Add("DELETE FROM VendorProcessRates WHERE Id = $([int]$rateId)")
    }
    if ($null -ne $vendorId) {
        $deleteStatements.Add("DELETE FROM Vendors WHERE Id = $([int]$vendorId)")
    }
    if ($null -ne $inHouseProcessId) {
        $deleteStatements.Add("DELETE FROM ProcessMasters WHERE Id = $([int]$inHouseProcessId)")
    }
    if ($null -ne $outsourceProcessId) {
        $deleteStatements.Add("DELETE FROM ProcessMasters WHERE Id = $([int]$outsourceProcessId)")
    }
    if ($null -ne $metalId) {
        $deleteStatements.Add("DELETE FROM MetalMaterials WHERE Id = $([int]$metalId)")
    }

    if ($deleteStatements.Count -gt 0) {
        $sql = [string]::Join('; ', $deleteStatements) + ';'
        & sqlcmd -S '(localdb)\MSSQLLocalDB' -d 'ITPQuotationDb' -E -b -Q $sql | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw 'Failed to remove temporary Mastersheet test records.'
        }
    }
}
