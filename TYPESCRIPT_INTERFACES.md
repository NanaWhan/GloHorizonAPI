# 🔤 TypeScript Interfaces

Copy these interfaces directly into your TypeScript project:

```typescript
// =================== AUTH INTERFACES ===================

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

interface OTPRequest {
  phoneNumber: string;
}

interface OTPVerifyRequest {
  phoneNumber: string;
  otpCode: string;
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

// =================== QUOTE INTERFACES ===================

type UrgencyLevel = 0 | 1 | 2;  // Standard, Urgent, Emergency
type ServiceType = 1 | 2 | 3 | 4 | 5;  // Flight, Hotel, Tour, Visa, Package

interface BaseQuoteRequest {
  contactEmail: string;
  contactPhone: string;
  contactName: string;
  specialRequests?: string;
  urgency: UrgencyLevel;
}

// Flight Types
type TravelClass = "Economy" | "Premium Economy" | "Business" | "First";
type TripType = "OneWay" | "RoundTrip";

interface FlightDetails {
  departureCity: string;
  arrivalCity: string;
  departureDate: string;
  returnDate?: string;
  passengerCount: number;
  travelClass: TravelClass;
  tripType: TripType;
}

interface FlightQuoteRequest extends BaseQuoteRequest {
  flightDetails: FlightDetails;
}

// Hotel Types
type RoomType = "Standard" | "Deluxe" | "Suite" | "Presidential";

interface HotelDetails {
  destination: string;
  checkInDate: string;
  checkOutDate: string;
  roomCount: number;
  guestCount: number;
  roomType: RoomType;
}

interface HotelQuoteRequest extends BaseQuoteRequest {
  hotelDetails: HotelDetails;
}

// Visa Types
type VisaType = "Tourist" | "Business" | "Student" | "Work" | "Transit";
type ProcessingTime = "Standard" | "Express" | "Emergency";

interface VisaDetails {
  destinationCountry: string;
  visaType: VisaType;
  intendedTravelDate: string;
  processingTime: ProcessingTime;
  passportNumber: string;
  nationality: string;
  durationOfStay: number;
  purposeOfVisit: string;
}

interface VisaQuoteRequest extends BaseQuoteRequest {
  visaDetails: VisaDetails;
}

// Tour Types
type TourType = "Cultural" | "Adventure" | "Safari" | "Historical" | "Beach" | "City";
type AccommodationType = "Hotel" | "Resort" | "Guesthouse" | "Camping";
type TransportationType = "Bus" | "Van" | "Car" | "Flight";

interface TourDetails {
  destination: string;
  startDate: string;
  endDate: string;
  groupSize: number;
  tourType: TourType;
  accommodationType: AccommodationType;
  transportationType: TransportationType;
  tourPackage: string;
}

interface TourQuoteRequest extends BaseQuoteRequest {
  tourDetails: TourDetails;
}

// Package Types
type PackageType = "Honeymoon" | "Family" | "Business" | "Group" | "Solo";

interface PackageDetails {
  packageType: PackageType;
  flightDetails: FlightDetails;
  hotelDetails: HotelDetails;
  visaDetails?: VisaDetails;
}

interface CompletePackageRequest extends BaseQuoteRequest {
  packageDetails: PackageDetails;
}

// =================== RESPONSE INTERFACES ===================

interface StatusHistory {
  fromStatus: number;
  toStatus: number;
  notes: string;
  changedAt: string;
  changedBy: string;
}

interface Quote {
  id: number;
  referenceNumber: string;
  serviceType: ServiceType;
  status: number;
  destination: string;
  quotedAmount: number | null;
  currency: string;
  createdAt: string;
  travelDate: string;
  quoteProvidedAt: string | null;
  quoteExpiresAt: string | null;
  contactEmail: string;
  contactPhone: string;
  contactName: string;
  specialRequests: string | null;
  adminNotes: string | null;
  urgency: UrgencyLevel;
  paymentLinkUrl: string | null;
  statusHistory: StatusHistory[];
}

interface QuoteResponse {
  success: boolean;
  message: string;
  referenceNumber: string;
  quote: Quote;
}

interface ErrorResponse {
  success: false;
  message: string;
  errors?: {
    [field: string]: string[];
  };
}

// =================== API RESPONSE UNION TYPES ===================

type AuthResult = AuthResponse | ErrorResponse;
type QuoteResult = QuoteResponse | ErrorResponse;

// =================== UTILITY TYPES ===================

interface ApiConfig {
  baseUrl: string;
  timeout?: number;
  headers?: Record<string, string>;
}

interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// =================== ENUMS (Alternative to Union Types) ===================

enum UrgencyLevelEnum {
  Standard = 0,
  Urgent = 1,
  Emergency = 2
}

enum ServiceTypeEnum {
  Flight = 1,
  Hotel = 2,
  Tour = 3,
  Visa = 4,
  CompletePackage = 5
}

enum QuoteStatusEnum {
  Submitted = 1,
  UnderReview = 2,
  QuoteProvided = 3,
  PaymentPending = 4,
  Processing = 5,
  Confirmed = 6,
  Completed = 7,
  Cancelled = 8
}

// =================== EXAMPLE USAGE ===================

// Type-safe API client example
class GloHorizonAPI {
  constructor(private config: ApiConfig) {}

  async login(request: LoginRequest): Promise<AuthResult> {
    const response = await fetch(`${this.config.baseUrl}/api/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...this.config.headers
      },
      body: JSON.stringify(request)
    });

    return response.json();
  }

  async requestFlightQuote(request: FlightQuoteRequest): Promise<QuoteResult> {
    const response = await fetch(`${this.config.baseUrl}/api/quote/flight`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...this.config.headers
      },
      body: JSON.stringify(request)
    });

    return response.json();
  }

  async trackQuote(referenceNumber: string): Promise<Quote | ErrorResponse> {
    const response = await fetch(`${this.config.baseUrl}/api/quote/track/${referenceNumber}`);
    return response.json();
  }
}

// Usage example
const api = new GloHorizonAPI({
  baseUrl: 'https://glohorizonapi.fly.dev'
});

const loginResult = await api.login({
  email: 'user@example.com',
  password: 'password123'
});

if (loginResult.success) {
  console.log('Logged in:', loginResult.user?.firstName);
} else {
  console.error('Login failed:', loginResult.message);
}
```

## 🎯 Quick Copy Sections

### Basic Types Only:
```typescript
type UrgencyLevel = 0 | 1 | 2;
type TravelClass = "Economy" | "Premium Economy" | "Business" | "First";
type TripType = "OneWay" | "RoundTrip";
type RoomType = "Standard" | "Deluxe" | "Suite" | "Presidential";
type VisaType = "Tourist" | "Business" | "Student" | "Work" | "Transit";
```

### Request Interfaces Only:
```typescript
interface LoginRequest {
  email: string;
  password: string;
}

interface FlightQuoteRequest {
  contactEmail: string;
  contactPhone: string;
  contactName: string;
  specialRequests?: string;
  urgency: 0 | 1 | 2;
  flightDetails: {
    departureCity: string;
    arrivalCity: string;
    departureDate: string;
    returnDate?: string;
    passengerCount: number;
    travelClass: "Economy" | "Premium Economy" | "Business" | "First";
    tripType: "OneWay" | "RoundTrip";
  };
}
```

### Response Interfaces Only:
```typescript
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

interface QuoteResponse {
  success: boolean;
  message: string;
  referenceNumber: string;
  quote: {
    id: number;
    referenceNumber: string;
    serviceType: number;
    status: number;
    destination: string;
    quotedAmount: number | null;
    currency: string;
    createdAt: string;
    travelDate: string;
    contactEmail: string;
    contactPhone: string;
    contactName: string;
    specialRequests: string | null;
    urgency: number;
  };
}
```