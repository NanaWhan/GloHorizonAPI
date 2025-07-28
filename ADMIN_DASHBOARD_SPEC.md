# GloHorizon Admin Dashboard - Technical Specification

## Overview
This document provides detailed technical specifications for building the GloHorizon Travel Admin Dashboard frontend. It includes API contracts, data models, UI requirements, and workflow specifications.

## Base API Configuration

```javascript
const API_BASE_URL = 'http://localhost:5080/api'
// Production: 'https://your-domain.com/api'

const API_ENDPOINTS = {
  // Authentication
  ADMIN_LOGIN: '/admin/login',
  ADMIN_CREATE: '/admin/create',
  
  // Bookings
  BOOKINGS_LIST: '/admin/bookings',
  BOOKING_DETAIL: '/admin/bookings/{id}',
  BOOKING_STATUS: '/admin/bookings/{id}/status',
  BOOKING_PRICING: '/admin/bookings/{id}/pricing',
  BOOKING_NOTES: '/admin/bookings/{id}/notes',
  PAYMENT_LINK: '/admin/bookings/{id}/payment-link',
  
  // Dashboard
  DASHBOARD: '/admin/dashboard',
  
  // Payments
  PAYMENT_VERIFY: '/payment/verify/{reference}',
  
  // Images
  IMAGE_UPLOAD: '/image/upload',
  IMAGE_UPLOAD_MULTIPLE: '/image/upload-multiple'
}
```

## Authentication System

### Login API
```http
POST /api/admin/login
Content-Type: application/json

{
  "email": "admin@globalhorizonstravel.com",
  "password": "admin-password"
}
```

### Response
```json
{
  "success": true,
  "message": "Admin login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "admin-uuid",
    "fullName": "Admin User",
    "email": "admin@globalhorizonstravel.com",
    "phoneNumber": "+233123456789"
  }
}
```

### Token Management
```javascript
// Store token in localStorage/sessionStorage
localStorage.setItem('adminToken', response.token)
localStorage.setItem('adminUser', JSON.stringify(response.user))

// Add to all API requests
const headers = {
  'Authorization': `Bearer ${localStorage.getItem('adminToken')}`,
  'Content-Type': 'application/json'
}
```

## Dashboard Overview API

### Request
```http
GET /api/admin/dashboard
Authorization: Bearer {token}
```

### Response
```json
{
  "totalBookings": 145,
  "pendingBookings": 23,
  "completedBookings": 89,
  "totalUsers": 67,
  "recentBookings": [
    {
      "id": 1,
      "referenceNumber": "GLOHORIZON-20250128-001",
      "serviceType": 1,
      "status": 2,
      "urgency": 1,
      "createdAt": "2025-01-28T10:30:00Z",
      "estimatedPrice": 1200.00,
      "finalPrice": null,
      "currency": "USD"
    }
  ]
}
```

### Dashboard Cards Layout
```javascript
const dashboardCards = [
  {
    title: 'Total Bookings',
    value: totalBookings,
    icon: 'BookingIcon',
    color: 'blue',
    trend: '+12% from last month'
  },
  {
    title: 'Pending Review',
    value: pendingBookings,
    icon: 'ClockIcon',
    color: 'orange',
    urgent: true,
    action: 'View Pending'
  },
  {
    title: 'Completed',
    value: completedBookings,
    icon: 'CheckIcon',
    color: 'green',
    trend: '+8% from last month'
  },
  {
    title: 'Total Customers',
    value: totalUsers,
    icon: 'UserIcon',
    color: 'purple',
    trend: '+15 new this month'
  }
]
```

## Bookings Management

### Bookings List API
```http
GET /api/admin/bookings?status=1&serviceType=1&page=1&pageSize=20
Authorization: Bearer {token}
```

### Bookings List Response
```json
[
  {
    "id": 1,
    "referenceNumber": "GLOHORIZON-20250128-001",
    "serviceType": 1,
    "status": 2,
    "urgency": 1,
    "createdAt": "2025-01-28T10:30:00Z",
    "estimatedPrice": 1200.00,
    "finalPrice": null,
    "currency": "USD"
  }
]
```

### Bookings Table Configuration
```javascript
const bookingsTableColumns = [
  {
    key: 'referenceNumber',
    title: 'Reference',
    sortable: true,
    render: (value, row) => (
      <Link to={`/bookings/${row.id}`} className="text-blue-600 font-mono">
        {value}
      </Link>
    )
  },
  {
    key: 'serviceType',
    title: 'Service',
    render: (value) => SERVICE_TYPES[value].name,
    filter: true,
    filterOptions: Object.values(SERVICE_TYPES)
  },
  {
    key: 'status',
    title: 'Status',
    render: (value) => (
      <StatusBadge status={value} />
    ),
    filter: true,
    filterOptions: Object.values(BOOKING_STATUSES)
  },
  {
    key: 'urgency',
    title: 'Priority',
    render: (value) => (
      <UrgencyBadge urgency={value} />
    ),
    filter: true
  },
  {
    key: 'createdAt',
    title: 'Created',
    sortable: true,
    render: (value) => formatDate(value)
  },
  {
    key: 'estimatedPrice',
    title: 'Price',
    render: (value, row) => (
      value ? `${row.currency} ${value.toLocaleString()}` : 'Pending'
    )
  },
  {
    key: 'actions',
    title: 'Actions',
    render: (_, row) => (
      <ActionMenu bookingId={row.id} status={row.status} />
    )
  }
]
```

### Booking Detail API
```http
GET /api/admin/bookings/1
Authorization: Bearer {token}
```

### Booking Detail Response
```json
{
  "id": 1,
  "referenceNumber": "GLOHORIZON-20250128-001",
  "serviceType": 1,
  "status": 2,
  "urgency": 1,
  "createdAt": "2025-01-28T10:30:00Z",
  "updatedAt": "2025-01-28T14:20:00Z",
  "estimatedPrice": 1200.00,
  "finalPrice": null,
  "currency": "USD",
  "adminNotes": "[2025-01-28 14:20:00 UTC] Admin: Reviewing flight options for customer",
  "user": {
    "id": "user-uuid",
    "fullName": "John Doe",
    "email": "john@example.com",
    "phoneNumber": "+233123456789"
  },
  "statusHistory": [
    {
      "fromStatus": 1,
      "toStatus": 2,
      "notes": "Moving to review after initial assessment",
      "changedBy": "Admin User",
      "changedAt": "2025-01-28T14:20:00Z"
    }
  ],
  "bookingDetails": {
    "departure": "Accra (ACC)",
    "destination": "London (LHR)",
    "departureDate": "2025-03-15",
    "returnDate": "2025-03-25",
    "passengers": 2,
    "classPreference": "Economy",
    "specialRequests": "Window seats preferred"
  }
}
```

## Data Models & Enums

### Service Types
```javascript
const SERVICE_TYPES = {
  1: { name: 'Flight', icon: 'PlaneIcon', color: 'blue' },
  2: { name: 'Hotel', icon: 'HotelIcon', color: 'green' },
  3: { name: 'Tour', icon: 'MapIcon', color: 'purple' },
  4: { name: 'Visa', icon: 'DocumentIcon', color: 'orange' },
  5: { name: 'Complete Package', icon: 'PackageIcon', color: 'indigo' }
}
```

### Booking Statuses
```javascript
const BOOKING_STATUSES = {
  1: { 
    name: 'Pending', 
    color: 'gray', 
    bgColor: 'bg-gray-100', 
    textColor: 'text-gray-800',
    icon: 'ClockIcon',
    description: 'New booking awaiting review',
    nextActions: ['Review', 'Assign']
  },
  2: { 
    name: 'Under Review', 
    color: 'yellow', 
    bgColor: 'bg-yellow-100', 
    textColor: 'text-yellow-800',
    icon: 'EyeIcon',
    description: 'Admin is researching options',
    nextActions: ['Set Pricing', 'Request Info']
  },
  3: { 
    name: 'Quote Ready', 
    color: 'blue', 
    bgColor: 'bg-blue-100', 
    textColor: 'text-blue-800',
    icon: 'DocumentTextIcon',
    description: 'Quote prepared for customer',
    nextActions: ['Contact Customer', 'Modify Quote']
  },
  4: { 
    name: 'Quote Accepted', 
    color: 'green', 
    bgColor: 'bg-green-100', 
    textColor: 'text-green-800',
    icon: 'CheckIcon',
    description: 'Customer accepted the quote',
    nextActions: ['Generate Payment Link', 'Set Final Price']
  },
  5: { 
    name: 'Payment Pending', 
    color: 'orange', 
    bgColor: 'bg-orange-100', 
    textColor: 'text-orange-800',
    icon: 'CreditCardIcon',
    description: 'Awaiting customer payment',
    nextActions: ['Check Payment', 'Send Reminder']
  },
  6: { 
    name: 'Processing', 
    color: 'indigo', 
    bgColor: 'bg-indigo-100', 
    textColor: 'text-indigo-800',
    icon: 'CogIcon',
    description: 'Booking services with suppliers',
    nextActions: ['Update Progress', 'Upload Documents']
  },
  7: { 
    name: 'Confirmed', 
    color: 'green', 
    bgColor: 'bg-green-100', 
    textColor: 'text-green-800',
    icon: 'ShieldCheckIcon',
    description: 'All services confirmed',
    nextActions: ['Send Details', 'Mark Complete']
  },
  8: { 
    name: 'Completed', 
    color: 'green', 
    bgColor: 'bg-green-100', 
    textColor: 'text-green-800',
    icon: 'CheckCircleIcon',
    description: 'Travel completed successfully',
    nextActions: ['Archive', 'Request Feedback']
  },
  9: { 
    name: 'Cancelled', 
    color: 'red', 
    bgColor: 'bg-red-100', 
    textColor: 'text-red-800',
    icon: 'XCircleIcon',
    description: 'Booking cancelled',
    nextActions: ['Process Refund', 'Archive']
  },
  10: { 
    name: 'Rejected', 
    color: 'red', 
    bgColor: 'bg-red-100', 
    textColor: 'text-red-800',
    icon: 'XIcon',
    description: 'Quote rejected by customer',
    nextActions: ['Revise Quote', 'Archive']
  }
}
```

### Urgency Levels
```javascript
const URGENCY_LEVELS = {
  1: { 
    name: 'Standard', 
    color: 'green', 
    responseTime: '4-8 hours',
    icon: 'ClockIcon'
  },
  2: { 
    name: 'Urgent', 
    color: 'orange', 
    responseTime: '2-4 hours',
    icon: 'ExclamationIcon'
  },
  3: { 
    name: 'Emergency', 
    color: 'red', 
    responseTime: '1 hour',
    icon: 'ExclamationTriangleIcon',
    alert: true
  }
}
```

## Action APIs

### Update Booking Status
```http
PUT /api/admin/bookings/1/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "newStatus": 3,
  "notes": "Quote prepared and ready for customer",
  "adminNotes": "Included economy class flights with baggage",
  "estimatedPrice": 1200.00,
  "finalPrice": 1150.00
}
```

### Update Pricing
```http
PUT /api/admin/bookings/1/pricing
Authorization: Bearer {token}
Content-Type: application/json

{
  "estimatedPrice": 1200.00,
  "finalPrice": 1150.00,
  "currency": "USD",
  "notes": "Early booking discount applied"
}
```

### Add Note
```http
POST /api/admin/bookings/1/notes
Authorization: Bearer {token}
Content-Type: application/json

{
  "note": "Customer confirmed travel dates via phone call"
}
```

### Generate Payment Link
```http
POST /api/admin/bookings/1/payment-link
Authorization: Bearer {token}
```

Response:
```json
{
  "success": true,
  "message": "Payment link generated successfully",
  "paymentUrl": "https://checkout.paystack.com/xyz123",
  "reference": "GLOHORIZON-20250128-001",
  "amount": 1150.00,
  "currency": "USD"
}
```

## UI Component Specifications

### StatusBadge Component
```jsx
const StatusBadge = ({ status }) => {
  const statusInfo = BOOKING_STATUSES[status]
  
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${statusInfo.bgColor} ${statusInfo.textColor}`}>
      <statusInfo.icon className="w-3 h-3 mr-1" />
      {statusInfo.name}
    </span>
  )
}
```

### UrgencyBadge Component
```jsx
const UrgencyBadge = ({ urgency }) => {
  const urgencyInfo = URGENCY_LEVELS[urgency]
  const baseClasses = "inline-flex items-center px-2 py-1 rounded text-xs font-medium"
  const colorClasses = {
    green: "bg-green-100 text-green-800",
    orange: "bg-orange-100 text-orange-800", 
    red: "bg-red-100 text-red-800"
  }
  
  return (
    <span className={`${baseClasses} ${colorClasses[urgencyInfo.color]} ${urgencyInfo.alert ? 'animate-pulse' : ''}`}>
      <urgencyInfo.icon className="w-3 h-3 mr-1" />
      {urgencyInfo.name}
    </span>
  )
}
```

### ActionMenu Component
```jsx
const ActionMenu = ({ bookingId, status }) => {
  const statusInfo = BOOKING_STATUSES[status]
  
  return (
    <Menu as="div" className="relative inline-block text-left">
      <Menu.Button className="inline-flex justify-center w-full px-2 py-1 text-sm bg-white rounded-md hover:bg-gray-50">
        Actions
        <ChevronDownIcon className="w-4 h-4 ml-1" />
      </Menu.Button>
      
      <Menu.Items className="absolute right-0 w-48 mt-2 bg-white rounded-md shadow-lg ring-1 ring-black ring-opacity-5">
        {statusInfo.nextActions.map(action => (
          <Menu.Item key={action}>
            <button onClick={() => handleAction(action, bookingId)}>
              {action}
            </button>
          </Menu.Item>
        ))}
      </Menu.Items>
    </Menu>
  )
}
```

### BookingDetailModal Component
```jsx
const BookingDetailModal = ({ bookingId, onClose }) => {
  const [booking, setBooking] = useState(null)
  const [loading, setLoading] = useState(true)
  
  useEffect(() => {
    fetchBookingDetail(bookingId)
      .then(setBooking)
      .finally(() => setLoading(false))
  }, [bookingId])
  
  return (
    <Modal onClose={onClose} size="xl">
      <div className="p-6">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-xl font-semibold">
            Booking Details - {booking?.referenceNumber}
          </h2>
          <StatusBadge status={booking?.status} />
        </div>
        
        <div className="grid grid-cols-2 gap-6">
          {/* Customer Info */}
          <CustomerInfoPanel customer={booking?.user} />
          
          {/* Booking Details */}
          <BookingDetailsPanel details={booking?.bookingDetails} />
          
          {/* Status History */}
          <StatusHistoryPanel history={booking?.statusHistory} />
          
          {/* Admin Notes */}
          <AdminNotesPanel notes={booking?.adminNotes} />
        </div>
        
        <ActionPanel bookingId={booking?.id} status={booking?.status} />
      </div>
    </Modal>
  )
}
```

## Form Components

### StatusUpdateForm
```jsx
const StatusUpdateForm = ({ bookingId, currentStatus, onSuccess }) => {
  const [formData, setFormData] = useState({
    newStatus: currentStatus + 1,
    notes: '',
    adminNotes: '',
    estimatedPrice: '',
    finalPrice: ''
  })
  
  const handleSubmit = async (e) => {
    e.preventDefault()
    
    try {
      await updateBookingStatus(bookingId, formData)
      toast.success('Status updated successfully')
      onSuccess()
    } catch (error) {
      toast.error('Failed to update status')
    }
  }
  
  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <SelectField
        label="New Status"
        value={formData.newStatus}
        onChange={(value) => setFormData({...formData, newStatus: value})}
        options={Object.entries(BOOKING_STATUSES).map(([id, status]) => ({
          value: id,
          label: status.name
        }))}
      />
      
      <TextareaField
        label="Customer Notes"
        value={formData.notes}
        onChange={(value) => setFormData({...formData, notes: value})}
        placeholder="Notes visible to customer..."
      />
      
      <TextareaField
        label="Admin Notes"
        value={formData.adminNotes}
        onChange={(value) => setFormData({...formData, adminNotes: value})}
        placeholder="Internal admin notes..."
      />
      
      {/* Pricing fields if status allows */}
      {[3, 4].includes(formData.newStatus) && (
        <>
          <NumberField
            label="Estimated Price"
            value={formData.estimatedPrice}
            onChange={(value) => setFormData({...formData, estimatedPrice: value})}
          />
          <NumberField
            label="Final Price"
            value={formData.finalPrice}
            onChange={(value) => setFormData({...formData, finalPrice: value})}
          />
        </>
      )}
      
      <div className="flex justify-end space-x-2">
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" variant="primary">
          Update Status
        </Button>
      </div>
    </form>
  )
}
```

### PricingForm
```jsx
const PricingForm = ({ bookingId, currentPricing, onSuccess }) => {
  const [formData, setFormData] = useState({
    estimatedPrice: currentPricing?.estimatedPrice || '',
    finalPrice: currentPricing?.finalPrice || '',
    currency: currentPricing?.currency || 'USD',
    notes: ''
  })
  
  const handleSubmit = async (e) => {
    e.preventDefault()
    
    try {
      await updateBookingPricing(bookingId, formData)
      toast.success('Pricing updated successfully')
      onSuccess()
    } catch (error) {
      toast.error('Failed to update pricing')
    }
  }
  
  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <NumberField
          label="Estimated Price"
          value={formData.estimatedPrice}
          onChange={(value) => setFormData({...formData, estimatedPrice: value})}
          required
        />
        <NumberField
          label="Final Price"
          value={formData.finalPrice}
          onChange={(value) => setFormData({...formData, finalPrice: value})}
        />
      </div>
      
      <SelectField
        label="Currency"
        value={formData.currency}
        onChange={(value) => setFormData({...formData, currency: value})}
        options={[
          { value: 'USD', label: 'USD - US Dollar' },
          { value: 'GHS', label: 'GHS - Ghana Cedi' },
          { value: 'EUR', label: 'EUR - Euro' },
          { value: 'GBP', label: 'GBP - British Pound' }
        ]}
      />
      
      <TextareaField
        label="Pricing Notes"
        value={formData.notes}
        onChange={(value) => setFormData({...formData, notes: value})}
        placeholder="Explain pricing decisions..."
      />
      
      <div className="flex justify-end space-x-2">
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" variant="primary">
          Update Pricing
        </Button>
      </div>
    </form>
  )
}
```

## API Helper Functions

```javascript
// api/bookings.js
export const fetchBookings = async (filters = {}) => {
  const params = new URLSearchParams(filters)
  const response = await fetch(`${API_BASE_URL}/admin/bookings?${params}`, {
    headers: getAuthHeaders()
  })
  return response.json()
}

export const fetchBookingDetail = async (id) => {
  const response = await fetch(`${API_BASE_URL}/admin/bookings/${id}`, {
    headers: getAuthHeaders()
  })
  return response.json()
}

export const updateBookingStatus = async (id, data) => {
  const response = await fetch(`${API_BASE_URL}/admin/bookings/${id}/status`, {
    method: 'PUT',
    headers: getAuthHeaders(),
    body: JSON.stringify(data)
  })
  return response.json()
}

export const updateBookingPricing = async (id, data) => {
  const response = await fetch(`${API_BASE_URL}/admin/bookings/${id}/pricing`, {
    method: 'PUT',
    headers: getAuthHeaders(),
    body: JSON.stringify(data)
  })
  return response.json()
}

export const addBookingNote = async (id, note) => {
  const response = await fetch(`${API_BASE_URL}/admin/bookings/${id}/notes`, {
    method: 'POST',
    headers: getAuthHeaders(),
    body: JSON.stringify({ note })
  })
  return response.json()
}

export const generatePaymentLink = async (id) => {
  const response = await fetch(`${API_BASE_URL}/admin/bookings/${id}/payment-link`, {
    method: 'POST',
    headers: getAuthHeaders()
  })
  return response.json()
}

// Helper function
const getAuthHeaders = () => ({
  'Authorization': `Bearer ${localStorage.getItem('adminToken')}`,
  'Content-Type': 'application/json'
})
```

## Real-time Updates

### WebSocket Integration (Optional)
```javascript
// For real-time booking updates
const useBookingUpdates = () => {
  useEffect(() => {
    const ws = new WebSocket('ws://localhost:5080/booking-updates')
    
    ws.onmessage = (event) => {
      const update = JSON.parse(event.data)
      
      // Update booking in state
      setBookings(prev => prev.map(booking => 
        booking.id === update.bookingId 
          ? { ...booking, ...update.changes }
          : booking
      ))
      
      // Show notification
      toast.info(`Booking ${update.reference} updated`)
    }
    
    return () => ws.close()
  }, [])
}
```

### Polling Alternative
```javascript
// Poll for updates every 30 seconds
const useBookingPolling = () => {
  useEffect(() => {
    const interval = setInterval(async () => {
      const updates = await fetchRecentUpdates()
      if (updates.length > 0) {
        // Update state and show notifications
        updateBookingsState(updates)
      }
    }, 30000)
    
    return () => clearInterval(interval)
  }, [])
}
```

## Error Handling

### Global Error Handler
```javascript
const handleApiError = (error, context = '') => {
  console.error(`API Error in ${context}:`, error)
  
  if (error.status === 401) {
    // Token expired
    localStorage.removeItem('adminToken')
    navigate('/login')
    toast.error('Session expired. Please log in again.')
  } else if (error.status === 403) {
    toast.error('Access denied. Insufficient permissions.')
  } else if (error.status === 404) {
    toast.error('Resource not found.')
  } else if (error.status >= 500) {
    toast.error('Server error. Please try again later.')
  } else {
    toast.error(error.message || 'An unexpected error occurred.')
  }
}
```

## Performance Considerations

### Data Caching
```javascript
// Use React Query or SWR for caching
const useBookings = (filters) => {
  return useQuery({
    queryKey: ['bookings', filters],
    queryFn: () => fetchBookings(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    cacheTime: 10 * 60 * 1000, // 10 minutes
  })
}
```

### Pagination
```javascript
const usePaginatedBookings = () => {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  
  const { data, isLoading } = useQuery({
    queryKey: ['bookings', { page, pageSize }],
    queryFn: () => fetchBookings({ page, pageSize }),
    keepPreviousData: true
  })
  
  return {
    bookings: data?.bookings || [],
    totalCount: data?.totalCount || 0,
    currentPage: page,
    totalPages: Math.ceil((data?.totalCount || 0) / pageSize),
    setPage,
    isLoading
  }
}
```

## Testing Requirements

### Unit Tests
```javascript
// Test API functions
describe('Booking API', () => {
  test('fetchBookings returns correct data', async () => {
    const mockResponse = [{ id: 1, referenceNumber: 'TEST-001' }]
    global.fetch = jest.fn().mockResolvedValue({
      json: () => Promise.resolve(mockResponse)
    })
    
    const result = await fetchBookings()
    expect(result).toEqual(mockResponse)
  })
})

// Test components
describe('StatusBadge', () => {
  test('renders correct status', () => {
    render(<StatusBadge status={1} />)
    expect(screen.getByText('Pending')).toBeInTheDocument()
  })
})
```

### Integration Tests
```javascript
// Test complete workflows
describe('Booking Management Flow', () => {
  test('admin can update booking status', async () => {
    // Mock API responses
    // Render component
    // Simulate user actions
    // Assert state changes
  })
})
```

This specification should give you everything you need to build a comprehensive admin dashboard that perfectly integrates with your GloHorizon API!