#!/usr/bin/env node

// 🧪 GloHorizon Quote API Test Script
// Run with: node test-quote-api.js

const https = require('https');
const http = require('http');

// Configuration
const BASE_URL = 'http://localhost:5080'; // Change to your API URL
const API_PREFIX = '/api/quote';

// Colors for console output
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  magenta: '\x1b[35m',
  cyan: '\x1b[36m'
};

// Test data
const testData = {
  hotel: {
    hotelDetails: {
      destination: "Dubai",
      checkInDate: "2024-09-15T00:00:00Z",
      checkOutDate: "2024-09-20T00:00:00Z",
      rooms: 1,
      adultGuests: 2,
      childGuests: 0,
      roomType: "deluxe",
      starRating: "4-star",
      amenities: ["pool", "spa", "gym"]
    },
    contactEmail: "test@example.com",
    contactPhone: "+233123456789",
    contactName: "John Test User",
    specialRequests: "Sea view room preferred",
    urgency: 1
  },
  
  flight: {
    flightDetails: {
      tripType: "round-trip",
      departureCity: "Accra",
      arrivalCity: "London",
      departureDate: "2024-10-01T00:00:00Z",
      returnDate: "2024-10-10T00:00:00Z",
      adultPassengers: 2,
      childPassengers: 1,
      infantPassengers: 0,
      preferredClass: "economy",
      preferredAirline: "British Airways",
      passengers: [
        {
          firstName: "John",
          lastName: "Doe",
          dateOfBirth: "1990-01-15",
          passportNumber: "A12345678",
          nationality: "Ghanaian"
        }
      ]
    },
    contactEmail: "test@example.com",
    contactPhone: "+233123456789",
    contactName: "John Test User",
    specialRequests: "Window seat preferred",
    urgency: 2
  },

  tour: {
    tourDetails: {
      tourPackage: "Safari Adventure",
      destination: "Kenya",
      startDate: "2024-11-01T00:00:00Z",
      endDate: "2024-11-07T00:00:00Z",
      travelers: 3,
      accommodationType: "luxury",
      tourType: "private",
      activities: ["game drives", "cultural visits", "photography"],
      mealPlan: "full-board"
    },
    contactEmail: "test@example.com",
    contactPhone: "+233123456789",
    contactName: "John Test User",
    specialRequests: "Photography guide required",
    urgency: 1
  },

  visa: {
    visaDetails: {
      visaType: "Tourist Visa",
      destinationCountry: "United States",
      processingType: "standard",
      intendedTravelDate: "2024-12-01T00:00:00Z",
      durationOfStay: 14,
      purposeOfVisit: "Tourism",
      passportNumber: "A12345678",
      passportExpiryDate: "2030-01-01T00:00:00Z",
      nationality: "Ghanaian",
      hasPreviousVisa: false,
      requiredDocuments: [
        {
          documentType: "passport",
          documentName: "Valid Passport",
          isRequired: true,
          isUploaded: false
        }
      ]
    },
    contactEmail: "test@example.com",
    contactPhone: "+233123456789",
    contactName: "John Test User",
    specialRequests: "Need expedited processing",
    urgency: 3
  },

  package: {
    packageDetails: {
      flightDetails: {
        tripType: "round-trip",
        departureCity: "Accra",
        arrivalCity: "Paris",
        departureDate: "2024-12-15T00:00:00Z",
        returnDate: "2024-12-22T00:00:00Z",
        adultPassengers: 2,
        preferredClass: "business"
      },
      hotelDetails: {
        destination: "Paris",
        checkInDate: "2024-12-15T00:00:00Z",
        checkOutDate: "2024-12-22T00:00:00Z",
        rooms: 1,
        adultGuests: 2,
        roomType: "suite",
        starRating: "5-star"
      },
      packageType: "honeymoon",
      estimatedBudget: 25000,
      additionalServices: ["travel insurance", "airport transfers"]
    },
    contactEmail: "test@example.com",
    contactPhone: "+233123456789",
    contactName: "John Test User",
    specialRequests: "Honeymoon package with special amenities",
    urgency: 1
  }
};

// HTTP request helper
function makeRequest(method, url, data = null) {
  return new Promise((resolve, reject) => {
    const urlObj = new URL(url);
    const options = {
      hostname: urlObj.hostname,
      port: urlObj.port || (urlObj.protocol === 'https:' ? 443 : 80),
      path: urlObj.pathname,
      method: method,
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      },
      // Allow self-signed certificates for localhost testing
      rejectUnauthorized: false
    };

    if (data) {
      const postData = JSON.stringify(data);
      options.headers['Content-Length'] = Buffer.byteLength(postData);
    }

    const client = urlObj.protocol === 'https:' ? https : http;
    const req = client.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => {
        body += chunk;
      });
      res.on('end', () => {
        try {
          const response = {
            statusCode: res.statusCode,
            headers: res.headers,
            body: body ? JSON.parse(body) : null
          };
          resolve(response);
        } catch (e) {
          resolve({
            statusCode: res.statusCode,
            headers: res.headers,
            body: body,
            parseError: e.message
          });
        }
      });
    });

    req.on('error', (err) => {
      reject(err);
    });

    if (data) {
      req.write(JSON.stringify(data));
    }

    req.end();
  });
}

// Test functions
async function testQuoteSubmission(serviceType, data) {
  console.log(`\n${colors.blue}🧪 Testing ${serviceType.toUpperCase()} Quote Request...${colors.reset}`);
  
  try {
    const response = await makeRequest('POST', `${BASE_URL}${API_PREFIX}/${serviceType}`, data);
    
    if (response.statusCode === 200 && response.body.success) {
      console.log(`${colors.green}✅ SUCCESS: ${serviceType} quote submitted${colors.reset}`);
      console.log(`${colors.cyan}📋 Reference Number: ${response.body.referenceNumber}${colors.reset}`);
      console.log(`${colors.yellow}💬 Message: ${response.body.message}${colors.reset}`);
      return response.body.referenceNumber;
    } else {
      console.log(`${colors.red}❌ FAILED: ${serviceType} quote submission${colors.reset}`);
      console.log(`${colors.red}Status: ${response.statusCode}${colors.reset}`);
      console.log(`${colors.red}Response: ${JSON.stringify(response.body, null, 2)}${colors.reset}`);
      return null;
    }
  } catch (error) {
    console.log(`${colors.red}❌ ERROR: ${serviceType} quote request failed${colors.reset}`);
    console.log(`${colors.red}${error.message}${colors.reset}`);
    return null;
  }
}

async function testQuoteTracking(referenceNumber) {
  if (!referenceNumber) {
    console.log(`${colors.yellow}⚠️  Skipping tracking test - no reference number${colors.reset}`);
    return;
  }

  console.log(`\n${colors.blue}🔍 Testing Quote Tracking...${colors.reset}`);
  
  try {
    const response = await makeRequest('GET', `${BASE_URL}${API_PREFIX}/track/${referenceNumber}`);
    
    if (response.statusCode === 200) {
      console.log(`${colors.green}✅ SUCCESS: Quote tracking works${colors.reset}`);
      console.log(`${colors.cyan}📊 Status: ${getStatusName(response.body.status)}${colors.reset}`);
      console.log(`${colors.cyan}🎯 Service: ${getServiceName(response.body.serviceType)}${colors.reset}`);
      console.log(`${colors.cyan}📅 Created: ${new Date(response.body.createdAt).toLocaleString()}${colors.reset}`);
    } else {
      console.log(`${colors.red}❌ FAILED: Quote tracking${colors.reset}`);
      console.log(`${colors.red}Status: ${response.statusCode}${colors.reset}`);
      console.log(`${colors.red}Response: ${JSON.stringify(response.body, null, 2)}${colors.reset}`);
    }
  } catch (error) {
    console.log(`${colors.red}❌ ERROR: Quote tracking failed${colors.reset}`);
    console.log(`${colors.red}${error.message}${colors.reset}`);
  }
}

async function testInvalidTracking() {
  console.log(`\n${colors.blue}🔍 Testing Invalid Reference Number...${colors.reset}`);
  
  try {
    const response = await makeRequest('GET', `${BASE_URL}${API_PREFIX}/track/INVALID123`);
    
    if (response.statusCode === 404) {
      console.log(`${colors.green}✅ SUCCESS: Invalid tracking properly returns 404${colors.reset}`);
    } else {
      console.log(`${colors.yellow}⚠️  UNEXPECTED: Expected 404, got ${response.statusCode}${colors.reset}`);
    }
  } catch (error) {
    console.log(`${colors.red}❌ ERROR: Invalid tracking test failed${colors.reset}`);
    console.log(`${colors.red}${error.message}${colors.reset}`);
  }
}

// Helper functions
function getStatusName(status) {
  const statuses = {
    1: "Submitted",
    2: "Under Review", 
    3: "Quote Provided",
    4: "Payment Pending",
    5: "Paid",
    6: "Booking Confirmed",
    7: "Expired",
    8: "Cancelled"
  };
  return statuses[status] || `Unknown (${status})`;
}

function getServiceName(serviceType) {
  const services = {
    1: "Flight",
    2: "Hotel",
    3: "Tour", 
    4: "Visa",
    5: "Complete Package"
  };
  return services[serviceType] || `Unknown (${serviceType})`;
}

// Main test runner
async function runTests() {
  console.log(`${colors.bright}${colors.magenta}🚀 GloHorizon Quote API Test Suite${colors.reset}`);
  console.log(`${colors.cyan}🎯 Testing API at: ${BASE_URL}${colors.reset}`);
  console.log(`${colors.yellow}⏰ Started at: ${new Date().toLocaleString()}${colors.reset}`);
  
  const referenceNumbers = [];
  
  // Test all quote types
  console.log(`\n${colors.bright}📋 PHASE 1: Quote Submission Tests${colors.reset}`);
  
  const hotelRef = await testQuoteSubmission('hotel', testData.hotel);
  if (hotelRef) referenceNumbers.push(hotelRef);
  
  const flightRef = await testQuoteSubmission('flight', testData.flight);
  if (flightRef) referenceNumbers.push(flightRef);
  
  const tourRef = await testQuoteSubmission('tour', testData.tour);
  if (tourRef) referenceNumbers.push(tourRef);
  
  const visaRef = await testQuoteSubmission('visa', testData.visa);
  if (visaRef) referenceNumbers.push(visaRef);
  
  const packageRef = await testQuoteSubmission('complete-package', testData.package);
  if (packageRef) referenceNumbers.push(packageRef);
  
  // Test quote tracking
  console.log(`\n${colors.bright}📋 PHASE 2: Quote Tracking Tests${colors.reset}`);
  
  if (referenceNumbers.length > 0) {
    await testQuoteTracking(referenceNumbers[0]);
  }
  
  await testInvalidTracking();
  
  // Summary
  console.log(`\n${colors.bright}📊 TEST SUMMARY${colors.reset}`);
  console.log(`${colors.green}✅ Quote submissions attempted: 5${colors.reset}`);
  console.log(`${colors.green}✅ Successful submissions: ${referenceNumbers.length}${colors.reset}`);
  console.log(`${colors.cyan}📋 Reference numbers generated: ${referenceNumbers.length}${colors.reset}`);
  
  if (referenceNumbers.length > 0) {
    console.log(`\n${colors.bright}📝 Generated Reference Numbers:${colors.reset}`);
    referenceNumbers.forEach((ref, index) => {
      console.log(`${colors.cyan}${index + 1}. ${ref}${colors.reset}`);
    });
    
    console.log(`\n${colors.yellow}💡 Use these reference numbers to test tracking in your frontend!${colors.reset}`);
  }
  
  console.log(`\n${colors.bright}${colors.green}🎉 Test Suite Complete!${colors.reset}`);
  console.log(`${colors.yellow}⏰ Finished at: ${new Date().toLocaleString()}${colors.reset}`);
}

// Error handling
process.on('unhandledRejection', (error) => {
  console.log(`${colors.red}❌ Unhandled Promise Rejection:${colors.reset}`);
  console.log(`${colors.red}${error.stack}${colors.reset}`);
  process.exit(1);
});

// Run the tests
if (require.main === module) {
  runTests().catch(console.error);
}

module.exports = { runTests, testQuoteSubmission, testQuoteTracking };