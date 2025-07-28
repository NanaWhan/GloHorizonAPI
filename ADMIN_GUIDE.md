# GloHorizon Travel API - Admin Guide

## Overview

This guide explains how to manage the GloHorizon Travel booking system as an administrator. The system handles travel bookings including flights, hotels, tours, visas, and complete travel packages with automated payment processing and customer notifications.

## System Architecture

### Core Components
- **Booking Management System**: Handles customer travel requests
- **PayStack Payment Integration**: Processes payments with webhooks
- **Actor-Based Notifications**: Automated SMS and email notifications
- **Image Upload System**: Manages travel package and profile images
- **Admin Dashboard**: Comprehensive booking and user management

### Technology Stack
- **Backend**: ASP.NET Core 8 API
- **Database**: PostgreSQL (Supabase)
- **Payment**: PayStack (Ghana)
- **SMS**: Mnotify API
- **Email**: SMTP (Mailgun/Brevo)
- **Storage**: Supabase Storage
- **Authentication**: JWT Bearer Tokens

## Admin Responsibilities

### 1. Daily Operations

#### Morning Routine (9:00 AM)
1. **Check Dashboard**: Review overnight bookings and system status
2. **Review Pending Bookings**: Process new customer requests
3. **Monitor Payment Status**: Verify any payment issues from overnight
4. **Check Notifications**: Ensure SMS/email systems are working

#### Throughout the Day
- **Respond to New Bookings**: Within 2-4 hours during business hours
- **Process Urgent Bookings**: Emergency requests within 1 hour
- **Update Booking Status**: Keep customers informed of progress
- **Handle Customer Inquiries**: Support calls and emails

#### End of Day (6:00 PM)
- **Complete Quote Preparations**: Prepare quotes for next day approval
- **Update Booking Notes**: Document day's activities
- **Check Payment Reconciliation**: Ensure all payments are processed

### 2. Booking Management Workflow

#### Step 1: New Booking Review
```
Customer Submits → Pending Status → Admin Reviews → Under Review Status
```

**Actions Required:**
- Review booking details and requirements
- Assess feasibility and special requests
- Research pricing from suppliers
- Update status to "Under Review"
- Add admin notes documenting initial assessment

#### Step 2: Quote Preparation
```
Under Review → Research & Pricing → Quote Ready Status
```

**Actions Required:**
- Get quotes from suppliers (flights, hotels, tours)
- Calculate total pricing including markup
- Set estimated price in system
- Update status to "Quote Ready"
- Add detailed notes about inclusions/exclusions

#### Step 3: Customer Communication
```
Quote Ready → Customer Contact → Quote Accepted/Rejected
```

**Actions Required:**
- Contact customer with quote details
- Explain inclusions, exclusions, and terms
- Handle customer questions and modifications
- Update status based on customer response

#### Step 4: Payment Processing
```
Quote Accepted → Set Final Price → Generate Payment Link → Payment Pending
```

**Actions Required:**
- Set final price in system
- Generate PayStack payment link
- Send payment link to customer
- Monitor payment status

#### Step 5: Booking Fulfillment
```
Payment Confirmed → Processing → Confirmed → Completed
```

**Actions Required:**
- Book flights, hotels, tours with suppliers
- Upload relevant documents/vouchers
- Send confirmation details to customer
- Update status as work progresses

## Admin API Endpoints Guide

### Authentication
```http
POST /api/admin/login
Content-Type: application/json

{
  "email": "admin@globalhorizonstravel.com",
  "password": "your-password"
}
```

### Booking Management

#### Get All Bookings
```http
GET /api/admin/bookings?status=1&page=1&pageSize=20
Authorization: Bearer {admin-token}
```

**Filters Available:**
- `status`: Filter by booking status (1=Pending, 2=Under Review, etc.)
- `serviceType`: Filter by service (1=Flight, 2=Hotel, 3=Tour, 4=Visa, 5=Package)
- `page`: Page number for pagination
- `pageSize`: Number of results per page

#### Get Specific Booking Details
```http
GET /api/admin/bookings/{id}
Authorization: Bearer {admin-token}
```

**Response includes:**
- Complete customer information
- Booking details and requirements
- Status history timeline
- Admin notes
- Pricing information

#### Update Booking Status
```http
PUT /api/admin/bookings/{id}/status
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "newStatus": 2,
  "notes": "Reviewing available options",
  "adminNotes": "Customer prefers morning flights",
  "estimatedPrice": 1200.00
}
```

#### Update Pricing
```http
PUT /api/admin/bookings/{id}/pricing
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "estimatedPrice": 1200.00,
  "finalPrice": 1150.00,
  "currency": "USD",
  "notes": "Early booking discount applied"
}
```

#### Add Admin Notes
```http
POST /api/admin/bookings/{id}/notes
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "note": "Customer confirmed travel dates via phone call"
}
```

#### Generate Payment Link
```http
POST /api/admin/bookings/{id}/payment-link
Authorization: Bearer {admin-token}
```

**Prerequisites:**
- Booking status must be "Quote Accepted" (4)
- Final price must be set and greater than 0
- Customer must have confirmed the quote

### Dashboard
```http
GET /api/admin/dashboard
Authorization: Bearer {admin-token}
```

**Provides:**
- Total bookings count
- Pending bookings requiring attention
- Completed bookings
- Total users
- Recent bookings list

## Booking Status Reference

| Status ID | Status Name | Description | Admin Actions Required |
|-----------|-------------|-------------|----------------------|
| 1 | Pending | New booking submitted | Review and move to Under Review |
| 2 | Under Review | Admin is researching | Prepare quote and set pricing |
| 3 | Quote Ready | Quote prepared for customer | Contact customer with quote |
| 4 | Quote Accepted | Customer accepted quote | Set final price and generate payment link |
| 5 | Payment Pending | Awaiting customer payment | Monitor payment status |
| 6 | Processing | Payment received, fulfilling booking | Book with suppliers, arrange services |
| 7 | Confirmed | Booking confirmed with suppliers | Send final details to customer |
| 8 | Completed | Travel completed | Archive booking |
| 9 | Cancelled | Booking cancelled | Handle refunds if applicable |
| 10 | Rejected | Quote rejected by customer | Archive or modify quote |

## Service Types Reference

| Service ID | Service Name | Typical Processing Time | Key Considerations |
|------------|--------------|------------------------|-------------------|
| 1 | Flight | 2-4 hours | Check visa requirements, seat preferences |
| 2 | Hotel | 1-2 hours | Room type, location preferences, amenities |
| 3 | Tour | 4-8 hours | Group size, physical requirements, dates |
| 4 | Visa | 1-2 days | Document requirements, processing time |
| 5 | Complete Package | 1-2 days | Complex coordination of multiple services |

## Urgency Levels

| Level | Name | Response Time | Actions |
|-------|------|---------------|---------|
| 1 | Standard | 4-8 hours | Normal processing |
| 2 | Urgent | 2-4 hours | Prioritize in queue |
| 3 | Emergency | 1 hour | Immediate attention, call customer |

## Payment Management

### Payment Workflow
1. **Admin generates payment link** → System creates PayStack payment URL
2. **Customer pays** → PayStack sends webhook to system
3. **System processes payment** → Booking status updated to Processing
4. **Notifications sent** → Customer and admin receive confirmations

### Manual Payment Verification
If webhooks fail or payments need manual verification:

```http
POST /api/payment/verify/{reference-number}
Authorization: Bearer {admin-token}
```

### Payment Status Monitoring
- Check PayStack dashboard for payment issues
- Monitor webhook delivery in system logs
- Verify payment notifications are being sent

## Customer Communication

### Automated Notifications
The system automatically sends:
- **Booking confirmation** when submitted
- **Status update notifications** when status changes
- **Payment confirmation** when payment received
- **Final booking details** when confirmed

### Manual Communication Points
- **Quote presentation**: Call/email customer with detailed quote
- **Payment reminders**: If payment is delayed
- **Travel document requests**: For visa applications
- **Final travel briefing**: Before departure

## Error Handling & Troubleshooting

### Common Issues

#### Payment Not Reflecting
1. Check PayStack dashboard for payment status
2. Use manual payment verification endpoint
3. Check webhook delivery logs
4. Verify customer used correct reference number

#### Customer Not Receiving Notifications
1. Check customer's email and phone number in system
2. Verify SMS service (Mnotify) is working
3. Check email service (SMTP) configuration
4. Review notification logs for errors

#### Booking Status Not Updating
1. Check admin permissions and JWT token validity
2. Verify booking ID exists in system
3. Check status transition rules
4. Review error logs for detailed information

#### Image Upload Failures
1. Verify file size is under 5MB
2. Check file format is supported (.jpg, .jpeg, .png, .gif, .webp, .bmp)
3. Ensure Supabase storage is configured correctly
4. Check user authentication

### Emergency Contacts
- **System Developer**: [Your contact information]
- **PayStack Support**: support@paystack.com
- **Mnotify Support**: [SMS service support]
- **Email Service Support**: [SMTP provider support]

## Security & Best Practices

### Admin Account Security
- Use strong passwords (minimum 12 characters)
- Never share admin credentials
- Log out when leaving workstation
- Regularly change passwords

### Data Protection
- Never store sensitive customer data in notes
- Use secure channels for document sharing
- Follow GDPR/data protection guidelines
- Regular backup verification

### System Monitoring
- Monitor daily transaction volumes
- Check error logs regularly
- Verify automated backups
- Monitor system performance

## Reporting & Analytics

### Daily Reports
- Total bookings received
- Bookings by status
- Revenue processed
- Customer communication summary

### Weekly Reports
- Booking completion rates
- Average processing time
- Customer satisfaction metrics
- System performance summary

### Monthly Reports
- Revenue analysis
- Customer acquisition metrics
- Operational efficiency
- System usage statistics

## Integration Management

### PayStack Configuration
- **Live Key**: Used for production payments
- **Test Key**: Used for testing (currently commented out)
- **Webhook URL**: Configured in PayStack dashboard
- **Callback URL**: Customer return URL after payment

### SMS Service (Mnotify)
- **API Key**: Configure in appsettings.json
- **Sender ID**: "GloHorizons"
- **Message Templates**: Predefined in system

### Email Service
- **SMTP Settings**: Configure in appsettings.json
- **From Address**: info@globalhorizonstravel.com
- **Templates**: HTML email templates for notifications

### Image Storage (Supabase)
- **Bucket**: travel-images
- **Folders**: profiles/, packages/, general/, documents/
- **Access**: Public URLs for customer viewing

## Backup & Recovery

### Daily Backups
- Database backup via Supabase
- Image storage backup
- Configuration backup

### Recovery Procedures
- Database restore process
- Configuration restoration
- Service restart procedures

## Performance Optimization

### System Monitoring
- API response times
- Database query performance
- Actor system health
- Storage usage

### Maintenance Windows
- Recommended: Sunday 2:00 AM - 4:00 AM GMT
- Monthly system updates
- Quarterly performance reviews

---

## Quick Reference Commands

### Start System
```bash
dotnet run --project GloHorizonApi
```

### View Logs
```bash
# Check application logs
tail -f debug.log

# Check system logs
journalctl -u glohorizon-api -f
```

### Database Backup
```bash
# Via Supabase dashboard or CLI
supabase db dump > backup-$(date +%Y%m%d).sql
```

---

## Support & Maintenance

For technical issues or system maintenance requests, contact the development team with:
1. **Issue description**
2. **Steps to reproduce**
3. **Error messages or logs**
4. **Urgency level**
5. **Business impact**

---

*This guide should be reviewed and updated quarterly to reflect system changes and operational improvements.*