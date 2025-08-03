# 🔐 Authentication Quick Reference

## Base URL
```
https://glohorizonapi.fly.dev
```

## 1. Register User
```typescript
// POST /api/auth/register
const register = await fetch('/api/auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    firstName: "John",
    lastName: "Doe",
    email: "john@example.com",
    phoneNumber: "+233245678901",
    password: "SecurePass123!",
    dateOfBirth: "1990-01-01T00:00:00Z",
    acceptMarketing: true
  })
});
```

## 2. Login User  
```typescript
// POST /api/auth/login
const login = await fetch('/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: "john@example.com",
    password: "SecurePass123!"
  })
});

const { token } = await login.json();
localStorage.setItem('authToken', token);
```

## 3. OTP Verification
```typescript
// Step 1: POST /api/auth/request-otp
await fetch('/api/auth/request-otp', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ phoneNumber: "+233245678901" })
});

// Step 2: POST /api/auth/verify-otp
const verify = await fetch('/api/auth/verify-otp', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    phoneNumber: "+233245678901",
    otpCode: "123456"
  })
});
```

## 4. Logout
```typescript
// POST /api/auth/logout
await fetch('/api/auth/logout', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${localStorage.getItem('authToken')}`
  }
});
localStorage.removeItem('authToken');
```

## Response Format
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
```