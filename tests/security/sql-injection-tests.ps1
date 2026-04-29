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
    @{ Name = 'StationSearch'; Method = 'GET'; Url = "$BaseUrl/Sefer/IstasyonAra?query={0}"; Body = $null; ContentType = $null },
    @{ Name = 'SeferIndex'; Method = 'GET'; Url = "$BaseUrl/Sefer/Index"; Body = $null; ContentType = $null },
    @{ Name = 'AuthGiris'; Method = 'GET'; Url = "$BaseUrl/Auth/Giris"; Body = $null; ContentType = $null },
    @{ Name = 'AuthLoginPost'; Method = 'POST'; Url = "$BaseUrl/Auth/Giris"; ContentType = 'application/x-www-form-urlencoded'; Body = {
            param($payload)
            @{ Email = $payload; Password = $payload; RememberMe = 'false' }
        }
    },
    @{ Name = 'AuthRegisterPost'; Method = 'POST'; Url = "$BaseUrl/Auth/Kayit"; ContentType = 'application/x-www-form-urlencoded'; Body = {
            param($payload)
            @{ FirstName = $payload; LastName = $payload; Email = "sqli+$([System.Net.WebUtility]::UrlEncode($payload))@example.com"; Password = $payload; ConfirmPassword = $payload; Phone = $payload }
        }
    },
    @{ Name = 'BiletSatinAlPost'; Method = 'POST'; Url = "$BaseUrl/Bilet/SatinAl"; ContentType = 'application/x-www-form-urlencoded'; Body = {
            param($payload)
            @{
                DepartureId = '1'
                'Passengers[0].SeatId' = '1'
                'Passengers[0].FirstName' = $payload
                'Passengers[0].LastName' = $payload
                'Passengers[0].IdNumber' = $payload
                'Payment.Amount' = '1.00'
                'Payment.Method' = 'credit_card'
                'Payment.TransactionId' = [guid]::NewGuid().ToString('N')
            }
        }
    },
    @{ Name = 'BiletKoltukKontrolPost'; Method = 'POST'; Url = "$BaseUrl/Bilet/KoltukKontrol?seferId=1"; ContentType = 'application/json'; Body = {
            param($payload)
            @($payload, $payload)
        }
    }
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
                $bodyFactory = $scenario.Body
                $contentType = $scenario.ContentType

                if ($contentType -eq 'application/json') {
                    $body = $bodyFactory.Invoke($payload) | ConvertTo-Json -Depth 5 -Compress
                    $resp = Invoke-WebRequest -Uri $url -Method Post -Body $body -ContentType $contentType -UseBasicParsing -ErrorAction Stop
                }
                else {
                    $body = $bodyFactory.Invoke($payload)
                    $resp = Invoke-WebRequest -Uri $url -Method Post -Body $body -ContentType $contentType -UseBasicParsing -ErrorAction Stop
                }
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
