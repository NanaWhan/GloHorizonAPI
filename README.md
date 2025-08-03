# GloHorizon Travel API

A comprehensive .NET 8 Web API for managing travel bookings, payments, and user management for Global Horizons Travel.

## Features

- **User Management**: Registration, authentication, and profile management
- **Quote Request System**: 5 service types with instant admin notifications (NEW - LIVE)
- **Travel Packages**: Browse and manage travel packages  
- **Booking System**: Complete booking workflow with status tracking
- **Payment Processing**: PayStack integration for secure payments
- **Admin Dashboard**: Administrative controls and monitoring
- **Real-time Notifications**: SMS and email notifications via Mnotify and SMTP
- **Image Management**: Supabase storage integration
- **Actor System**: Akka.NET for real-time quote notifications and background processing

## Tech Stack

- **.NET 8**: Web API framework
- **Entity Framework Core**: ORM with PostgreSQL
- **JWT Authentication**: Secure token-based authentication
- **PayStack**: Payment processing
- **Supabase**: Database and storage
- **Akka.NET**: Actor system for background processing
- **Swagger/OpenAPI**: API documentation

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) and [Docker Compose](https://docs.docker.com/compose/)
- PostgreSQL (if running locally without Docker)

## Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd GloHorizon
   ```

2. **Run with Docker Compose**
   ```bash
   docker-compose up --build
   ```

3. **Access the application**
   - API: http://localhost:5080
   - Swagger UI: http://localhost:5080
   - Database: localhost:5432

## Local Development Setup

1. **Install dependencies**
   ```bash
   dotnet restore
   ```

2. **Update connection string**
   - Edit `appsettings.Development.json` with your PostgreSQL connection string

3. **Run database migrations**
   ```bash
   dotnet ef database update --project GloHorizonApi
   ```

4. **Start the application**
   ```bash
   cd GloHorizonApi
   dotnet run
   ```

## Configuration

### Environment Variables

The application uses the following configuration sections:

- **ApiSettings**: JWT secret key
- **ConnectionStrings**: Database connection
- **PayStackKeys**: Payment processing keys
- **MnotifySettings**: SMS service configuration
- **EmailSettings**: SMTP email configuration
- **SupabaseStorage**: File storage settings

### Docker Environment

The Docker setup includes:
- **API Container**: Runs on port 8080 (mapped to 5080)
- **PostgreSQL Container**: Runs on port 5432
- **Persistent Data**: Database data persisted in Docker volumes
- **Health Checks**: Ensures database is ready before starting API

## API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/otp-login` - OTP-based login

### User Management
- `GET /api/user/profile` - Get user profile
- `PUT /api/user/profile` - Update user profile

### Travel Packages
- `GET /api/travelpackage` - Get all packages
- `GET /api/travelpackage/{id}` - Get package by ID
- `POST /api/travelpackage` - Create package (Admin)

### Quote Requests (NEW - LIVE)
- `POST /api/quote/hotel` - Submit hotel quote request
- `POST /api/quote/flight` - Submit flight quote request  
- `POST /api/quote/tour` - Submit tour quote request
- `POST /api/quote/visa` - Submit visa quote request
- `POST /api/quote/complete-package` - Submit complete package quote request
- `GET /api/quote/track/{referenceNumber}` - Track quote status (public)
- `GET /api/quote/my-quotes` - Get user's quotes (authenticated)

### Bookings
- `POST /api/booking/submit` - Submit booking request
- `GET /api/booking/track/{trackingNumber}` - Track booking status

### Payments
- `POST /api/payment/initiate` - Initiate payment
- `POST /api/payment/callback` - Payment callback

### Admin
- `POST /api/admin/login` - Admin login
- `GET /api/admin/bookings` - Get all bookings
- `PUT /api/admin/bookings/{id}/status` - Update booking status

## Quote API Integration Guide (FRONTEND TEAM)

### 🚀 **Live Endpoints Ready for Frontend Integration**

All quote endpoints are **LIVE** and ready for production use:

**Base URL**: `http://localhost:5080` (Development) | `https://your-domain.com` (Production)

### **1. Hotel Quote Request**
```javascript
POST /api/quote/hotel
Content-Type: application/json

{
  "hotelDetails": {
    "destination": "Dubai",
    "checkInDate": "2024-09-15T00:00:00Z",
    "checkOutDate": "2024-09-20T00:00:00Z", 
    "rooms": 1,
    "adultGuests": 2,
    "childGuests": 0,
    "roomType": "deluxe",
    "starRating": "4-star",
    "amenities": ["pool", "spa", "gym"]
  },
  "contactEmail": "customer@example.com",
  "contactPhone": "+233123456789",
  "contactName": "John Doe",
  "specialRequests": "Sea view room preferred",
  "urgency": 1  // 1=Standard, 2=Urgent, 3=Emergency
}
```

**Response**:
```javascript
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
    "createdAt": "2025-07-31T17:05:25Z",
    "contactEmail": "customer@example.com",
    "contactPhone": "+233123456789",
    "contactName": "John Doe",
    "specialRequests": "Sea view room preferred",
    "urgency": 1,
    "statusHistory": [...]
  }
}
```

### **2. Flight Quote Request** 
```javascript
POST /api/quote/flight
// Similar structure with flightDetails object
```

### **3. Quote Tracking (Public - No Auth Required)**
```javascript
GET /api/quote/track/{referenceNumber}

// Example: GET /api/quote/track/QHT17054939887
```

### **4. User's Quotes (Authenticated)**
```javascript
GET /api/quote/my-quotes
Authorization: Bearer <jwt-token>
```

### **⚡ Real-time Notifications**
- **Admin notifications**: Sent instantly via email/SMS on every quote request
- **Customer confirmations**: Sent with reference number for tracking
- **Status updates**: Automated notifications on quote status changes

### **📱 Frontend Integration Notes**
- **No authentication required** for quote submissions (guest-friendly)
- **Reference numbers** provided for easy tracking  
- **Comprehensive validation** with helpful error messages
- **Mobile-optimized** JSON structure
- **All 5 service types** supported: Hotel, Flight, Tour, Visa, Complete Package

### **🧪 Test Data**
Use these working reference numbers for testing:
- Hotel: `QHT17054939887`
- Flight: `QFL17054981853` 
- Tour: `QTR17055023233`
- Visa: `QVS17055066240`
- Package: `QCP17055107498`

## Database Schema

The application uses the following main entities:

- **User**: User accounts and profiles
- **Admin**: Administrative accounts  
- **QuoteRequest**: Quote requests with real-time notifications (NEW)
- **QuoteStatusHistory**: Quote status tracking and audit trail (NEW)
- **TravelPackage**: Travel package information
- **BookingRequest**: Customer booking requests
- **BookingStatusHistory**: Booking status tracking
- **OtpVerification**: OTP verification records

## Background Services

- **QuoteNotificationActor**: Handles real-time quote notifications to admins (NEW - LIVE)
- **PaymentVerificationService**: Monitors and verifies pending payments  
- **BookingNotificationActor**: Handles booking-related notifications
- **PaymentActor**: Processes payment workflows
- **OtpCleanupService**: Cleans up expired OTP records

## Security Features

- JWT-based authentication
- Password hashing with BCrypt
- CORS configuration
- Input validation and sanitization
- Secure API key management

## Monitoring & Logging

- Structured logging with built-in .NET logging
- Console logging for development
- Request/response logging
- Error handling and reporting

## Production Deployment

1. **Update configuration**
   - Set production connection strings
   - Configure production PayStack keys
   - Update email/SMS service credentials

2. **Build and deploy**
   ```bash
   docker-compose -f docker-compose.yml up -d --build
   ```

3. **Database migrations**
   - Migrations run automatically on startup
   - Admin seeding included

## Development

### Project Structure
```
GloHorizonApi/
├── Controllers/         # API controllers
├── Models/             # Data models and DTOs
├── Services/           # Business logic services
├── Data/              # Database context and migrations
├── Actors/            # Akka.NET actor system
├── Extensions/        # Service extensions
└── Properties/        # Launch settings
```

### Adding New Migrations
```bash
dotnet ef migrations add MigrationName --project GloHorizonApi
dotnet ef database update --project GloHorizonApi
```

### Testing
```bash
dotnet test
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests and ensure they pass
5. Submit a pull request

## Support

For support and questions, contact:
- Email: admin@globalhorizonstravel.com
- Phone: +233 24 905 8729

## License

This project is proprietary software owned by Global Horizons Travel.