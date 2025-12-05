-- =====================================================
-- Скрипт для анализа структуры базы данных
-- Выполните этот скрипт чтобы увидеть текущее состояние БД
-- =====================================================

-- 1. Список всех таблиц
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY tablename;

-- 2. Количество записей в каждой таблице (кроме партнеров)
SELECT 
    'users' as table_name,
    COUNT(*) as record_count
FROM "users"
UNION ALL
SELECT 'wallets', COUNT(*) FROM "wallets"
UNION ALL
SELECT 'orders', COUNT(*) FROM "orders"
UNION ALL
SELECT 'transactions', COUNT(*) FROM "transactions"
UNION ALL
SELECT 'payment_provider_transactions', COUNT(*) FROM "PaymentProviderTransactions"
UNION ALL
SELECT 'roles', COUNT(*) FROM "Roles"
UNION ALL
SELECT 'user_roles', COUNT(*) FROM "UserRoles"
UNION ALL
SELECT 'promotions', COUNT(*) FROM "Promotions"
UNION ALL
SELECT 'achievements', COUNT(*) FROM "Achievements"
UNION ALL
SELECT 'notifications', COUNT(*) FROM "Notifications"
ORDER BY table_name;

-- 3. Пользователи (первые 10)
SELECT 
    "Id",
    "Phone",
    "Email",
    "Name",
    "IsActive",
    "PhoneVerified",
    "CreatedAt"
FROM "users"
ORDER BY "Id"
LIMIT 10;

-- 4. Кошельки пользователей
SELECT 
    w."UserId",
    u."Phone",
    u."Email",
    w."Balance",
    w."YescoinBalance",
    w."TotalEarned",
    w."TotalSpent"
FROM "wallets" w
JOIN "users" u ON w."UserId" = u."Id"
ORDER BY w."UserId"
LIMIT 10;

-- 5. Транзакции (последние 10)
SELECT 
    t."Id",
    t."UserId",
    u."Phone",
    t."Type",
    t."Amount",
    t."Status",
    t."CreatedAt"
FROM "transactions" t
JOIN "users" u ON t."UserId" = u."Id"
ORDER BY t."CreatedAt" DESC
LIMIT 10;

-- 6. Платежи от провайдеров (последние 10)
SELECT 
    "Id",
    "Provider",
    "Account",
    "Amount",
    "Status",
    "CreatedAt"
FROM "PaymentProviderTransactions"
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- 7. Партнеры (для информации, не удаляем)
SELECT 
    COUNT(*) as partners_count
FROM "partners";

-- 8. Проверка пользователей с ID 15 и 16
SELECT 
    u."Id",
    u."Phone",
    u."Email",
    u."Name",
    u."IsActive",
    w."Balance",
    w."YescoinBalance"
FROM "users" u
LEFT JOIN "wallets" w ON u."Id" = w."UserId"
WHERE u."Id" IN (15, 16);

-- 9. Структура таблицы users (столбцы)
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'users' AND table_schema = 'public'
ORDER BY ordinal_position;

-- 10. Структура таблицы wallets (столбцы)
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'wallets' AND table_schema = 'public'
ORDER BY ordinal_position;

