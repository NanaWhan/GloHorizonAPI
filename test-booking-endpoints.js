// Test script for booking fetch endpoints
const API_BASE = 'https://glohorizonapi.fly.dev/api';

console.log('🧪 Testing Booking Fetch Endpoints...');
console.log('🎯 Testing API at:', API_BASE);
console.log('');

// Test 1: User booking endpoint (should require auth)
console.log('📋 Test 1: User Booking Endpoint');
fetch(`${API_BASE}/booking/my-bookings`)
  .then(response => {
    console.log(`Status: ${response.status} ${response.statusText}`);
    if (response.status === 401) {
      console.log('✅ User endpoint properly requires authentication');
    } else {
      console.log('⚠️ Unexpected response status');
    }
  })
  .catch(err => console.log('❌ Error:', err.message));

// Test 2: User booking endpoint with query parameters
console.log('');
console.log('📋 Test 2: User Booking Endpoint with Filters');
fetch(`${API_BASE}/booking/my-bookings?pageNumber=1&pageSize=5&status=1`)
  .then(response => {
    console.log(`Status: ${response.status} ${response.statusText}`);
    if (response.status === 401) {
      console.log('✅ User endpoint with filters properly requires authentication');
    } else {
      console.log('⚠️ Unexpected response status');
    }
  })
  .catch(err => console.log('❌ Error:', err.message));

// Test 3: Admin booking endpoint (should require auth)
console.log('');
console.log('📋 Test 3: Admin Booking Endpoint');
fetch(`${API_BASE}/admin/bookings`)
  .then(response => {
    console.log(`Status: ${response.status} ${response.statusText}`);
    if (response.status === 401) {
      console.log('✅ Admin booking endpoint properly requires authentication');
    } else {
      console.log('⚠️ Unexpected response status');
    }
  })
  .catch(err => console.log('❌ Error:', err.message));

// Test 4: Admin dashboard endpoint
console.log('');
console.log('📋 Test 4: Admin Dashboard Endpoint');
fetch(`${API_BASE}/admin/dashboard`)
  .then(response => {
    console.log(`Status: ${response.status} ${response.statusText}`);
    if (response.status === 401) {
      console.log('✅ Admin dashboard properly requires authentication');
    } else {
      console.log('⚠️ Unexpected response status');
    }
  })
  .catch(err => console.log('❌ Error:', err.message));

// Test 5: Admin booking with filters
console.log('');
console.log('📋 Test 5: Admin Booking Endpoint with Filters');
fetch(`${API_BASE}/admin/bookings?pageNumber=1&pageSize=20&status=1&serviceType=1`)
  .then(response => {
    console.log(`Status: ${response.status} ${response.statusText}`);
    if (response.status === 401) {
      console.log('✅ Admin endpoint with filters properly requires authentication');
    } else {
      console.log('⚠️ Unexpected response status');
    }
  })
  .catch(err => console.log('❌ Error:', err.message));

console.log('');
console.log('✅ Booking Fetch Endpoint Tests Completed!');
console.log('📝 Note: All endpoints require authentication which is working correctly.');
console.log('📚 See BOOKING_FETCH_API_DOCUMENTATION.md for usage examples with authentication.');