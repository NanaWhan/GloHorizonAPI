# 📧 Newsletter SMS Subscription API

The Newsletter API allows users to subscribe to SMS updates and travel news from Global Horizons Travel.

## 🚀 Live API Base URL
```
https://glohorizonapi.fly.dev/api/newsletter
```

## 📱 Endpoints

### 1. Subscribe to Newsletter
Subscribe a phone number to receive SMS travel updates.

**Endpoint:** `POST /api/newsletter/subscribe`
**Access:** Public (No authentication required)

#### Request Body:
```json
{
  "phoneNumber": "0541458512",
  "source": "Website"
}
```

#### Request Fields:
- `phoneNumber` (required): Valid Ghanaian phone number (format: 0XXXXXXXXX or +233XXXXXXXXX)
- `source` (optional): Source of subscription (e.g., "Website", "Mobile App", "API")

#### Response (Success):
```json
{
  "success": true,
  "message": "Successfully subscribed! You'll receive travel deals and updates via SMS.",
  "phoneNumber": "+233541458512",
  "subscribedAt": "2025-08-01T23:45:00Z"
}
```

#### Response (Already Subscribed):
```json
{
  "success": true,
  "message": "You are already subscribed to our travel updates!",
  "phoneNumber": "+233541458512",
  "subscribedAt": "2025-08-01T20:30:00Z"
}
```

#### Response (Error):
```json
{
  "success": false,
  "message": "Invalid phone number format. Please enter a valid Ghanaian phone number."
}
```

### 2. Unsubscribe from Newsletter
Remove a phone number from SMS updates.

**Endpoint:** `POST /api/newsletter/unsubscribe`
**Access:** Public (No authentication required)

#### Request Body:
```json
{
  "phoneNumber": "0541458512"
}
```

#### Response (Success):
```json
{
  "success": true,
  "message": "Successfully unsubscribed from travel updates."
}
```

### 3. Newsletter Statistics
Get subscription statistics (Admin only).

**Endpoint:** `GET /api/newsletter/stats`
**Access:** Requires authentication

#### Response:
```json
{
  "activeSubscribers": 150,
  "unsubscribedCount": 25,
  "recentSubscriptions": 12,
  "lastUpdated": "2025-08-01T23:45:00Z"
}
```

## 📲 SMS Notifications

### Welcome SMS (New Subscription):
```
Welcome to Global Horizons Travel! 🌍 You'll receive exclusive travel deals, destination updates & special offers. Reply STOP to unsubscribe.
```

### Re-activation SMS:
```
Welcome back to Global Horizons! 🌍 You'll receive exclusive travel deals & updates. Reply STOP to unsubscribe.
```

### Unsubscribe Confirmation:
```
You've been unsubscribed from Global Horizons travel updates. To re-subscribe, visit our website. Thank you! 🌍
```

### Admin Notification (New Subscription):
```
📧 NEW NEWSLETTER SUBSCRIPTION: +233541458512 subscribed via Website to travel updates! Total active subscribers growing. - Global Horizons
```

## 📝 Testing Examples

### Test Newsletter Subscription:
```bash
curl -X POST "https://glohorizonapi.fly.dev/api/newsletter/subscribe" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "0541458512",
    "source": "API Test"
  }'
```

### Test Newsletter Unsubscription:
```bash
curl -X POST "https://glohorizonapi.fly.dev/api/newsletter/unsubscribe" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "0541458512"
  }'
```

## 🔒 Phone Number Formats Supported

The API automatically formats and validates Ghanaian phone numbers:

- `0541458512` → `+233541458512`
- `+233541458512` → `+233541458512` 
- `233541458512` → `+233233541458512`
- `541458512` → `+233541458512`

## ⚠️ Important Notes

1. **Unique Phone Numbers**: Each phone number can only be subscribed once
2. **Automatic SMS**: Welcome SMS sent immediately upon subscription
3. **Admin Alerts**: Admins (0205078908, 0240464248) receive notifications for new subscriptions
4. **Re-activation**: Previously unsubscribed numbers can re-subscribe
5. **Validation**: Only valid Ghanaian phone number formats accepted
6. **Database Migration**: Run migration `20250801234000_AddNewsletterSubscribers` to enable functionality

## 🎯 Integration Tips

### Frontend Integration:
```javascript
const subscribeToNewsletter = async (phoneNumber) => {
  try {
    const response = await fetch('https://glohorizonapi.fly.dev/api/newsletter/subscribe', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        phoneNumber: phoneNumber,
        source: 'Website'
      })
    });
    
    const result = await response.json();
    
    if (result.success) {
      alert('Successfully subscribed to travel updates!');
    } else {
      alert(result.message);
    }
  } catch (error) {
    alert('Unable to process subscription. Please try again.');
  }
};
```

### Mobile App Integration:
```javascript
const subscribeNewsletter = (phoneNumber) => {
  return fetch('https://glohorizonapi.fly.dev/api/newsletter/subscribe', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      phoneNumber: phoneNumber,
      source: 'Mobile App'
    })
  }).then(response => response.json());
};
```

## 🚀 Ready for Production!

The Newsletter API is fully deployed and ready for use at:
**https://glohorizonapi.fly.dev/api/newsletter**

*Note: Database migration needs to be applied before the endpoint will work correctly.*