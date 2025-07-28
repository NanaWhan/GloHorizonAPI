# 📧 Email Provider Setup Guide

SendGrid often deactivates accounts unexpectedly. Here are reliable alternatives:

## 🥇 Option 1: Mailgun (Recommended - Most Reliable)

### Why Mailgun?

- Rarely deactivates accounts
- Excellent deliverability
- Developer-friendly
- Good free tier

### Setup Steps:

1. **Sign up:** [mailgun.com](https://mailgun.com)
2. **Add domain:** Use their sandbox or add your domain
3. **Get SMTP credentials:**
   - Go to Sending → Domain settings → SMTP credentials
   - Username: `postmaster@mg.yourdomain.com` (or sandbox domain)
   - Password: Your SMTP password

### Configuration:

```json
{
  "SmtpHost": "smtp.mailgun.org",
  "SmtpPort": 587,
  "Username": "postmaster@mg.yourdomain.com",
  "Password": "your-mailgun-smtp-password"
}
```

---

## 🥈 Option 2: Brevo (Best Free Tier)

### Why Brevo?

- 300 emails/day forever (free)
- Easy setup
- No domain verification required initially

### Setup Steps:

1. **Sign up:** [brevo.com](https://brevo.com)
2. **Go to:** SMTP & API → SMTP
3. **Generate SMTP key**

### Configuration:

```json
{
  "SmtpHost": "smtp-relay.brevo.com",
  "SmtpPort": 587,
  "Username": "your-email@gmail.com",
  "Password": "your-brevo-smtp-key"
}
```

---

## 🥉 Option 3: Amazon SES (Cheapest)

### Why Amazon SES?

- $0.10 per 1,000 emails
- AWS reliability
- Scales infinitely

### Setup Steps:

1. **AWS Account:** Create at [aws.amazon.com](https://aws.amazon.com)
2. **Go to:** SES Console → SMTP settings
3. **Create SMTP credentials**

### Configuration:

```json
{
  "SmtpHost": "email-smtp.us-east-1.amazonaws.com",
  "SmtpPort": 587,
  "Username": "your-ses-smtp-username",
  "Password": "your-ses-smtp-password"
}
```

---

## 🔧 Option 4: Gmail (Quick Development)

### Setup Steps:

1. **Enable 2FA** on Gmail
2. **App Password:** Google Account → Security → App passwords
3. **Generate password** for "Mail"

### Configuration:

```json
{
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "Username": "youremail@gmail.com",
  "Password": "your-16-digit-app-password"
}
```

---

## ⚡ Quick Test Commands

After updating your config, test with:

```bash
# Start your API
dotnet run

# Test email (replace with your email)
curl -X POST https://localhost:7138/api/auth/test-email \
  -H "Content-Type: application/json" \
  -d '"your-email@example.com"'
```

---

## 📊 Comparison

| Provider | Free Tier | Reliability | Setup Time | Cost (Paid) |
| -------- | --------- | ----------- | ---------- | ----------- |
| Mailgun  | 5K/month  | ⭐⭐⭐⭐⭐  | 5 min      | $35/month   |
| Brevo    | 300/day   | ⭐⭐⭐⭐    | 2 min      | $25/month   |
| AWS SES  | None      | ⭐⭐⭐⭐⭐  | 10 min     | $0.10/1K    |
| Gmail    | 500/day   | ⭐⭐⭐      | 1 min      | Free        |

**Recommendation:** Start with **Brevo** for development, switch to **Mailgun** for production.
