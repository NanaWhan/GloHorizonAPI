# 🧪 Frontend Testing Examples

## Quick API Connectivity Test

```javascript
// Test if API is working
const testAPI = async () => {
  try {
    const response = await fetch('https://glohorizonapi.fly.dev/api/quote/test-minimal', {
      method: 'POST'
    });
    const data = await response.json();
    console.log('✅ API Status:', data.success ? 'Working' : 'Error');
    return data;
  } catch (error) {
    console.error('❌ API Test Failed:', error);
  }
};

testAPI();
```

## Complete Working Examples

### 1. User Registration & Login Flow
```javascript
const authFlow = async () => {
  // 1. Register new user
  console.log('🔄 Registering user...');
  const registerResponse = await fetch('https://glohorizonapi.fly.dev/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      firstName: "Test",
      lastName: "User",
      email: `test${Date.now()}@example.com`, // Unique email
      phoneNumber: "+233245678901",
      password: "TestPass123!",
      dateOfBirth: "1990-01-01T00:00:00Z",
      acceptMarketing: true
    })
  });
  
  const registerData = await registerResponse.json();
  console.log('📝 Registration:', registerData.success ? '✅ Success' : '❌ Failed');
  
  if (!registerData.success) {
    console.log('📧 Try login instead...');
    
    // 2. Login existing user
    const loginResponse = await fetch('https://glohorizonapi.fly.dev/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: "test@example.com",
        password: "TestPassword123!"
      })
    });
    
    const loginData = await loginResponse.json();
    console.log('🔐 Login:', loginData.success ? '✅ Success' : '❌ Failed');
    
    if (loginData.success) {
      localStorage.setItem('authToken', loginData.token);
      console.log('💾 Token saved:', loginData.token.substring(0, 20) + '...');
    }
  }
};

authFlow();
```

### 2. Test All Quote Types
```javascript
const testAllQuotes = async () => {
  console.log('🧪 Testing all quote types...');
  
  // Test Flight Quote
  console.log('✈️ Testing flight quote...');
  const flightResponse = await fetch('https://glohorizonapi.fly.dev/api/quote/flight', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      contactEmail: "test@example.com",
      contactPhone: "+233245678901",
      contactName: "Test User",
      specialRequests: "Window seat",
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
  });
  
  const flightData = await flightResponse.json();
  console.log('✈️ Flight Quote:', flightData.success ? `✅ ${flightData.referenceNumber}` : '❌ Failed');
  
  // Test Hotel Quote
  console.log('🏨 Testing hotel quote...');
  const hotelResponse = await fetch('https://glohorizonapi.fly.dev/api/quote/hotel', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      contactEmail: "test@example.com",
      contactPhone: "+233245678901",
      contactName: "Test User",
      specialRequests: "Ocean view",
      urgency: 0,
      hotelDetails: {
        destination: "Dubai",
        checkInDate: "2025-10-15T14:00:00Z",
        checkOutDate: "2025-10-20T12:00:00Z",
        roomCount: 1,
        guestCount: 2,
        roomType: "Deluxe"
      }
    })
  });
  
  const hotelData = await hotelResponse.json();
  console.log('🏨 Hotel Quote:', hotelData.success ? `✅ ${hotelData.referenceNumber}` : '❌ Failed');
  
  // Test Visa Quote
  console.log('📄 Testing visa quote...');
  const visaResponse = await fetch('https://glohorizonapi.fly.dev/api/quote/visa', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      contactEmail: "test@example.com",
      contactPhone: "+233245678901",
      contactName: "Test User",
      specialRequests: "Express processing",
      urgency: 1,
      visaDetails: {
        destinationCountry: "Canada",
        visaType: "Tourist",
        intendedTravelDate: "2025-11-01T08:00:00Z",
        processingTime: "Standard",
        passportNumber: "A1234567",
        nationality: "Ghanaian",
        durationOfStay: 14,
        purposeOfVisit: "Tourism"
      }
    })
  });
  
  const visaData = await visaResponse.json();
  console.log('📄 Visa Quote:', visaData.success ? `✅ ${visaData.referenceNumber}` : '❌ Failed');
  
  // Test Tour Quote
  console.log('🗺️ Testing tour quote...');
  const tourResponse = await fetch('https://glohorizonapi.fly.dev/api/quote/tour', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      contactEmail: "test@example.com",
      contactPhone: "+233245678901",
      contactName: "Test User",
      specialRequests: "Photography tour",
      urgency: 0,
      tourDetails: {
        destination: "Egypt",
        startDate: "2025-12-01T08:00:00Z",
        endDate: "2025-12-10T18:00:00Z",
        groupSize: 2,
        tourType: "Cultural",
        accommodationType: "Hotel",
        transportationType: "Bus",
        tourPackage: "Premium Egypt Cultural Tour"
      }
    })
  });
  
  const tourData = await tourResponse.json();
  console.log('🗺️ Tour Quote:', tourData.success ? `✅ ${tourData.referenceNumber}` : '❌ Failed');
  
  console.log('🎉 All tests completed!');
  
  // Return reference numbers for tracking
  return {
    flight: flightData.referenceNumber,
    hotel: hotelData.referenceNumber, 
    visa: visaData.referenceNumber,
    tour: tourData.referenceNumber
  };
};

testAllQuotes().then(refs => {
  console.log('📋 Reference Numbers:', refs);
});
```

### 3. Newsletter Subscription Test
```javascript
const testNewsletter = async () => {
  console.log('📧 Testing newsletter subscription...');
  
  const subscribeResponse = await fetch('https://glohorizonapi.fly.dev/api/newsletter/subscribe', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      phoneNumber: "+233201234567",
      source: "Frontend Test"
    })
  });
  
  const data = await subscribeResponse.json();
  console.log('📧 Newsletter:', data.success ? '✅ Subscribed' : '❌ Failed');
  console.log('📱 Phone:', data.phoneNumber);
  
  return data;
};

testNewsletter();
```

### 4. Error Handling Example
```javascript
const robustApiCall = async (url, options) => {
  try {
    console.log(`🔄 Calling: ${url}`);
    
    const response = await fetch(url, {
      headers: { 'Content-Type': 'application/json' },
      ...options
    });
    
    console.log(`📊 Status: ${response.status} ${response.statusText}`);
    
    const data = await response.json();
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${data.message || 'Request failed'}`);
    }
    
    if (!data.success) {
      throw new Error(`API Error: ${data.message}`);
    }
    
    console.log('✅ Success:', data.message);
    return data;
    
  } catch (error) {
    console.error('❌ Error:', error.message);
    
    // Handle specific error types
    if (error.message.includes('401')) {
      console.log('🔐 Authentication required');
    } else if (error.message.includes('400')) {
      console.log('📝 Check request format');
    } else if (error.message.includes('500')) {
      console.log('🔧 Server error - try again later');
    }
    
    throw error;
  }
};

// Usage
robustApiCall('https://glohorizonapi.fly.dev/api/quote/test-minimal', {
  method: 'POST'
});
```

### 5. React Hook Example
```javascript
// Custom hook for quotes
const useQuoteSubmission = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [quote, setQuote] = useState(null);
  
  const submitQuote = async (type, data) => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await fetch(`https://glohorizonapi.fly.dev/api/quote/${type}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      });
      
      const result = await response.json();
      
      if (result.success) {
        setQuote(result);
        // Save reference number
        localStorage.setItem('lastQuoteRef', result.referenceNumber);
      } else {
        setError(result.message);
      }
      
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };
  
  return { submitQuote, loading, error, quote };
};

// Usage in component
const QuoteForm = () => {
  const { submitQuote, loading, error, quote } = useQuoteSubmission();
  
  const handleSubmit = (formData) => {
    submitQuote('flight', {
      contactEmail: formData.email,
      contactPhone: formData.phone,
      contactName: formData.name,
      urgency: 0,
      flightDetails: {
        departureCity: formData.from,
        arrivalCity: formData.to,
        departureDate: formData.departDate,
        returnDate: formData.returnDate,
        passengerCount: formData.passengers,
        travelClass: formData.class,
        tripType: formData.tripType
      }
    });
  };
  
  if (loading) return <div>Submitting quote...</div>;
  if (error) return <div>Error: {error}</div>;
  if (quote) return <div>Quote submitted! Reference: {quote.referenceNumber}</div>;
  
  return <form onSubmit={handleSubmit}>...</form>;
};
```

### 6. Environment Configuration
```javascript
// config.js
const API_CONFIG = {
  development: {
    baseUrl: 'http://localhost:5080',
    timeout: 5000
  },
  production: {
    baseUrl: 'https://glohorizonapi.fly.dev',
    timeout: 10000
  },
  staging: {
    baseUrl: 'https://staging-glohorizonapi.fly.dev',
    timeout: 8000
  }
};

const getCurrentConfig = () => {
  const env = process.env.NODE_ENV || 'development';
  return API_CONFIG[env] || API_CONFIG.development;
};

// api.js
const config = getCurrentConfig();

const apiCall = async (endpoint, options = {}) => {
  const url = `${config.baseUrl}${endpoint}`;
  
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), config.timeout);
  
  try {
    const response = await fetch(url, {
      signal: controller.signal,
      headers: { 'Content-Type': 'application/json' },
      ...options
    });
    
    clearTimeout(timeoutId);
    return await response.json();
    
  } catch (error) {
    clearTimeout(timeoutId);
    if (error.name === 'AbortError') {
      throw new Error('Request timeout');
    }
    throw error;
  }
};

export { apiCall, config };
```

## 🎯 Quick Test Checklist

Run these in your browser console:

```javascript
// 1. API Connectivity
await fetch('https://glohorizonapi.fly.dev/api/quote/test-minimal', {method: 'POST'})
  .then(r => r.json()).then(d => console.log('API:', d.success ? '✅' : '❌'));

// 2. Flight Quote
await fetch('https://glohorizonapi.fly.dev/api/quote/flight', {
  method: 'POST',
  headers: {'Content-Type': 'application/json'},
  body: JSON.stringify({
    contactEmail: "test@example.com",
    contactPhone: "+233123456789",
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
}).then(r => r.json()).then(d => console.log('Flight:', d.success ? '✅' + d.referenceNumber : '❌'));

// 3. Newsletter
await fetch('https://glohorizonapi.fly.dev/api/newsletter/subscribe', {
  method: 'POST',
  headers: {'Content-Type': 'application/json'},
  body: JSON.stringify({phoneNumber: "+233200000000", source: "Test"})
}).then(r => r.json()).then(d => console.log('Newsletter:', d.success ? '✅' : '❌'));
```

Copy and paste any of these examples to get started immediately! 🚀