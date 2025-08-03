# 🌍 GloHorizon API - Frontend Integration Guide

## 📋 Table of Contents
- [Base URLs & Setup](#base-urls--setup)
- [Authentication System](#authentication-system)
- [Quote System](#quote-system)
  - [Flight Quotes](#flight-quotes)
  - [Hotel Quotes](#hotel-quotes) 
  - [Visa Quotes](#visa-quotes)
  - [Tour Quotes](#tour-quotes)
  - [Complete Package Quotes](#complete-package-quotes)
- [TypeScript Interfaces](#typescript-interfaces)
- [Error Handling](#error-handling)
- [Testing & Examples](#testing--examples)

---

## 🔗 Base URLs & Setup

### Production API
```
https://glohorizonapi.fly.dev
```

### Development API
```
http://localhost:5080
```

### Headers
```javascript
// For all requests
{
  "Content-Type": "application/json"
}

// For authenticated requests
{
  "Content-Type": "application/json",
  "Authorization": "Bearer YOUR_JWT_TOKEN"
}
```

---

## 🔐 Authentication System

### 1. User Registration
**Endpoint**: `POST /api/auth/register`

#### Request Body:
```typescript
interface RegisterRequest {
  firstName: string;           // Required, max 100 chars
  lastName: string;            // Required, max 100 chars  
  email: string;               // Required, valid email format
  phoneNumber: string;         // Required, Ghanaian format (+233xxxxxxxxx)
  password: string;            // Required, min 8 chars, include special chars
  dateOfBirth: string;         // Required, ISO format: "1990-01-01T00:00:00Z"
  acceptMarketing: boolean;    // Required, true/false
}
```

#### Example Request:
```javascript
const registerUser = async () => {
  const response = await fetch('https://glohorizonapi.fly.dev/api/auth/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      firstName: "John",
      lastName: "Doe", 
      email: "john.doe@example.com",
      phoneNumber: "+233245678901",
      password: "SecurePass123!",
      dateOfBirth: "1990-05-15T00:00:00Z",
      acceptMarketing: true
    })
  });
  
  return await response.json();
};
```

#### Response:
```typescript
interface AuthResponse {
  success: boolean;
  message: string;
  token: string | null;        // JWT token if successful
  user: {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    role: string;              // "User" or "Admin"
    createdAt: string;         // ISO date string
  } | null;
}
```

### 2. User Login
**Endpoint**: `POST /api/auth/login`

#### Request Body:
```typescript
interface LoginRequest {
  email: string;               // Required
  password: string;            // Required
}
```

#### Example Request:
```javascript
const loginUser = async (email, password) => {
  const response = await fetch('https://glohorizonapi.fly.dev/api/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      email: email,
      password: password
    })
  });
  
  const data = await response.json();
  
  // Store token for authenticated requests
  if (data.success && data.token) {
    localStorage.setItem('authToken', data.token);
  }
  
  return data;
};
```

### 3. OTP Verification System
**Step 1 - Request OTP**: `POST /api/auth/request-otp`

```typescript
interface OTPRequest {
  phoneNumber: string;         // Required, +233 format
}
```

**Step 2 - Verify OTP**: `POST /api/auth/verify-otp`

```typescript
interface OTPVerifyRequest {
  phoneNumber: string;         // Required
  otpCode: string;             // Required, 6-digit code
}
```

#### Example OTP Flow:
```javascript
// Step 1: Request OTP
const requestOTP = async (phoneNumber) => {
  const response = await fetch('https://glohorizonapi.fly.dev/api/auth/request-otp', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ phoneNumber })
  });
  return await response.json();
};

// Step 2: Verify OTP  
const verifyOTP = async (phoneNumber, otpCode) => {
  const response = await fetch('https://glohorizonapi.fly.dev/api/auth/verify-otp', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ phoneNumber, otpCode })
  });
  
  const data = await response.json();
  if (data.success && data.token) {
    localStorage.setItem('authToken', data.token);
  }
  return data;
};
```

### 4. Logout
**Endpoint**: `POST /api/auth/logout`

```javascript
const logout = async () => {
  const token = localStorage.getItem('authToken');
  
  const response = await fetch('https://glohorizonapi.fly.dev/api/auth/logout', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  
  // Clear local storage
  localStorage.removeItem('authToken');
  return await response.json();
};
```

---

## 💬 Quote System

> **Note**: All quote endpoints are **PUBLIC** - no authentication required!

### Common Quote Properties
```typescript
interface BaseQuoteRequest {
  contactEmail: string;        // Required, valid email
  contactPhone: string;        // Required, phone number
  contactName: string;         // Required, full name
  specialRequests?: string;    // Optional, max 1000 chars
  urgency: 0 | 1 | 2;         // Required: 0=Standard, 1=Urgent, 2=Emergency
}

interface QuoteResponse {
  success: boolean;
  message: string;
  referenceNumber: string;     // Track quote with this
  quote: {
    id: number;
    referenceNumber: string;
    serviceType: number;       // 1=Flight, 2=Hotel, 3=Tour, 4=Visa, 5=Package
    status: number;            // 1=Submitted, 2=UnderReview, 3=QuoteProvided
    destination: string;
    quotedAmount: number | null;
    currency: string;          // "GHS"
    createdAt: string;         // ISO date
    travelDate: string;        // ISO date
    contactEmail: string;
    contactPhone: string;
    contactName: string;
    specialRequests: string | null;
    urgency: number;
    statusHistory: Array<{
      fromStatus: number;
      toStatus: number;
      notes: string;
      changedAt: string;
      changedBy: string;
    }>;
  };
}
```

---

### ✈️ Flight Quotes
**Endpoint**: `POST /api/quote/flight`

#### Request Body:
```typescript
interface FlightQuoteRequest extends BaseQuoteRequest {
  flightDetails: {
    departureCity: string;     // Required, e.g., "Accra"
    arrivalCity: string;       // Required, e.g., "Dubai"
    departureDate: string;     // Required, ISO format: "2025-09-01T08:00:00Z"
    returnDate?: string;       // Optional for one-way, required for round-trip
    passengerCount: number;    // Required, min 1, max 50
    travelClass: "Economy" | "Premium Economy" | "Business" | "First";
    tripType: "OneWay" | "RoundTrip";  // Required
  };
}
```

#### Complete Example:
```javascript
const requestFlightQuote = async () => {
  const response = await fetch('https://glohorizonapi.fly.dev/api/quote/flight', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      contactEmail: "customer@example.com",
      contactPhone: "+233245678901", 
      contactName: "John Doe",
      specialRequests: "Window seat preferred, vegetarian meal",
      urgency: 0,  // Standard urgency
      flightDetails: {
        departureCity: "Accra",
        arrivalCity: "London",
        departureDate: "2025-10-15T08:00:00Z",
        returnDate: "2025-10-25T18:00:00Z",  // For round trip
        passengerCount: 2,
        travelClass: "Business",
        tripType: "RoundTrip"
      }
    })
  });
  
  const data = await response.json();
  console.log('Quote Reference:', data.referenceNumber); // Save this!
  return data;
};
```

---

### 🏨 Hotel Quotes  
**Endpoint**: `POST /api/quote/hotel`

#### Request Body:
```typescript
interface HotelQuoteRequest extends BaseQuoteRequest {
  hotelDetails: {
    destination: string;       // Required, e.g., "Dubai" 
    checkInDate: string;       // Required, ISO format: "2025-09-01T14:00:00Z"
    checkOutDate: string;      // Required, ISO format: "2025-09-05T12:00:00Z"
    roomCount: number;         // Required, min 1
    guestCount: number;        // Required, min 1
    roomType: "Standard" | "Deluxe" | "Suite" | "Presidential"; // Required
  };
}
```

#### Complete Example:
```javascript
const requestHotelQuote = async () => {
  const response = await fetch('https://glohorizonapi.fly.dev/api/quote/hotel', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      contactEmail: "customer@example.com",
      contactPhone: "+233245678901",
      contactName: "John Doe", 
      specialRequests: "Ocean view room, late checkout",
      urgency: 1,  // Urgent
      hotelDetails: {
        destination: "Maldives",
        checkInDate: "2025-12-01T14:00:00Z",
        checkOutDate: "2025-12-07T12:00:00Z", 
        roomCount: 1,
        guestCount: 2,
        roomType: "Suite"
      }
    })
  });
  
  return await response.json();
};
```

---

### 📄 Visa Quotes
**Endpoint**: `POST /api/quote/visa`

#### Request Body:
```typescript
interface VisaQuoteRequest extends BaseQuoteRequest {
  visaDetails: {
    destinationCountry: string;        // Required, e.g., "Canada"
    visaType: "Tourist" | "Business" | "Student" | "Work" | "Transit";
    intendedTravelDate: string;        // Required, ISO format
    processingTime: "Standard" | "Express" | "Emergency";
    passportNumber: string;            // Required
    nationality: string;               // Required, e.g., "Ghanaian"
    durationOfStay: number;            // Required, days (1-365)
    purposeOfVisit: string;            // Required, e.g., "Tourism"
  };
}
```

#### Complete Example:
```javascript
const requestVisaQuote = async () => {
  const response = await fetch('https://glohorizonapi.fly.dev/api/quote/visa', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      contactEmail: "customer@example.com",
      contactPhone: "+233245678901",
      contactName: "John Doe",
      specialRequests: "Express processing needed for business trip",
      urgency: 2,  // Emergency
      visaDetails: {
        destinationCountry: "Canada",
        visaType: "Business",
        intendedTravelDate: "2025-11-01T08:00:00Z",
        processingTime: "Express",
        passportNumber: "A1234567",
        nationality: "Ghanaian", 
        durationOfStay: 14,
        purposeOfVisit: "Business meetings and conferences"
      }
    })
  });
  
  return await response.json();
};
```

---

### 🗺️ Tour Quotes
**Endpoint**: `POST /api/quote/tour`

#### Request Body:
```typescript
interface TourQuoteRequest extends BaseQuoteRequest {
  tourDetails: {
    destination: string;               // Required, e.g., "Egypt"
    startDate: string;                 // Required, ISO format
    endDate: string;                   // Required, ISO format  
    groupSize: number;                 // Required, min 1
    tourType: "Cultural" | "Adventure" | "Safari" | "Historical" | "Beach" | "City";
    accommodationType: "Hotel" | "Resort" | "Guesthouse" | "Camping";
    transportationType: "Bus" | "Van" | "Car" | "Flight";
    tourPackage: string;               // Required, e.g., "Premium Egypt Cultural Tour"
  };
}
```

#### Complete Example:
```javascript
const requestTourQuote = async () => {
  const response = await fetch('https://glohorizonapi.fly.dev/api/quote/tour', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      contactEmail: "customer@example.com",
      contactPhone: "+233245678901",
      contactName: "John Doe",
      specialRequests: "Photography tour, early morning starts preferred",
      urgency: 0,  // Standard
      tourDetails: {
        destination: "Egypt",
        startDate: "2025-12-01T08:00:00Z",
        endDate: "2025-12-10T18:00:00Z",
        groupSize: 4,
        tourType: "Cultural",
        accommodationType: "Hotel", 
        transportationType: "Bus",
        tourPackage: "Premium Egypt Cultural & Historical Tour Package"
      }
    })
  });
  
  return await response.json();
};
```

---

### 📦 Complete Package Quotes
**Endpoint**: `POST /api/quote/complete-package`

#### Request Body:
```typescript
interface CompletePackageRequest extends BaseQuoteRequest {
  packageDetails: {
    packageType: "Honeymoon" | "Family" | "Business" | "Group" | "Solo";
    flightDetails: {
      departureCity: string;
      arrivalCity: string;
      departureDate: string;
      returnDate: string;
      passengerCount: number;
      travelClass: "Economy" | "Premium Economy" | "Business" | "First";
      tripType: "OneWay" | "RoundTrip";
    };
    hotelDetails: {
      destination: string;
      checkInDate: string;
      checkOutDate: string;
      roomCount: number;
      guestCount: number;
      roomType: "Standard" | "Deluxe" | "Suite" | "Presidential";
    };
    visaDetails?: {  // Optional
      destinationCountry: string;
      visaType: "Tourist" | "Business" | "Student" | "Work" | "Transit";
      intendedTravelDate: string;
      processingTime: "Standard" | "Express" | "Emergency";
      passportNumber: string;
      nationality: string;
      durationOfStay: number;
      purposeOfVisit: string;
    };
  };
}
```

#### Complete Example:
```javascript
const requestCompletePackage = async () => {
  const response = await fetch('https://glohorizonapi.fly.dev/api/quote/complete-package', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      contactEmail: "customer@example.com",
      contactPhone: "+233245678901",
      contactName: "John & Jane Doe",
      specialRequests: "Honeymoon package with romantic dinners and spa treatments",
      urgency: 0,
      packageDetails: {
        packageType: "Honeymoon",
        flightDetails: {
          departureCity: "Accra",
          arrivalCity: "Paris",
          departureDate: "2025-12-15T08:00:00Z",
          returnDate: "2025-12-25T18:00:00Z",
          passengerCount: 2,
          travelClass: "Business",
          tripType: "RoundTrip"
        },
        hotelDetails: {
          destination: "Paris",
          checkInDate: "2025-12-15T14:00:00Z", 
          checkOutDate: "2025-12-25T12:00:00Z",
          roomCount: 1,
          guestCount: 2,
          roomType: "Suite"
        },
        visaDetails: {
          destinationCountry: "France",
          visaType: "Tourist",
          intendedTravelDate: "2025-12-15T08:00:00Z",
          processingTime: "Standard",
          passportNumber: "A1234567",
          nationality: "Ghanaian",
          durationOfStay: 10,
          purposeOfVisit: "Tourism and honeymoon"
        }
      }
    })
  });
  
  return await response.json();
};
```

---

### 📍 Quote Tracking
**Endpoint**: `GET /api/quote/track/{referenceNumber}`

```javascript
const trackQuote = async (referenceNumber) => {
  const response = await fetch(`https://glohorizonapi.fly.dev/api/quote/track/${referenceNumber}`);
  return await response.json();
};
```

---

## 🔤 TypeScript Interfaces

### Complete Interface Definitions:
```typescript
// === AUTH INTERFACES ===
interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  password: string;
  dateOfBirth: string;
  acceptMarketing: boolean;
}

interface LoginRequest {
  email: string;
  password: string;
}

interface AuthResponse {
  success: boolean;
  message: string;
  token: string | null;
  user: {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    role: string;
    createdAt: string;
  } | null;
}

// === QUOTE INTERFACES ===
type UrgencyLevel = 0 | 1 | 2;  // Standard, Urgent, Emergency
type ServiceType = 1 | 2 | 3 | 4 | 5;  // Flight, Hotel, Tour, Visa, Package

interface BaseQuoteRequest {
  contactEmail: string;
  contactPhone: string;
  contactName: string;
  specialRequests?: string;
  urgency: UrgencyLevel;
}

interface FlightDetails {
  departureCity: string;
  arrivalCity: string;
  departureDate: string;
  returnDate?: string;
  passengerCount: number;
  travelClass: "Economy" | "Premium Economy" | "Business" | "First";
  tripType: "OneWay" | "RoundTrip";
}

interface HotelDetails {
  destination: string;
  checkInDate: string;
  checkOutDate: string;
  roomCount: number;
  guestCount: number;
  roomType: "Standard" | "Deluxe" | "Suite" | "Presidential";
}

interface VisaDetails {
  destinationCountry: string;
  visaType: "Tourist" | "Business" | "Student" | "Work" | "Transit";
  intendedTravelDate: string;
  processingTime: "Standard" | "Express" | "Emergency";
  passportNumber: string;
  nationality: string;
  durationOfStay: number;
  purposeOfVisit: string;
}

interface TourDetails {
  destination: string;
  startDate: string;
  endDate: string;
  groupSize: number;
  tourType: "Cultural" | "Adventure" | "Safari" | "Historical" | "Beach" | "City";
  accommodationType: "Hotel" | "Resort" | "Guesthouse" | "Camping";
  transportationType: "Bus" | "Van" | "Car" | "Flight";
  tourPackage: string;
}

// === SPECIFIC QUOTE REQUESTS ===
interface FlightQuoteRequest extends BaseQuoteRequest {
  flightDetails: FlightDetails;
}

interface HotelQuoteRequest extends BaseQuoteRequest {
  hotelDetails: HotelDetails;
}

interface VisaQuoteRequest extends BaseQuoteRequest {
  visaDetails: VisaDetails;
}

interface TourQuoteRequest extends BaseQuoteRequest {
  tourDetails: TourDetails;
}

interface CompletePackageRequest extends BaseQuoteRequest {
  packageDetails: {
    packageType: "Honeymoon" | "Family" | "Business" | "Group" | "Solo";
    flightDetails: FlightDetails;
    hotelDetails: HotelDetails;
    visaDetails?: VisaDetails;
  };
}
```

---

## ⚠️ Error Handling

### Common HTTP Status Codes:
- **200**: Success
- **400**: Bad Request (validation errors)
- **401**: Unauthorized (invalid/missing token)
- **404**: Not Found
- **500**: Internal Server Error

### Error Response Format:
```typescript
interface ErrorResponse {
  success: false;
  message: string;
  errors?: {
    [field: string]: string[];  // Validation errors
  };
}
```

### Error Handling Example:
```javascript
const handleApiCall = async (apiFunction) => {
  try {
    const response = await apiFunction();
    
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.message || 'API call failed');
    }
    
    const data = await response.json();
    
    if (!data.success) {
      throw new Error(data.message || 'Operation failed');
    }
    
    return data;
  } catch (error) {
    console.error('API Error:', error.message);
    throw error;
  }
};
```

---

## 🧪 Testing & Examples

### Quick Test Function:
```javascript
const testAPI = async () => {
  try {
    // Test quote endpoint
    const quoteResponse = await fetch('https://glohorizonapi.fly.dev/api/quote/test-minimal', {
      method: 'POST'
    });
    const quoteData = await quoteResponse.json();
    console.log('API Status:', quoteData.success ? 'Working ✅' : 'Error ❌');
    
    return quoteData;
  } catch (error) {
    console.error('API Test Failed:', error);
  }
};

// Call this to test API connectivity
testAPI();
```

### Environment Configuration:
```javascript
const API_CONFIG = {
  production: 'https://glohorizonapi.fly.dev',
  development: 'http://localhost:5080',
  current: process.env.NODE_ENV === 'production' 
    ? 'https://glohorizonapi.fly.dev' 
    : 'http://localhost:5080'
};

const apiCall = (endpoint, options = {}) => {
  return fetch(`${API_CONFIG.current}${endpoint}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers
    },
    ...options
  });
};
```

---

## 📞 Support & Questions

For additional support or questions:
- **API Status**: All endpoints tested and working ✅
- **Reference Numbers**: Always save quote reference numbers for tracking
- **Phone Format**: Use international format (+233xxxxxxxxx)
- **Date Format**: Always use ISO format (YYYY-MM-DDTHH:mm:ssZ)

**Happy coding! 🚀**