# 📋 Booking Fetch API Documentation

Enhanced booking retrieval endpoints for both users and admins with pagination, filtering, and search capabilities.

## 🚀 Live API Base URL
```
https://glohorizonapi.fly.dev/api
```

## 👤 User Endpoints

### 1. Get User's Own Bookings
Retrieve current user's bookings with pagination and filtering.

**Endpoint:** `GET /api/booking/my-bookings`
**Access:** Requires user authentication
**Method:** GET

#### Query Parameters:
- `pageNumber` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 10)
- `status` (optional): Filter by BookingStatus (Submitted, InProgress, QuoteProvided, etc.)
- `serviceType` (optional): Filter by service type (Flight, Hotel, Tour, Visa, CompletePackage)
- `urgency` (optional): Filter by urgency level (Standard, High, Urgent)
- `fromDate` (optional): Filter bookings from this date (ISO format)
- `toDate` (optional): Filter bookings to this date (ISO format)
- `searchTerm` (optional): Search in reference number, destination, special requests

#### Example Request:
```bash
curl -X GET "https://glohorizonapi.fly.dev/api/booking/my-bookings?pageNumber=1&pageSize=5&status=Submitted&serviceType=Flight" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Response:
```json
{
  "success": true,
  "message": "Bookings retrieved successfully",
  "bookings": [
    {
      "id": 123,
      "referenceNumber": "GH-FL-20250102-001",
      "serviceType": 1,
      "status": 1,
      "destination": "London",
      "quotedAmount": 2500.00,
      "finalAmount": null,
      "currency": "GHS",
      "createdAt": "2025-01-02T10:30:00Z",
      "travelDate": "2025-02-15T00:00:00Z",
      "contactEmail": "user@example.com",
      "contactPhone": "+233541458512",
      "specialRequests": "Window seat preferred",
      "adminNotes": null,
      "urgency": 1,
      "statusHistory": [
        {
          "fromStatus": 1,
          "toStatus": 1,
          "notes": "Booking submitted",
          "changedAt": "2025-01-02T10:30:00Z",
          "changedBy": "System"
        }
      ],
      "documents": []
    }
  ],
  "totalCount": 15,
  "pageNumber": 1,
  "pageSize": 5,
  "totalPages": 3
}
```

## 👨‍💼 Admin Endpoints

### 1. Get All Bookings (Admin)
Retrieve all system bookings with advanced filtering and search.

**Endpoint:** `GET /api/admin/bookings`
**Access:** Requires admin authentication
**Method:** GET

#### Query Parameters:
Same as user endpoint, plus:
- Enhanced search includes: user names, contact email, phone numbers

#### Example Request:
```bash
curl -X GET "https://glohorizonapi.fly.dev/api/admin/bookings?pageNumber=1&pageSize=20&status=InProgress&searchTerm=john" \
  -H "Authorization: Bearer ADMIN_JWT_TOKEN"
```

#### Response:
```json
{
  "success": true,
  "message": "Bookings retrieved successfully",
  "bookings": [
    {
      "id": 123,
      "referenceNumber": "GH-FL-20250102-001",
      "serviceType": 1,
      "status": 2,
      "destination": "London",
      "quotedAmount": 2500.00,
      "finalAmount": 2400.00,
      "currency": "GHS",
      "createdAt": "2025-01-02T10:30:00Z",
      "travelDate": "2025-02-15T00:00:00Z",
      "contactEmail": "john@example.com",
      "contactPhone": "+233541458512",
      "specialRequests": "Window seat preferred",
      "adminNotes": "Customer confirmed pricing",
      "urgency": 1,
      "statusHistory": [
        {
          "fromStatus": 1,
          "toStatus": 2,
          "notes": "Quote provided to customer",
          "changedAt": "2025-01-02T14:30:00Z",
          "changedBy": "Admin John"
        }
      ],
      "documents": [
        {
          "id": 1,
          "documentType": "Passport",
          "fileName": "passport.pdf",
          "fileUrl": "https://example.com/docs/passport.pdf",
          "uploadedAt": "2025-01-02T12:00:00Z"
        }
      ]
    }
  ],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

### 2. Get Single Booking Details (Admin)
Get detailed information about a specific booking.

**Endpoint:** `GET /api/admin/bookings/{id}`
**Access:** Requires admin authentication

#### Example Request:
```bash
curl -X GET "https://glohorizonapi.fly.dev/api/admin/bookings/123" \
  -H "Authorization: Bearer ADMIN_JWT_TOKEN"
```

### 3. Update Booking Status (Admin)
Update the status of a booking.

**Endpoint:** `PUT /api/admin/bookings/{id}/status`
**Access:** Requires admin authentication

#### Request Body:
```json
{
  "newStatus": 3,
  "notes": "Quote accepted by customer",
  "adminNotes": "Customer ready to proceed",
  "estimatedPrice": 2500.00,
  "finalPrice": 2400.00
}
```

### 4. Admin Dashboard
Get overview statistics for admin dashboard.

**Endpoint:** `GET /api/admin/dashboard`
**Access:** Requires admin authentication

#### Response:
```json
{
  "totalBookings": 150,
  "pendingBookings": 25,
  "completedBookings": 100,
  "totalUsers": 500,
  "recentBookings": [...]
}
```

## 📊 Enums and Status Values

### BookingStatus:
- `0` - Draft
- `1` - Submitted
- `2` - InProgress
- `3` - QuoteProvided
- `4` - QuoteAccepted
- `5` - PaymentPending
- `6` - PaymentReceived
- `7` - Confirmed
- `8` - Completed
- `9` - Cancelled

### ServiceType:
- `1` - Flight
- `2` - Hotel
- `3` - Tour
- `4` - Visa
- `5` - CompletePackage

### UrgencyLevel:
- `1` - Standard
- `2` - High
- `3` - Urgent

## 🔍 Advanced Search Examples

### User Examples:
```bash
# Get user's hotel bookings from last month
GET /api/booking/my-bookings?serviceType=2&fromDate=2024-12-01&toDate=2024-12-31

# Search user's bookings by destination
GET /api/booking/my-bookings?searchTerm=london

# Get pending bookings only
GET /api/booking/my-bookings?status=1&pageSize=20
```

### Admin Examples:
```bash
# Get all urgent bookings
GET /api/admin/bookings?urgency=3&pageSize=50

# Search by customer name or email
GET /api/admin/bookings?searchTerm=john.doe

# Get completed bookings this year
GET /api/admin/bookings?status=8&fromDate=2025-01-01

# Get all flight bookings in progress
GET /api/admin/bookings?serviceType=1&status=2
```

## 🚀 Integration Examples

### Frontend JavaScript:
```javascript
// Get user bookings with pagination
const getUserBookings = async (page = 1, filters = {}) => {
  const params = new URLSearchParams({
    pageNumber: page,
    pageSize: 10,
    ...filters
  });

  const response = await fetch(`/api/booking/my-bookings?${params}`, {
    headers: {
      'Authorization': `Bearer ${userToken}`,
      'Content-Type': 'application/json'
    }
  });

  return response.json();
};

// Search bookings
const searchBookings = async (searchTerm) => {
  return getUserBookings(1, { searchTerm });
};
```

### Admin Dashboard Integration:
```javascript
// Get admin bookings with filters
const getAdminBookings = async (filters = {}) => {
  const params = new URLSearchParams({
    pageNumber: 1,
    pageSize: 25,
    ...filters
  });

  const response = await fetch(`/api/admin/bookings?${params}`, {
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json'
    }
  });

  return response.json();
};

// Get pending bookings for admin attention
const getPendingBookings = () => getAdminBookings({ status: 1 });

// Get urgent bookings
const getUrgentBookings = () => getAdminBookings({ urgency: 3 });
```

## ✅ Features Available

### User Features:
- ✅ View own bookings with pagination
- ✅ Filter by status, service type, urgency
- ✅ Date range filtering
- ✅ Search in bookings
- ✅ Complete booking history and documents

### Admin Features:
- ✅ View all system bookings
- ✅ Advanced filtering and search
- ✅ Update booking status and pricing
- ✅ Add admin notes
- ✅ Generate payment links
- ✅ Dashboard with statistics
- ✅ Detailed booking information
- ✅ User information included

## 🔧 Response Format
All endpoints return consistent pagination metadata:
- `totalCount`: Total items available
- `pageNumber`: Current page number
- `pageSize`: Items per page
- `totalPages`: Total pages available

Ready for production use! 🚀