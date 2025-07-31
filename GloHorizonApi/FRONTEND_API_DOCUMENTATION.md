# 🚀 Frontend API Documentation - Authentication System

## 📋 Base Configuration

```typescript
const API_BASE_URL = "https://glohorizonapi.fly.dev/index.html" 
// const API_BASE_URL = "https://your-api-domain.com/api" // Production

// Headers for authenticated requests
const authHeaders = {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
}
```

---

## 🔐 Authentication Endpoints

### 1. **User Registration**

```http
POST /api/auth/register
Content-Type: application/json
```

**Request Body:**

```typescript
interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;  // Format: "+233XXXXXXXXX"
  password: string;     // Minimum 8 characters
  dateOfBirth?: string; // Format: "YYYY-MM-DD" or null
  acceptMarketing: boolean;
}
```

**Success Response (201):**

```typescript
interface AuthResponse {
  success: boolean;
  message: string;
  token: string;
  user: UserInfo;
}

interface UserInfo {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  role: string;        // "User" | "Admin"
  createdAt: string;   // ISO date string
}
```

**Error Responses:**

- `400 Bad Request`: Validation errors or duplicate email/phone
- `500 Internal Server Error`: Server error

---

### 2. **Email/Password Login**

```http
POST /api/auth/login
Content-Type: application/json
```

**Request Body:**

```typescript
interface LoginRequest {
  email: string;
  password: string;
}
```

**Success Response (200):** Same as registration (`AuthResponse`)

**Error Responses:**

- `401 Unauthorized`: Invalid email or password
- `500 Internal Server Error`: Server error

---

### 3. **OTP Request (Existing Users Only)**

```http
POST /api/auth/request-otp
Content-Type: application/json
```

**Request Body:**

```typescript
interface OtpRequest {
  phoneNumber: string; // Must be registered user's phone
}
```

**Success Response (200):**

```typescript
interface OtpResponse {
  success: boolean;
  message: string;
  otpId?: string; // Session tracking (optional)
}
```

**Error Responses:**

- `404 Not Found`: Phone number not registered
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: SMS service error

---

### 4. **OTP Verification (Login)**

```http
POST /api/auth/verify-otp
Content-Type: application/json
```

**Request Body:**

```typescript
interface VerifyOtpRequest {
  phoneNumber: string;
  otpCode: string; // 6-digit code
}
```

**Success Response (200):** Same as login (`AuthResponse`)

**Error Responses:**

- `404 Not Found`: Phone number not found
- `401 Unauthorized`: Invalid OTP code
- `422 Unprocessable Entity`: OTP expired
- `500 Internal Server Error`: Server error

---

### 5. **Logout**

```http
POST /api/auth/logout
Authorization: Bearer {token}
```

**Success Response (200):**

```typescript
interface LogoutResponse {
  success: boolean;
  message: string;
}
```

---

## 👤 User Management Endpoints

### 6. **Get User Profile**

```http
GET /api/user/profile
Authorization: Bearer {token}
```

**Success Response (200):**

```typescript
interface UserProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth?: string; // "YYYY-MM-DD" or null
  acceptMarketing: boolean;
  role: string;
  createdAt: string;
  updatedAt?: string;
}
```

**Error Responses:**

- `401 Unauthorized`: Invalid or expired token
- `404 Not Found`: User not found

---

### 7. **Get Booking History**

```http
GET /api/user/booking-history
Authorization: Bearer {token}
```

**Success Response (200):**

```typescript
interface BookingHistoryResponse {
  bookings: BookingHistory[];
  stats: BookingStats;
}

interface BookingHistory {
  id: number;
  referenceNumber: string;
  serviceType: string;
  status: string;
  totalAmount: number;
  createdAt: string;
}

interface BookingStats {
  totalBookings: number;
  pendingBookings: number;
  confirmedBookings: number;
  totalSpent: number;
}
```

---

## 🚨 Error Handling

### Standard Error Response Format

```typescript
interface ErrorResponse {
  success: boolean;
  message: string;
  // Sometimes includes validation details
}
```

### Common HTTP Status Codes

- `200 OK`: Success
- `201 Created`: Resource created (registration)
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Authentication failed
- `404 Not Found`: Resource not found
- `422 Unprocessable Entity`: Validation failed
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: Server error

---

## 🔄 Authentication Flow Examples

### Registration → Login Flow

```typescript
// 1. Register new user
const registerData = {
  firstName: "John",
  lastName: "Doe",
  email: "john@example.com",
  phoneNumber: "+233123456789",
  password: "password123",
  dateOfBirth: "1990-01-01",
  acceptMarketing: true
};

const response = await fetch(`${API_BASE_URL}/auth/register`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(registerData)
});

const { token, user } = await response.json();
// Store token for future requests
localStorage.setItem('authToken', token);
```

### OTP Login Flow

```typescript
// 1. Request OTP (only for existing users)
const otpRequest = await fetch(`${API_BASE_URL}/auth/request-otp`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ phoneNumber: "+233123456789" })
});

if (otpRequest.status === 404) {
  // User not registered - redirect to registration
  console.log("Phone number not registered");
  return;
}

// 2. Verify OTP
const verifyResponse = await fetch(`${API_BASE_URL}/auth/verify-otp`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    phoneNumber: "+233123456789",
    otpCode: "123456"
  })
});

const { token, user } = await verifyResponse.json();
localStorage.setItem('authToken', token);
```

---

## 🛡️ Security Notes

### Token Management

- Store JWT token securely (localStorage or httpOnly cookie)
- Include token in Authorization header: `Bearer {token}`
- Token expires in 24 hours (recommended)
- Clear token on logout

### Phone Number Format

- Always use international format: `+233XXXXXXXXX`
- Validate format on frontend before sending

### Password Requirements

- Minimum 8 characters
- Frontend should enforce complexity rules

### Rate Limiting

- OTP requests: Max 3 per phone per hour
- Login attempts: Max 5 per IP per 15 minutes
- Handle 429 responses appropriately

---

## 📱 Mobile/React Native Considerations

```typescript
// Store token securely
import AsyncStorage from '@react-native-async-storage/async-storage';

// Save token
await AsyncStorage.setItem('authToken', token);

// Retrieve token
const token = await AsyncStorage.getItem('authToken');

// Add to headers
const headers = {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
};
```

---

## 🧪 Testing & Development

Use the provided `test-auth-endpoints.http` file for API testing.

### Quick Test Checklist

- ✅ Registration with all required fields
- ✅ Login with email/password
- ✅ OTP request for existing user (should work)
- ✅ OTP request for non-existent user (should return 404)
- ✅ OTP verification for existing user
- ✅ Get user profile with token
- ✅ Get booking history with token
- ✅ Logout with token

### Environment Variables

```typescript
const config = {
  apiBaseUrl: process.env.REACT_APP_API_BASE_URL || 'http://localhost:5080/api',
  environment: process.env.NODE_ENV || 'development'
};
```

**🚀 Happy Coding! Your backend is ready for seamless frontend integration.**
