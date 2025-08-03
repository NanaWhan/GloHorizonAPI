#!/bin/bash

# Test script to verify key endpoints are working
BASE_URL="http://localhost:5080"

echo "🚀 Testing GloHorizon API Endpoints..."
echo "🎯 Testing API at: $BASE_URL"
echo ""

# Test 1: Health check (try getting any endpoint that should return something)
echo "📋 Test 1: API Health Check"
curl -s -w "\nStatus Code: %{http_code}\n" "$BASE_URL/api/user/profile" || echo "❌ API might be down"
echo ""

# Test 2: Quote submission (this should work without auth)
echo "📋 Test 2: Quote Submission Test"
curl -s -w "\nStatus Code: %{http_code}\n" \
  -X POST "$BASE_URL/api/quote/hotel" \
  -H "Content-Type: application/json" \
  -d '{
    "contactName": "Test User",
    "contactEmail": "test@example.com", 
    "contactPhone": "+233123456789",
    "destination": "London",
    "checkInDate": "2024-08-15T00:00:00Z",
    "checkOutDate": "2024-08-20T00:00:00Z",
    "rooms": 1,
    "adultGuests": 2,
    "childGuests": 0,
    "roomType": "standard",
    "preferredHotel": "Test Hotel",
    "starRating": "4-star",
    "amenities": ["wifi", "pool"],
    "specialRequests": "Test request"
  }'
echo ""
echo ""

# Test 3: Registration endpoint
echo "📋 Test 3: User Registration Test"
curl -s -w "\nStatus Code: %{http_code}\n" \
  -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Test",
    "lastName": "User",
    "email": "test'$(date +%s)'@example.com",
    "phoneNumber": "+233'$(date +%s | tail -c 10)'",
    "password": "Test123!",
    "dateOfBirth": "1990-01-01",
    "acceptMarketing": true
  }'
echo ""
echo ""

echo "✅ Basic endpoint testing completed!"
echo "📝 Note: If you see status codes 200-201 for quote submission and 400+ for auth (without token), the API is running correctly."