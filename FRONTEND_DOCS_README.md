# 📚 Frontend Documentation Package

## 🎯 For Frontend Developers

Your complete integration guide for the GloHorizon Travel API.

### 📋 Documentation Files

| File | Purpose | Best For |
|------|---------|----------|
| **[FRONTEND_INTEGRATION_GUIDE.md](./FRONTEND_INTEGRATION_GUIDE.md)** | 📖 Complete documentation | Full reference & onboarding |
| **[AUTH_QUICK_REFERENCE.md](./AUTH_QUICK_REFERENCE.md)** | 🔐 Authentication only | User login/registration |
| **[QUOTES_QUICK_REFERENCE.md](./QUOTES_QUICK_REFERENCE.md)** | 💬 Quotes only | Flight, hotel, visa, tour quotes |
| **[TYPESCRIPT_INTERFACES.md](./TYPESCRIPT_INTERFACES.md)** | 🔤 TypeScript types | Copy-paste interfaces |
| **[FRONTEND_TESTING_EXAMPLES.md](./FRONTEND_TESTING_EXAMPLES.md)** | 🧪 Working examples | Testing & debugging |

---

## 🚀 Quick Start (2 minutes)

### 1. Test API Connection
```javascript
// Paste in browser console
fetch('https://glohorizonapi.fly.dev/api/quote/test-minimal', {method: 'POST'})
  .then(r => r.json())
  .then(d => console.log('API Status:', d.success ? '✅ Working' : '❌ Error'));
```

### 2. Submit Your First Quote
```javascript
// Flight quote example
fetch('https://glohorizonapi.fly.dev/api/quote/flight', {
  method: 'POST',
  headers: {'Content-Type': 'application/json'},
  body: JSON.stringify({
    contactEmail: "test@example.com",
    contactPhone: "+233245678901",
    contactName: "Test User",
    urgency: 0,
    flightDetails: {
      departureCity: "Accra",
      arrivalCity: "London",
      departureDate: "2025-10-15T08:00:00Z",
      returnDate: "2025-10-25T18:00:00Z",
      passengerCount: 1,
      travelClass: "Economy",
      tripType: "RoundTrip"
    }
  })
}).then(r => r.json()).then(d => console.log('Quote:', d.referenceNumber));
```

### 3. Copy TypeScript Interfaces
Open `TYPESCRIPT_INTERFACES.md` and copy the interfaces you need.

---

## 🎯 What Each Service Does

| Service | Endpoint | Purpose | Auth Required |
|---------|----------|---------|---------------|
| **Authentication** | `/api/auth/*` | User login, registration, OTP | ❌ No |
| **Flight Quotes** | `/api/quote/flight` | Request flight pricing | ❌ No |
| **Hotel Quotes** | `/api/quote/hotel` | Request hotel pricing | ❌ No |
| **Visa Quotes** | `/api/quote/visa` | Request visa assistance | ❌ No |
| **Tour Quotes** | `/api/quote/tour` | Request tour packages | ❌ No |
| **Complete Packages** | `/api/quote/complete-package` | Full travel packages | ❌ No |
| **Quote Tracking** | `/api/quote/track/{ref}` | Check quote status | ❌ No |

---

## 🔗 Production API

**Base URL**: `https://glohorizonapi.fly.dev`

**Status**: ✅ Live and fully operational

---

## 📞 Common Questions

### Q: Do I need authentication for quotes?
**A**: No! All quote endpoints are public. Only user management requires auth.

### Q: What format should I use for phone numbers?
**A**: International format: `+233245678901` (Ghana country code)

### Q: What format should I use for dates?
**A**: ISO format: `2025-10-15T08:00:00Z`

### Q: How do I track a quote?
**A**: Save the `referenceNumber` from the response and use `/api/quote/track/{referenceNumber}`

### Q: What are the urgency levels?
**A**: `0` = Standard, `1` = Urgent, `2` = Emergency

### Q: How do I handle errors?
**A**: Check the `success` field in the response. If `false`, check the `message` field.

---

## 🛠️ Development Tips

1. **Start with quotes** - They're public and don't require auth
2. **Save reference numbers** - You'll need them to track quotes
3. **Use TypeScript** - Copy the interfaces for better development experience
4. **Test in console first** - Use the examples to verify API calls work
5. **Handle errors gracefully** - Always check the `success` field

---

## 📱 Mobile Considerations

- All endpoints work with mobile apps
- Phone number validation expects international format
- Consider network timeouts for slow connections
- Cache reference numbers locally

---

## 🎉 You're Ready!

Pick the documentation file that matches your needs and start building! The API is fully tested and ready for production use.

**Happy coding! 🚀**