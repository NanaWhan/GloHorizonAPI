# 🌍 Global Horizons Travel & Tour - Frontend API Integration Guide

## 🚀 **Quick Start**

**Base URL (Production):** `https://glohorizonapi.fly.dev`  
**Base URL (Development):** `http://localhost:5080`

**Content-Type:** `application/json` for all POST requests

---

## 📋 **Table of Contents**

1. [Authentication Endpoints](#authentication-endpoints)
2. [Quote Request System (LIVE)](#quote-request-system-live)
3. [User Management](#user-management)
4. [Travel Packages](#travel-packages)
5. [Booking System](#booking-system)
6. [Payment System](#payment-system)
7. [Admin Endpoints](#admin-endpoints)
8. [Error Handling](#error-handling)
9. [Testing Guide](#testing-guide)

---

## 🔐 **Authentication Endpoints**

### **1. User Registration**
```http
POST /api/auth/register
```

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "fullName": "John Doe",
  "phoneNumber": "+233123456789",
  "dateOfBirth": "1990-01-15T00:00:00Z"
}
```

**Response (Success):**
```json
{
  "success": true,
  "message": "Registration successful",
  "user": {
    "id": "user-uuid",
    "email": "user@example.com",
    "fullName": "John Doe",
    "phoneNumber": "+233123456789"
  },
  "token": "jwt-token-here"
}
```

### **2. User Login**
```http
POST /api/auth/login
```

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response (Success):**
```json
{
  "success": true,
  "message": "Login successful",
  "user": {
    "id": "user-uuid",
    "email": "user@example.com",
    "fullName": "John Doe"
  },
  "token": "jwt-token-here"
}
```

### **3. OTP Login**
```http
POST /api/auth/otp-login
```

**Request Body:**
```json
{
  "phoneNumber": "+233123456789",
  "otp": "123456"
}
```

---

## 🎯 **Quote Request System (LIVE)**

> **✨ No Authentication Required - Perfect for Guest Users!**

### **1. Hotel Quote Request**
```http
POST /api/quote/hotel
```

**Request Body:**
```json
{
  "hotelDetails": {
    "destination": "Dubai",
    "checkInDate": "2024-12-15T00:00:00Z",
    "checkOutDate": "2024-12-22T00:00:00Z",
    "rooms": 2,
    "adultGuests": 4,
    "childGuests": 1,
    "roomType": "suite",
    "starRating": "5-star",
    "amenities": ["pool", "spa", "gym", "beach-access"],
    "preferredHotel": "Burj Al Arab"
  },
  "contactEmail": "customer@example.com",
  "contactPhone": "+233123456789",
  "contactName": "Sarah Johnson",
  "specialRequests": "Honeymoon package with sunset dinner",
  "urgency": 2
}
```

**Response (Success):**
```json
{
  "success": true,
  "message": "Hotel quote request submitted successfully. You will receive a quote within 24 hours.",
  "referenceNumber": "QHT17054939887",
  "quote": {
    "id": 3,
    "referenceNumber": "QHT17054939887",
    "serviceType": 2,
    "status": 1,
    "destination": "Dubai",
    "quotedAmount": null,
    "currency": "GHS",
    "createdAt": "2025-07-31T17:05:25Z",
    "travelDate": "2024-12-15T00:00:00Z",
    "contactEmail": "customer@example.com",
    "contactPhone": "+233123456789",
    "contactName": "Sarah Johnson",
    "specialRequests": "Honeymoon package with sunset dinner",
    "urgency": 2,
    "statusHistory": [
      {
        "fromStatus": 1,
        "toStatus": 1,
        "notes": "Hotel quote request submitted",
        "changedAt": "2025-07-31T17:05:25Z",
        "changedBy": "System"
      }
    ]
  }
}
```

### **2. Flight Quote Request**
```http
POST /api/quote/flight
```

**Request Body:**
```json
{
  "flightDetails": {
    "tripType": "round-trip",
    "departureCity": "Accra",
    "arrivalCity": "London",
    "departureDate": "2024-11-10T00:00:00Z",
    "returnDate": "2024-11-20T00:00:00Z",
    "adultPassengers": 2,
    "childPassengers": 1,
    "infantPassengers": 0,
    "preferredClass": "business",
    "preferredAirline": "British Airways",
    "passengers": [
      {
        "firstName": "Michael",
        "lastName": "Business",
        "dateOfBirth": "1985-03-15",
        "passportNumber": "GH1234567",
        "nationality": "Ghanaian"
      }
    ]
  },
  "contactEmail": "michael@business.com",
  "contactPhone": "+233555123456",
  "contactName": "Michael Business",
  "specialRequests": "Extra legroom seats and kosher meals",
  "urgency": 3
}
```

### **3. Tour Quote Request**
```http
POST /api/quote/tour
```

**Request Body:**
```json
{
  "tourDetails": {
    "tourPackage": "African Safari Adventure",
    "destination": "Kenya",
    "startDate": "2025-01-20T00:00:00Z",
    "endDate": "2025-01-27T00:00:00Z",
    "travelers": 6,
    "accommodationType": "luxury-lodge",
    "tourType": "private",
    "activities": ["game-drives", "hot-air-balloon", "cultural-visits"],
    "mealPlan": "full-board"
  },
  "contactEmail": "adventure@family.com",
  "contactPhone": "+233444567890",
  "contactName": "Adventure Family",
  "specialRequests": "Professional wildlife photographer guide needed",
  "urgency": 1
}
```

### **4. Visa Quote Request**
```http
POST /api/quote/visa
```

**Request Body:**
```json
{
  "visaDetails": {
    "visaType": "Tourist Visa",
    "destinationCountry": "United States",
    "processingType": "express",
    "intendedTravelDate": "2025-03-15T00:00:00Z",
    "durationOfStay": 21,
    "purposeOfVisit": "Tourism and family visit",
    "passportNumber": "GH9876543",
    "passportExpiryDate": "2028-06-30T00:00:00Z",
    "nationality": "Ghanaian",
    "hasPreviousVisa": true,
    "requiredDocuments": [
      {
        "documentType": "bank-statement",
        "documentName": "3-month bank statement",
        "isRequired": true,
        "isUploaded": false
      }
    ]
  },
  "contactEmail": "visa@applicant.com",
  "contactPhone": "+233333456789",
  "contactName": "Visa Applicant",
  "specialRequests": "Need expedited processing",
  "urgency": 3
}
```

### **5. Complete Package Quote Request**
```http
POST /api/quote/complete-package
```

**Request Body:**
```json
{
  "packageDetails": {
    "flightDetails": {
      "tripType": "round-trip",
      "departureCity": "Accra",
      "arrivalCity": "Dubai",
      "departureDate": "2025-02-14T00:00:00Z",
      "returnDate": "2025-02-21T00:00:00Z",
      "adultPassengers": 2,
      "childPassengers": 0,
      "preferredClass": "economy"
    },
    "hotelDetails": {
      "destination": "Dubai",
      "checkInDate": "2025-02-14T00:00:00Z",
      "checkOutDate": "2025-02-21T00:00:00Z",
      "rooms": 1,
      "adultGuests": 2,
      "roomType": "deluxe",
      "starRating": "4-star",
      "amenities": ["pool", "gym", "spa"]
    },
    "visaDetails": {
      "visaType": "Tourist Visa",
      "destinationCountry": "UAE",
      "processingType": "standard",
      "intendedTravelDate": "2025-02-14T00:00:00Z",
      "durationOfStay": 7,
      "purposeOfVisit": "Tourism",
      "passportNumber": "GH5555555",
      "passportExpiryDate": "2027-12-31T00:00:00Z",
      "nationality": "Ghanaian"
    },
    "packageType": "honeymoon",
    "estimatedBudget": 15000,
    "additionalServices": ["travel-insurance", "airport-transfers", "city-tours"]
  },
  "contactEmail": "honeymoon@couple.com",
  "contactPhone": "+233222345678",
  "contactName": "Honeymoon Couple",
  "specialRequests": "Romantic dinner setup and couples spa session",
  "urgency": 2
}
```

### **6. Track Quote (Public)**
```http
GET /api/quote/track/{referenceNumber}
```

**Example:** `GET /api/quote/track/QHT17054939887`

**Response:**
```json
{
  "id": 3,
  "referenceNumber": "QHT17054939887",
  "serviceType": 2,
  "status": 1,
  "destination": "Dubai",
  "quotedAmount": null,
  "currency": "GHS",
  "createdAt": "2025-07-31T17:05:25Z",
  "travelDate": "2024-12-15T00:00:00Z",
  "quoteProvidedAt": null,
  "quoteExpiresAt": null,
  "contactEmail": "customer@example.com",
  "contactPhone": "+233123456789",
  "contactName": "Sarah Johnson",
  "specialRequests": "Honeymoon package with sunset dinner",
  "adminNotes": null,
  "urgency": 2,
  "paymentLinkUrl": null,
  "statusHistory": [...]
}
```

### **7. Get User's Quotes (Authenticated)**
```http
GET /api/quote/my-quotes
Authorization: Bearer {jwt-token}
```

**Response:**
```json
{
  "success": true,
  "message": "Quotes retrieved successfully",
  "quotes": [...],
  "totalCount": 5
}
```

---

## 👤 **User Management**

### **1. Get User Profile**
```http
GET /api/user/profile
Authorization: Bearer {jwt-token}
```

**Response:**
```json
{
  "success": true,
  "user": {
    "id": "user-uuid",
    "email": "user@example.com",
    "fullName": "John Doe",
    "phoneNumber": "+233123456789",
    "dateOfBirth": "1990-01-15T00:00:00Z",
    "createdAt": "2025-01-01T00:00:00Z"
  }
}
```

### **2. Update User Profile**
```http
PUT /api/user/profile
Authorization: Bearer {jwt-token}
```

**Request Body:**
```json
{
  "fullName": "John Updated Doe",
  "phoneNumber": "+233987654321",
  "dateOfBirth": "1990-01-15T00:00:00Z"
}
```

---

## 🎒 **Travel Packages**

### **1. Get All Packages**
```http
GET /api/travelpackage
```

**Response:**
```json
{
  "success": true,
  "packages": [
    {
      "id": 1,
      "title": "Dubai Adventure Package",
      "description": "7-day luxury experience in Dubai",
      "price": 5500.00,
      "currency": "GHS",
      "duration": 7,
      "destination": "Dubai",
      "imageUrl": "https://example.com/dubai.jpg",
      "features": ["5-star hotel", "City tours", "Desert safari"],
      "isActive": true
    }
  ]
}
```

### **2. Get Package by ID**
```http
GET /api/travelpackage/{id}
```

### **3. Create Package (Admin Only)**
```http
POST /api/travelpackage
Authorization: Bearer {admin-jwt-token}
```

**Request Body:**
```json
{
  "title": "Paris Romance Package",
  "description": "5-day romantic getaway in Paris",
  "price": 4500.00,
  "currency": "GHS",
  "duration": 5,
  "destination": "Paris",
  "imageUrl": "https://example.com/paris.jpg",
  "features": ["4-star hotel", "Seine river cruise", "Eiffel Tower dinner"]
}
```

---

## 📋 **Booking System**

### **1. Submit Booking Request**
```http
POST /api/booking/submit
Authorization: Bearer {jwt-token}
```

**Request Body:**
```json
{
  "serviceType": 2,
  "destination": "Dubai",
  "travelDate": "2024-12-15T00:00:00Z",
  "contactEmail": "user@example.com",
  "contactPhone": "+233123456789",
  "specialRequests": "Airport pickup required",
  "urgency": 1,
  "hotelDetails": {
    "destination": "Dubai",
    "checkInDate": "2024-12-15T00:00:00Z",
    "checkOutDate": "2024-12-22T00:00:00Z",
    "rooms": 1,
    "adultGuests": 2
  }
}
```

### **2. Track Booking**
```http
GET /api/booking/track/{trackingNumber}
```

---

## 💳 **Payment System**

### **1. Initiate Payment**
```http
POST /api/payment/initiate
Authorization: Bearer {jwt-token}
```

**Request Body:**
```json
{
  "bookingId": 123,
  "amount": 5500.00,
  "currency": "GHS",
  "paymentMethod": "paystack"
}
```

**Response:**
```json
{
  "success": true,
  "paymentUrl": "https://checkout.paystack.com/...",
  "reference": "pay_ref_123456",
  "message": "Payment initialized successfully"
}
```

### **2. Payment Callback**
```http
POST /api/payment/callback
```

---

## 🔧 **Admin Endpoints**

### **1. Admin Login**
```http
POST /api/admin/login
```

**Request Body:**
```json
{
  "email": "admin@globalhorizons.com",
  "password": "AdminPass123!"
}
```

### **2. Get All Bookings**
```http
GET /api/admin/bookings
Authorization: Bearer {admin-jwt-token}
```

### **3. Update Booking Status**
```http
PUT /api/admin/bookings/{id}/status
Authorization: Bearer {admin-jwt-token}
```

**Request Body:**
```json
{
  "status": 3,
  "adminNotes": "Booking confirmed and processed"
}
```

---

## 🚨 **Error Handling**

### **Standard Error Response Format:**
```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

### **Common HTTP Status Codes:**
- `200` - Success
- `201` - Created successfully
- `400` - Bad Request (validation errors)
- `401` - Unauthorized (invalid/missing token)
- `403` - Forbidden (insufficient permissions)
- `404` - Not Found
- `409` - Conflict (duplicate data)
- `500` - Internal Server Error

### **Validation Error Example:**
```json
{
  "success": false,
  "message": "Invalid request data. Please check your input and try again.",
  "errors": [
    "Contact email is required",
    "Contact phone is required",
    "Check-in date must be in the future"
  ]
}
```

---

## 🧪 **Testing Guide**

### **Working Test Reference Numbers:**
- Hotel: `QHT17054939887`
- Flight: `QFL17054981853`
- Tour: `QTR17055023233`
- Visa: `QVS17055066240`
- Package: `QCP17055107498`

### **Test Phone Numbers:**
- Ghana: `+233123456789`, `0541458512`
- International: `+1234567890`

### **Test Emails:**
- Customer: `test@example.com`, `customer@globalhorizons.com`
- Admin: `admin@globalhorizons.com`

### **cURL Example:**
```bash
curl -X POST https://glohorizonapi.fly.dev/api/quote/hotel \
  -H "Content-Type: application/json" \
  -d '{
    "hotelDetails": {
      "destination": "Dubai",
      "checkInDate": "2024-12-15T00:00:00Z",
      "checkOutDate": "2024-12-22T00:00:00Z",
      "rooms": 1,
      "adultGuests": 2,
      "roomType": "deluxe",
      "starRating": "4-star"
    },
    "contactEmail": "test@example.com",
    "contactPhone": "+233123456789",
    "contactName": "Test User",
    "urgency": 1
  }'
```

---

## 📊 **Data Reference**

### **Service Types (Enums):**
```json
{
  "Flight": 1,
  "Hotel": 2,
  "Tour": 3,
  "Visa": 4,
  "CompletePackage": 5
}
```

### **Quote Status (Enums):**
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

### **Urgency Levels:**
```json
{
  "Standard": 1,
  "Urgent": 2,
  "Emergency": 3
}
```

### **Room Types:**
- `standard`, `deluxe`, `suite`, `executive`

### **Star Ratings:**
- `3-star`, `4-star`, `5-star`

### **Flight Classes:**
- `economy`, `business`, `first`

### **Trip Types:**
- `one-way`, `round-trip`, `multi-city`

### **Processing Types (Visa):**
- `standard`, `express`, `super-express`

---

## 🌍 **Real-time Notifications**

When quotes are submitted, the system automatically sends:

### **Customer Notifications:**
- **SMS:** Personalized confirmation with reference number
- **Email:** Detailed confirmation with next steps

### **Admin Notifications:**
- **SMS:** Instant alert with customer details and urgency
- **Email:** Comprehensive request details for processing

**Example SMS to Customer:**
> "Hello Sarah Johnson! Your Hotel quote request (QHT17054939887) has been received. We'll respond within 24hrs. Thank you for choosing Global Horizons Travel & Tour! 🌍"

---

## 🎯 **Frontend Integration Tips**

1. **Quote System is Guest-Friendly:** No authentication required for quote submissions
2. **Reference Numbers:** Always display and allow users to track quotes
3. **Urgency Levels:** Use color coding (Standard=Green, Urgent=Orange, Emergency=Red)
4. **Real-time Updates:** Consider polling `/api/quote/track/{ref}` for status updates
5. **Error Handling:** Always handle validation errors gracefully
6. **Mobile-First:** All endpoints are optimized for mobile applications
7. **Notifications:** Inform users they'll receive SMS/Email confirmations

---

## 📞 **Support**

**Business:** Global Horizons Travel & Tour  
**API Base URL:** https://glohorizonapi.fly.dev  
**Contact:** +233 24 905 8729  
**Email:** admin@globalhorizonstravel.com

---

**🚀 Ready to build amazing travel experiences? Start integrating today!**