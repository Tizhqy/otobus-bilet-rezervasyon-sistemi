param(
    [string]$BaseUrl = "https://localhost:5001"
)

$ErrorActionPreference = "Stop"

$payloads = @(
    "' OR 1=1 --",
    "' UNION SELECT NULL --",
    "'; DROP TABLE users; --",
    "' OR 'a'='a",
    "admin'/*",
    '" OR "1"="1'
)

# Scenarios: define endpoint template and method
$scenarios = @(
    @{ Name = 'StationSearch'; Method = 'GET'; Url = "$BaseUrl/Sefer/IstasyonAra?query={0}" },
    @{ Name = 'SeferIndex';   Method = 'GET'; Url = "$BaseUrl/Sefer/Index" },
    @{ Name = 'AuthGiris';    Method = 'GET'; Url = "$BaseUrl/Auth/Giris" },
    @{ Name = 'AuthLoginPost'; Method = 'POST'; Url = "$BaseUrl/Auth/Giris" }
)

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param(
        [string]$Scenario,
        [string]$Payload,
        [string]$Method,
        [string]$Url,
        [int]$StatusCode,
        [int]$ResponseLength,
        [string]$Outcome,
        [string]$Notes,
        [string]$ExceptionMessage
    )

    $results.Add([PSCustomObject]@{
        Scenario         = $Scenario
        Payload          = $Payload
        Method           = $Method
        Url              = $Url
        StatusCode       = $StatusCode
        ResponseLength   = $ResponseLength
        Outcome          = $Outcome
        Notes            = $Notes
        ExceptionMessage = $ExceptionMessage
    }) | Out-Null
}

foreach ($scenario in $scenarios) {
    foreach ($payload in $payloads) {
        $method = $scenario.Method
        if ($method -eq 'GET') {
            $encoded = [System.Net.WebUtility]::UrlEncode($payload)
            $url = ($scenario.Url -f $encoded)
        }
        else {
            $url = $scenario.Url
        }

        Write-Host "Testing $($scenario.Name) with payload: $payload" -ForegroundColor Cyan

        try {
            if ($method -eq 'GET') {
                $resp = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -ErrorAction Stop
            }
            else {
                # POST form payloads (common fields)
                $body = @{ email = $payload; password = $payload }
                $resp = Invoke-WebRequest -Uri $url -Method Post -Body $body -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -ErrorAction Stop
            }

            $status = 0
            if ($resp.StatusCode) { $status = [int]$resp.StatusCode }
            $length = 0
            if ($resp.Content) { $length = $resp.Content.Length }

            $outcome = if ($status -ge 500) { 'FAIL' } else { 'PASS' }
            $notes = ""
            Add-Result -Scenario $scenario.Name -Payload $payload -Method $method -Url $url -StatusCode $status -ResponseLength $length -Outcome $outcome -Notes $notes -ExceptionMessage ""
        }
        catch {
            $status = 0
            $length = 0
            $exMsg = $_.Exception.Message
            if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
                $status = [int]$_.Exception.Response.StatusCode
            }
            $outcome = if ($status -ge 500) { 'FAIL' } else { 'ERROR' }
            $notes = "Exception during request"
            Add-Result -Scenario $scenario.Name -Payload $payload -Method $method -Url $url -StatusCode $status -ResponseLength $length -Outcome $outcome -Notes $notes -ExceptionMessage $exMsg
        }
    }
}

$outputPath = Join-Path $PSScriptRoot "sql-injection-results.csv"
$results | Export-Csv -Path $outputPath -NoTypeInformation -Encoding UTF8

Write-Host "SQL injection test run completed."
Write-Host "Result file: $outputPath"
$results | Format-Table -AutoSize
