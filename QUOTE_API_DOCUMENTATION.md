# 🎯 GloHorizon Quote Request API Documentation

## 📋 Overview

The GloHorizon Quote Request API allows users to submit travel service quote requests without authentication. This system is designed for both **guest users** and **registered users** to easily request quotes for various travel services.

### 🔗 Base URL
```
https://your-api-domain.com/api/quote
```

---

## 🚀 Quote Request Endpoints

### ✈️ Flight Quote Request
**POST** `/api/quote/flight`

Submit a flight quote request (no authentication required).

**Request Body:**
```json
{
  "flightDetails": {
    "tripType": "round-trip",
    "departureCity": "Accra",
    "arrivalCity": "London",
    "departureDate": "2024-08-15T00:00:00Z",
    "returnDate": "2024-08-25T00:00:00Z",
    "adultPassengers": 2,
    "childPassengers": 1,
    "infantPassengers": 0,
    "preferredClass": "economy",
    "preferredAirline": "British Airways",
    "passengers": [
      {
        "firstName": "John",
        "lastName": "Doe",
        "dateOfBirth": "1990-01-15",
        "passportNumber": "A12345678",
        "nationality": "Ghanaian"
      }
    ]
  },
  "contactEmail": "john.doe@example.com",
  "contactPhone": "+233123456789",
  "contactName": "John Doe",
  "specialRequests": "Window seat preferred",
  "urgency": 1
}
```

---

### 🏨 Hotel Quote Request
**POST** `/api/quote/hotel`

Submit a hotel quote request (no authentication required).

**Request Body:**
```json
{
  "hotelDetails": {
    "destination": "Dubai",
    "checkInDate": "2024-08-15T00:00:00Z",
    "checkOutDate": "2024-08-20T00:00:00Z",
    "rooms": 1,
    "adultGuests": 2,
    "childGuests": 1,
    "roomType": "deluxe",
    "preferredHotel": "Burj Al Arab",
    "starRating": "5-star",
    "amenities": ["pool", "spa", "gym"]
  },
  "contactEmail": "john.doe@example.com",
  "contactPhone": "+233123456789",
  "contactName": "John Doe",
  "specialRequests": "High floor room with sea view",
  "urgency": 1
}
```

---

### 🗺️ Tour Quote Request
**POST** `/api/quote/tour`

Submit a tour package quote request (no authentication required).

**Request Body:**
```json
{
  "tourDetails": {
    "tourPackage": "Safari Adventure",
    "destination": "Kenya",
    "startDate": "2024-09-01T00:00:00Z",
    "endDate": "2024-09-07T00:00:00Z",
    "travelers": 4,
    "accommodationType": "luxury",
    "tourType": "private",
    "activities": ["game drives", "cultural visits", "photography"],
    "mealPlan": "full-board"
  },
  "contactEmail": "john.doe@example.com",
  "contactPhone": "+233123456789",
  "contactName": "John Doe",
  "specialRequests": "Photography guide required",
  "urgency": 2
}
```

---

### 📄 Visa Quote Request
**POST** `/api/quote/visa`

Submit a visa processing quote request (no authentication required).

**Request Body:**
```json
{
  "visaDetails": {
    "visaType": "Tourist Visa",
    "destinationCountry": "United States",
    "processingType": "standard",
    "intendedTravelDate": "2024-10-01T00:00:00Z",
    "durationOfStay": 14,
    "purposeOfVisit": "Tourism",
    "passportNumber": "A12345678",
    "passportExpiryDate": "2030-01-01T00:00:00Z",
    "nationality": "Ghanaian",
    "hasPreviousVisa": false,
    "requiredDocuments": [
      {
        "documentType": "passport",
        "documentName": "Valid Passport",
        "isRequired": true,
        "isUploaded": false
      }
    ]
  },
  "contactEmail": "john.doe@example.com",
  "contactPhone": "+233123456789",
  "contactName": "John Doe",
  "specialRequests": "Need expedited processing",
  "urgency": 3
}
```

---

### 📦 Complete Package Quote Request
**POST** `/api/quote/complete-package`

Submit a complete travel package quote request (flight + hotel + visa).

**Request Body:**
```json
{
  "packageDetails": {
    "flightDetails": {
      "tripType": "round-trip",
      "departureCity": "Accra",
      "arrivalCity": "New York",
      "departureDate": "2024-11-01T00:00:00Z",
      "returnDate": "2024-11-15T00:00:00Z",
      "adultPassengers": 2,
      "preferredClass": "business"
    },
    "hotelDetails": {
      "destination": "New York",
      "checkInDate": "2024-11-01T00:00:00Z",
      "checkOutDate": "2024-11-15T00:00:00Z",
      "rooms": 1,
      "adultGuests": 2,
      "roomType": "suite",
      "starRating": "4-star"
    },
    "visaDetails": {
      "visaType": "B1/B2 Tourist Visa",
      "destinationCountry": "United States",
      "processingType": "standard",
      "intendedTravelDate": "2024-11-01T00:00:00Z",
      "durationOfStay": 14,
      "purposeOfVisit": "Tourism"
    },
    "packageType": "luxury",
    "estimatedBudget": 15000,
    "additionalServices": ["travel insurance", "airport transfers"]
  },
  "contactEmail": "john.doe@example.com",
  "contactPhone": "+233123456789",
  "contactName": "John Doe",
  "specialRequests": "Honeymoon package with special amenities",
  "urgency": 1
}
```

---

## 📊 Quote Tracking Endpoints

### 🔍 Track Quote by Reference Number
**GET** `/api/quote/track/{referenceNumber}`

Track any quote using its reference number (no authentication required).

**Example:**
```
GET /api/quote/track/GHQHT20240801143022456
```

**Response:**
```json
{
  "id": 123,
  "referenceNumber": "GHQHT20240801143022456",
  "serviceType": 2,
  "status": 1,
  "destination": "Dubai",
  "quotedAmount": null,
  "currency": "GHS",
  "createdAt": "2024-08-01T14:30:22Z",
  "travelDate": "2024-08-15T00:00:00Z",
  "quoteProvidedAt": null,
  "quoteExpiresAt": null,
  "contactEmail": "john.doe@example.com",
  "contactPhone": "+233123456789",
  "contactName": "John Doe",
  "specialRequests": "High floor room with sea view",
  "adminNotes": null,
  "urgency": 1,
  "paymentLinkUrl": null,
  "statusHistory": [
    {
      "fromStatus": 1,
      "toStatus": 1,
      "notes": "Hotel quote request submitted",
      "changedAt": "2024-08-01T14:30:22Z",
      "changedBy": "System"
    }
  ]
}
```

---

### 📋 Get My Quotes (Registered Users Only)
**GET** `/api/quote/my-quotes`

Get all quotes for the authenticated user.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Quotes retrieved successfully",
  "quotes": [
    {
      "id": 123,
      "referenceNumber": "GHQHT20240801143022456",
      "serviceType": 2,
      "status": 1,
      "destination": "Dubai",
      "quotedAmount": null,
      "currency": "GHS",
      "createdAt": "2024-08-01T14:30:22Z"
    }
  ],
  "totalCount": 1
}
```

---

## 📝 Response Format

### ✅ Successful Quote Submission
```json
{
  "success": true,
  "message": "Hotel quote request submitted successfully. You will receive a quote within 24 hours.",
  "referenceNumber": "GHQHT20240801143022456",
  "quote": {
    "id": 123,
    "referenceNumber": "GHQHT20240801143022456",
    "serviceType": 2,
    "status": 1,
    "destination": "Dubai",
    "createdAt": "2024-08-01T14:30:22Z",
    "contactEmail": "john.doe@example.com",
    "contactPhone": "+233123456789"
  }
}
```

### ❌ Error Response
```json
{
  "success": false,
  "message": "Error submitting Hotel quote request. Please try again.",
  "referenceNumber": null,
  "quote": null
}
```

---

## 🔢 Enums and Constants

### Service Types (QuoteType)
```json
{
  "Flight": 1,
  "Hotel": 2,
  "Tour": 3,
  "Visa": 4,
  "CompletePackage": 5
}
```

### Quote Status (QuoteStatus)
```json
{
  "Submitted": 1,
  "UnderReview": 2,
  "QuoteProvided": 3,
  "PaymentPending": 4,
  "Paid": 5,
  "BookingConfirmed": 6,
  "Expired": 7,
  "Cancelled": 8
}
```

### Urgency Levels
```json
{
  "Standard": 1,
  "Urgent": 2,
  "Emergency": 3
}
```

---

## 📋 Field Requirements

### Required Fields for All Requests
- `contactEmail` (valid email format)
- `contactPhone` (valid phone format)
- Service-specific details object
- `urgency` (1-3)

### Optional Fields
- `contactName` (will use registered user's name if logged in)
- `specialRequests` (max 1000 characters)

---

## 🎯 Reference Number Format

Quote reference numbers follow this pattern:
```
GH{ServicePrefix}{Timestamp}{RandomNumber}
```

**Service Prefixes:**
- `QFL` - Flight quotes
- `QHT` - Hotel quotes  
- `QTR` - Tour quotes
- `QVS` - Visa quotes
- `QCP` - Complete package quotes

**Example:** `GHQHT20240801143022456`

---

## 🔔 Notifications

### Customer Notifications
- **Email confirmation** sent immediately after quote submission
- **SMS confirmation** with reference number
- **Quote ready notifications** when admin provides quote
- **Status update notifications** for quote changes

### Admin Notifications
- **Instant email alerts** for new quote requests
- **SMS alerts** for urgent/emergency requests
- **Priority-based notifications** based on urgency level

---

## 🛡️ Error Handling

### Common HTTP Status Codes
- `200 OK` - Successful quote submission/retrieval
- `400 Bad Request` - Invalid request data
- `404 Not Found` - Quote not found
- `401 Unauthorized` - Invalid/missing token (for authenticated endpoints)
- `500 Internal Server Error` - Server error

### Validation Errors
The API validates all required fields and formats. Error messages will specify which fields are invalid.

---

## 🚀 Getting Started

### For Guest Users
1. Choose the appropriate quote endpoint
2. Fill in all required fields
3. Submit POST request
4. Save the returned reference number
5. Use reference number to track quote status

### For Registered Users
1. Same as guest users, but can optionally include Authorization header
2. Can view all quotes via `/api/quote/my-quotes`
3. Better experience with saved contact information

---

## 💡 Best Practices

### Frontend Implementation Tips
1. **Save reference numbers** - Store them locally for easy tracking
2. **Validate before submit** - Check required fields client-side
3. **Handle urgency levels** - Show appropriate UI for urgent requests
4. **Provide feedback** - Show loading states and success messages
5. **Track status** - Allow users to check quote status easily

### UX Recommendations
- Pre-fill contact info for logged-in users
- Provide estimated response times (24 hours standard)
- Show urgency level impact on pricing/timing
- Allow easy copying of reference numbers
- Provide clear status explanations

---

## 📞 Support

For technical support or API issues, contact the GloHorizon development team.

**Remember:** All quote endpoints work without authentication, making it easy for anyone to request travel quotes! 🌟