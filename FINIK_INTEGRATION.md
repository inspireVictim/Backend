# Finik Payment Provider Integration

## Overview

Интеграция с платежным провайдером Finik для обработки платежей заказов в системе Yess Loyalty.

## Architecture

### Components

1. **Configuration**: `FinikPaymentConfig` - конфигурация из appsettings.json
2. **DTOs**: `FinikPaymentRequestDto`, `FinikPaymentResponseDto`, `FinikWebhookDto`
3. **Service Interface**: `IFinikService` - интерфейс для работы с Finik API
4. **Service Implementation**: `FinikService` - реализация с HttpClient
5. **Controllers**: 
   - `FinikPaymentController` - создание платежей
   - `WebhooksController` - обработка webhook от Finik

## Payment Flow

```
MAUI Frontend → Backend API (/api/v1/payment/create) 
    → Finik API → User Payment 
    → Finik Webhook (/api/v1/finik/webhook) 
    → Update Order Status
```

## Configuration

### appsettings.json

```json
{
  "FinikPayment": {
    "Enabled": true,
    "ClientId": "your_client_id",
    "ClientSecret": "your_client_secret",
    "AccountId": "your_account_id",
    "ApiBaseUrl": "https://api.finik.kg",
    "CallbackUrl": "https://yourdomain.com/api/v1/finik/webhook",
    "RequestTimeoutSeconds": 30,
    "VerifySignature": true
  }
}
```

### Environment Variables (Production)

```bash
FinikPayment__ClientSecret=your_production_secret
FinikPayment__CallbackUrl=https://yourdomain.com/api/v1/finik/webhook
```

## API Endpoints

### 1. Create Payment

**POST** `/api/v1/payment/create`

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "order_id": 123,
  "amount": 1000.00,
  "description": "Payment for order #123",
  "success_url": "https://yourapp.com/payment/success",
  "cancel_url": "https://yourapp.com/payment/cancel"
}
```

**Response:**
```json
{
  "payment_id": "finik_payment_id_123",
  "payment_url": "https://finik.kg/pay/123",
  "order_id": 123,
  "amount": 1000.00,
  "status": "pending",
  "message": "Payment created successfully"
}
```

### 2. Get Payment Status

**GET** `/api/v1/payment/{payment_id}/status`

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "payment_id": "finik_payment_id_123",
  "order_id": 123,
  "status": "success",
  "amount": 1000.00,
  "currency": "KGS",
  "created_at": "2025-01-15T10:30:00Z",
  "updated_at": "2025-01-15T10:35:00Z",
  "paid_at": "2025-01-15T10:35:00Z"
}
```

### 3. Webhook (from Finik)

**POST** `/api/v1/finik/webhook`

**Headers:**
```
X-Signature: {hmac_sha256_signature}
Content-Type: application/json
```

**Request Body:**
```json
{
  "payment_id": "finik_payment_id_123",
  "order_id": 123,
  "status": "success",
  "amount": 1000.00,
  "currency": "KGS",
  "created_at": "2025-01-15T10:30:00Z",
  "updated_at": "2025-01-15T10:35:00Z",
  "paid_at": "2025-01-15T10:35:00Z"
}
```

**Response:**
```json
{
  "status": "ok",
  "message": "Webhook processed successfully"
}
```

## Security

1. **Authentication**: All endpoints except webhook require JWT token
2. **Signature Verification**: Webhook signature is verified using HMAC-SHA256
3. **Secrets**: ClientSecret should be stored in environment variables or appsettings.Production.json (not committed to git)

## Database

The service uses `PaymentProviderTransaction` table to track all Finik payments:

- `Qid` - Payment ID from Finik
- `Provider` - "finik"
- `Status` - Payment status
- `RawRequest` / `RawResponse` - For reconciliation

## Order Status Updates

When webhook is received with status "success"/"completed"/"paid":
- Order status changes to `OrderStatus.Paid`
- Order payment_status changes to "paid"
- Transaction is created
- `PaymentProviderTransaction` is updated

## Error Handling

- All errors are logged with appropriate log levels
- Invalid requests return 400 Bad Request
- Authentication failures return 401 Unauthorized
- Server errors return 500 Internal Server Error

## Testing

1. **Development**: Use Finik sandbox/test environment
2. **Production**: Configure real ClientId, ClientSecret, AccountId
3. **Webhook Testing**: Use tools like ngrok to expose local endpoint for testing

## Notes

- The service automatically creates `PaymentProviderTransaction` records for reconciliation
- All Finik API calls use Basic Authentication (ClientId:ClientSecret as Base64)
- Webhook signature verification can be disabled in config for development/testing
- CallbackUrl must be publicly accessible for Finik to send webhooks

