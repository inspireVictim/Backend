# Интеграция Finik Acquiring API

## Обзор

Реализована полная интеграция с Finik Acquiring API согласно официальной документации из `docs/finik.kg.txt`.

## Архитектура

### Сервисы

1. **FinikSignatureService** (`YessBackend.Infrastructure/Services/FinikSignatureService.cs`)
   - Генерация канонической строки для подписи
   - RSA-SHA256 подпись с приватным ключом
   - Проверка подписи с публичным ключом Finik
   - Сортировка JSON по ключам для канонизации

2. **FinikPaymentService** (`YessBackend.Infrastructure/Services/FinikPaymentService.cs`)
   - Создание платежей через Finik API
   - Обработка 302 redirect с извлечением Location header
   - Генерация UUID для PaymentId
   - Формирование запроса согласно спецификации

### Контроллеры

1. **PaymentsController** (`YessBackend.Api/Controllers/v1/PaymentsController.cs`)
   - `POST /api/v1/payments/create` - создание платежа
   - Принимает: `FinikCreatePaymentRequestDto`
   - Возвращает: `FinikCreatePaymentResponseDto` с `paymentUrl` и `paymentId`

2. **FinikWebhookController** (`YessBackend.Api/Controllers/v1/FinikWebhookController.cs`)
   - `POST /api/v1/webhooks/finik` - обработка webhook от Finik
   - Проверка подписи входящего запроса
   - Проверка timestamp на допустимое отклонение
   - Идемпотентная обработка по `transactionId`
   - Быстрый возврат 200 OK

### Модели DTO

- `FinikCreatePaymentRequestDto` - запрос на создание платежа
- `FinikCreatePaymentResponseDto` - ответ с paymentUrl и paymentId
- `FinikPaymentRequestBody` - тело запроса к Finik API (Amount, CardType, PaymentId, RedirectUrl, Data)
- `FinikPaymentData` - данные для QR кода (accountId, merchantCategoryCode, name_en, etc.)
- `FinikWebhookDto` - модель webhook от Finik

### Конфигурация

`FinikPaymentConfig` содержит:
- `ApiKey` - API ключ от Finik
- `AccountId` - ID аккаунта
- `PrivateKeyPem` - приватный ключ в формате PEM
- `FinikPublicKeyPem` - публичный ключ Finik (опционально, иначе используется из документации)
- `Environment` - "production" или "beta"
- `ApiBaseUrl` - базовый URL API
- `WebhookUrl` - URL для webhook
- `RedirectUrl` - URL для редиректа после оплаты
- `MerchantCategoryCode` - MCC код
- `QrName` - название QR кода
- `VerifySignature` - включить/выключить проверку подписи
- `TimestampSkewMs` - допустимое отклонение timestamp (по умолчанию 5 минут)

## Алгоритм подписи

Согласно спецификации Finik:

1. Канонизация HTTP метода (lowercase)
2. Канонизация path (абсолютный путь без query)
3. Сбор и сортировка headers (Host + все x-api-*)
4. Сортировка query параметров (если есть)
5. Сортировка JSON body по ключам
6. RSA-SHA256 подпись с приватным ключом
7. Base64 кодирование подписи

## Использование

### Создание платежа

```http
POST /api/v1/payments/create
Content-Type: application/json
Authorization: Bearer <token>

{
  "amount": 100.00,
  "description": "Оплата заказа #123",
  "redirectUrl": "https://example.com/success",
  "orderId": 123,
  "startDate": 1737369000000,
  "endDate": 1737455400000
}
```

Ответ:
```json
{
  "paymentUrl": "https://qr.finik/<payment-path>",
  "paymentId": "00000000-0000-0000-0000-000000000000"
}
```

### Webhook

Finik отправляет POST запрос на `/api/v1/webhooks/finik` с телом:

```json
{
  "id": "transaction-id-15423_CREDIT",
  "transactionId": "transaction-id-241234",
  "status": "SUCCEEDED",
  "amount": 100,
  "net": 100,
  "accountId": "your-account-id",
  "fields": {...},
  "requestDate": 1737369012345,
  "transactionDate": 1737369012345,
  "transactionType": "DEBIT",
  "receiptNumber": "some-number"
}
```

## Настройка

В `appsettings.json`:

```json
{
  "FinikPayment": {
    "Enabled": true,
    "ApiKey": "YOUR_FINIK_API_KEY",
    "AccountId": "YOUR_FINIK_ACCOUNT_ID",
    "PrivateKeyPem": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----",
    "Environment": "production",
    "ApiBaseUrl": "https://api.acquiring.averspay.kg",
    "WebhookUrl": "https://yessgo.org/api/v1/webhooks/finik",
    "RedirectUrl": "https://yessgo.org/payment/success",
    "MerchantCategoryCode": "0742",
    "QrName": "Yess Payment",
    "RequestTimeoutSeconds": 30,
    "VerifySignature": true,
    "TimestampSkewMs": 300000
  }
}
```

## Важные моменты

1. **302 Redirect**: Finik API возвращает 302 с Location header. HttpClient настроен на отключение автоматического следования за redirect (`AllowAutoRedirect = false`).

2. **Подпись**: Все запросы подписываются RSA-SHA256 с приватным ключом. Webhook проверяются с публичным ключом Finik.

3. **Timestamp**: Проверяется отклонение timestamp в webhook (по умолчанию ±5 минут).

4. **Идемпотентность**: Webhook обрабатываются идемпотентно по `transactionId`.

5. **JSON сортировка**: JSON body сортируется по ключам для канонизации подписи.

## Публичные ключи Finik

Публичные ключи Finik встроены в `FinikPaymentService`:
- Production: используется по умолчанию
- Beta: используется если `Environment = "beta"`

Можно переопределить через `FinikPublicKeyPem` в конфигурации.

