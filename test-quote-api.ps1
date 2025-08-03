# 🧪 GloHorizon Quote API Test Script (PowerShell)
# Run with: .\test-quote-api.ps1

param(
    [string]$BaseUrl = "https://localhost:5001"
)

# Configuration
$ApiPrefix = "/api/quote"
$FullUrl = "$BaseUrl$ApiPrefix"

Write-Host "🚀 GloHorizon Quote API Test Suite" -ForegroundColor Magenta
Write-Host "🎯 Testing API at: $BaseUrl" -ForegroundColor Cyan
Write-Host "⏰ Started at: $(Get-Date)" -ForegroundColor Yellow

# Test data
$TestData = @{
    hotel = @{
        hotelDetails = @{
            destination = "Dubai"
            checkInDate = "2024-09-15T00:00:00Z"
            checkOutDate = "2024-09-20T00:00:00Z"
            rooms = 1
            adultGuests = 2
            childGuests = 0
            roomType = "deluxe"
            starRating = "4-star"
            amenities = @("pool", "spa", "gym")
        }
        contactEmail = "test@example.com"
        contactPhone = "+233123456789"
        contactName = "John Test User"
        specialRequests = "Sea view room preferred"
        urgency = 1
    }
    
    flight = @{
        flightDetails = @{
            tripType = "round-trip"
            departureCity = "Accra"
            arrivalCity = "London"
            departureDate = "2024-10-01T00:00:00Z"
            returnDate = "2024-10-10T00:00:00Z"
            adultPassengers = 2
            childPassengers = 1
            infantPassengers = 0
            preferredClass = "economy"
            preferredAirline = "British Airways"
            passengers = @(
                @{
                    firstName = "John"
                    lastName = "Doe"
                    dateOfBirth = "1990-01-15"
                    passportNumber = "A12345678"
                    nationality = "Ghanaian"
                }
            )
        }
        contactEmail = "test@example.com"
        contactPhone = "+233123456789"
        contactName = "John Test User"
        specialRequests = "Window seat preferred"
        urgency = 2
    }
    
    tour = @{
        tourDetails = @{
            tourPackage = "Safari Adventure"
            destination = "Kenya"
            startDate = "2024-11-01T00:00:00Z"
            endDate = "2024-11-07T00:00:00Z"
            travelers = 3
            accommodationType = "luxury"
            tourType = "private"
            activities = @("game drives", "cultural visits", "photography")
            mealPlan = "full-board"
        }
        contactEmail = "test@example.com"
        contactPhone = "+233123456789"
        contactName = "John Test User"
        specialRequests = "Photography guide required"
        urgency = 1
    }
}

# Function to test quote submission
function Test-QuoteSubmission {
    param(
        [string]$ServiceType,
        [hashtable]$Data
    )
    
    Write-Host "`n🧪 Testing $($ServiceType.ToUpper()) Quote Request..." -ForegroundColor Blue
    
    try {
        $Uri = "$FullUrl/$ServiceType"
        $Body = $Data | ConvertTo-Json -Depth 10
        
        # Skip SSL certificate validation for localhost testing
        if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type) {
            $CertValidationCode = @"
                using System;
                using System.Net;
                using System.Net.Security;
                using System.Security.Cryptography.X509Certificates;
                public class ServerCertificateValidationCallback {
                    public static void Ignore() {
                        if(ServicePointManager.ServerCertificateValidationCallback == null) {
                            ServicePointManager.ServerCertificateValidationCallback += 
                                delegate(Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
                                    return true;
                                };
                        }
                    }
                }
"@
            Add-Type $CertValidationCode
            [ServerCertificateValidationCallback]::Ignore()
        }
        
        $Response = Invoke-RestMethod -Uri $Uri -Method POST -Body $Body -ContentType "application/json" -ErrorAction Stop
        
        if ($Response.success) {
            Write-Host "✅ SUCCESS: $ServiceType quote submitted" -ForegroundColor Green
            Write-Host "📋 Reference Number: $($Response.referenceNumber)" -ForegroundColor Cyan
            Write-Host "💬 Message: $($Response.message)" -ForegroundColor Yellow
            return $Response.referenceNumber
        } else {
            Write-Host "❌ FAILED: $ServiceType quote submission" -ForegroundColor Red
            Write-Host "Response: $($Response | ConvertTo-Json)" -ForegroundColor Red
            return $null
        }
    } catch {
        Write-Host "❌ ERROR: $ServiceType quote request failed" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Function to test quote tracking
function Test-QuoteTracking {
    param(
        [string]$ReferenceNumber
    )
    
    if (-not $ReferenceNumber) {
        Write-Host "⚠️  Skipping tracking test - no reference number" -ForegroundColor Yellow
        return
    }
    
    Write-Host "`n🔍 Testing Quote Tracking..." -ForegroundColor Blue
    
    try {
        $Uri = "$FullUrl/track/$ReferenceNumber"
        $Response = Invoke-RestMethod -Uri $Uri -Method GET -ErrorAction Stop
        
        Write-Host "✅ SUCCESS: Quote tracking works" -ForegroundColor Green
        Write-Host "📊 Status: $(Get-StatusName $Response.status)" -ForegroundColor Cyan
        Write-Host "🎯 Service: $(Get-ServiceName $Response.serviceType)" -ForegroundColor Cyan
        Write-Host "📅 Created: $([DateTime]::Parse($Response.createdAt).ToString())" -ForegroundColor Cyan
    } catch {
        Write-Host "❌ FAILED: Quote tracking" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Function to test invalid tracking
function Test-InvalidTracking {
    Write-Host "`n🔍 Testing Invalid Reference Number..." -ForegroundColor Blue
    
    try {
        $Uri = "$FullUrl/track/INVALID123"
        Invoke-RestMethod -Uri $Uri -Method GET -ErrorAction Stop
        Write-Host "⚠️  UNEXPECTED: Should have returned 404" -ForegroundColor Yellow
    } catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Host "✅ SUCCESS: Invalid tracking properly returns 404" -ForegroundColor Green
        } else {
            Write-Host "❌ ERROR: Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Helper functions
function Get-StatusName {
    param([int]$Status)
    
    $Statuses = @{
        1 = "Submitted"
        2 = "Under Review"
        3 = "Quote Provided"
        4 = "Payment Pending"
        5 = "Paid"
        6 = "Booking Confirmed"
        7 = "Expired"
        8 = "Cancelled"
    }
    
    return $Statuses[$Status] ?? "Unknown ($Status)"
}

function Get-ServiceName {
    param([int]$ServiceType)
    
    $Services = @{
        1 = "Flight"
        2 = "Hotel"
        3 = "Tour"
        4 = "Visa"
        5 = "Complete Package"
    }
    
    return $Services[$ServiceType] ?? "Unknown ($ServiceType)"
}

# Main test execution
$ReferenceNumbers = @()

# Phase 1: Quote Submission Tests
Write-Host "`n📋 PHASE 1: Quote Submission Tests" -ForegroundColor White

$HotelRef = Test-QuoteSubmission -ServiceType "hotel" -Data $TestData.hotel
if ($HotelRef) { $ReferenceNumbers += $HotelRef }

$FlightRef = Test-QuoteSubmission -ServiceType "flight" -Data $TestData.flight
if ($FlightRef) { $ReferenceNumbers += $FlightRef }

$TourRef = Test-QuoteSubmission -ServiceType "tour" -Data $TestData.tour
if ($TourRef) { $ReferenceNumbers += $TourRef }

# Phase 2: Quote Tracking Tests
Write-Host "`n📋 PHASE 2: Quote Tracking Tests" -ForegroundColor White

if ($ReferenceNumbers.Count -gt 0) {
    Test-QuoteTracking -ReferenceNumber $ReferenceNumbers[0]
}

Test-InvalidTracking

# Summary
Write-Host "`n📊 TEST SUMMARY" -ForegroundColor White
Write-Host "✅ Quote submissions attempted: 3" -ForegroundColor Green
Write-Host "✅ Successful submissions: $($ReferenceNumbers.Count)" -ForegroundColor Green
Write-Host "📋 Reference numbers generated: $($ReferenceNumbers.Count)" -ForegroundColor Cyan

if ($ReferenceNumbers.Count -gt 0) {
    Write-Host "`n📝 Generated Reference Numbers:" -ForegroundColor White
    for ($i = 0; $i -lt $ReferenceNumbers.Count; $i++) {
        Write-Host "$($i + 1). $($ReferenceNumbers[$i])" -ForegroundColor Cyan
    }
    
    Write-Host "`n💡 Use these reference numbers to test tracking in your frontend!" -ForegroundColor Yellow
}

Write-Host "`n🎉 Test Suite Complete!" -ForegroundColor Green
Write-Host "⏰ Finished at: $(Get-Date)" -ForegroundColor Yellow