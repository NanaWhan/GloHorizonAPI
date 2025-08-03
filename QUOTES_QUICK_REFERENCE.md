# 💬 Quotes Quick Reference

> **No Authentication Required** - All quote endpoints are public!

## Base URL
```
https://glohorizonapi.fly.dev
```

## ✈️ Flight Quote
```typescript
// POST /api/quote/flight
await fetch('/api/quote/flight', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    contactEmail: "customer@example.com",
    contactPhone: "+233245678901",
    contactName: "John Doe",
    specialRequests: "Window seat preferred",
    urgency: 0,  // 0=Standard, 1=Urgent, 2=Emergency
    flightDetails: {
      departureCity: "Accra",
      arrivalCity: "London",
      departureDate: "2025-10-15T08:00:00Z",
      returnDate: "2025-10-25T18:00:00Z",
      passengerCount: 2,
      travelClass: "Business",  // Economy, Premium Economy, Business, First
      tripType: "RoundTrip"     // OneWay, RoundTrip
    }
  })
});
```

## 🏨 Hotel Quote
```typescript
// POST /api/quote/hotel
await fetch('/api/quote/hotel', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    contactEmail: "customer@example.com",
    contactPhone: "+233245678901",
    contactName: "John Doe",
    specialRequests: "Ocean view room",
    urgency: 0,
    hotelDetails: {
      destination: "Dubai",
      checkInDate: "2025-10-15T14:00:00Z",
      checkOutDate: "2025-10-20T12:00:00Z",
      roomCount: 1,
      guestCount: 2,
      roomType: "Suite"  // Standard, Deluxe, Suite, Presidential
    }
  })
});
```

## 📄 Visa Quote
```typescript
// POST /api/quote/visa  
await fetch('/api/quote/visa', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    contactEmail: "customer@example.com",
    contactPhone: "+233245678901",
    contactName: "John Doe",
    specialRequests: "Express processing needed",
    urgency: 1,
    visaDetails: {
      destinationCountry: "Canada",
      visaType: "Business",  // Tourist, Business, Student, Work, Transit
      intendedTravelDate: "2025-11-01T08:00:00Z",
      processingTime: "Express",  // Standard, Express, Emergency
      passportNumber: "A1234567",
      nationality: "Ghanaian",
      durationOfStay: 14,  // days (1-365)
      purposeOfVisit: "Business meetings"
    }
  })
});
```

## 🗺️ Tour Quote
```typescript
// POST /api/quote/tour
await fetch('/api/quote/tour', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    contactEmail: "customer@example.com", 
    contactPhone: "+233245678901",
    contactName: "John Doe",
    specialRequests: "Photography focused tour",
    urgency: 0,
    tourDetails: {
      destination: "Egypt",
      startDate: "2025-12-01T08:00:00Z",
      endDate: "2025-12-10T18:00:00Z",
      groupSize: 4,
      tourType: "Cultural",  // Cultural, Adventure, Safari, Historical, Beach, City
      accommodationType: "Hotel",  // Hotel, Resort, Guesthouse, Camping
      transportationType: "Bus",   // Bus, Van, Car, Flight
      tourPackage: "Premium Egypt Cultural Tour"
    }
  })
});
```

## 📦 Complete Package Quote
```typescript
// POST /api/quote/complete-package
await fetch('/api/quote/complete-package', {
  method: 'POST', 
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    contactEmail: "customer@example.com",
    contactPhone: "+233245678901", 
    contactName: "John & Jane Doe",
    specialRequests: "Honeymoon package",
    urgency: 0,
    packageDetails: {
      packageType: "Honeymoon",  // Honeymoon, Family, Business, Group, Solo
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
      }
    }
  })
});
```

## 📍 Track Quote
```typescript
// GET /api/quote/track/{referenceNumber}
const track = await fetch('/api/quote/track/QFL03062303060');
const status = await track.json();
```

## Response Format
```typescript
interface QuoteResponse {
  success: boolean;
  message: string;
  referenceNumber: string;  // SAVE THIS! Use for tracking
  quote: {
    id: number;
    referenceNumber: string;
    serviceType: number;  // 1=Flight, 2=Hotel, 3=Tour, 4=Visa, 5=Package
    status: number;       // 1=Submitted, 2=UnderReview, 3=QuoteProvided
    destination: string;
    quotedAmount: number | null;
    currency: string;     // "GHS" 
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

## 🎯 Key Points
- **Save Reference Number**: Always store `referenceNumber` from response
- **Phone Format**: Use international format (+233xxxxxxxxx)
- **Date Format**: ISO format (YYYY-MM-DDTHH:mm:ssZ)
- **Urgency Levels**: 0=Standard, 1=Urgent, 2=Emergency
- **No Auth Needed**: All quote endpoints are public