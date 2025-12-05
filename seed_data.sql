-- SQL скрипт для заполнения базы данных тестовыми данными
-- Таблицы: partners, partner_products (нижний регистр)
-- Колонки: PascalCase в кавычках

-- Вставка партнёров
INSERT INTO partners (
    "Name", "Description", "Category", "CityId", "LogoUrl", "CoverImageUrl", 
    "Phone", "Email", "Website", "MaxDiscountPercent", "CashbackRate", "DefaultCashbackRate",
    "IsActive", "IsVerified", "Latitude", "Longitude", "CreatedAt", "UpdatedAt"
)
SELECT 
    'Глобус',
    'Сеть супермаркетов с широким ассортиментом продуктов питания, бытовой химии и товаров для дома',
    'Продукты',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Бишкек' LIMIT 1),
    'https://via.placeholder.com/200x200/0F6B53/FFFFFF?text=Глобус',
    'https://via.placeholder.com/800x400/0F6B53/FFFFFF?text=Глобус+Супермаркет',
    '+996 312 123456',
    'info@globus.kg',
    'https://globus.kg',
    10.0,
    2.0,
    2.0,
    true,
    true,
    42.8746,
    74.5698,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Глобус')

UNION ALL SELECT
    'Фрунзе',
    'Крупный торговый центр в центре Бишкека с множеством магазинов, кафе и развлечений',
    'Торговый центр',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Бишкек' LIMIT 1),
    'https://via.placeholder.com/200x200/008C5D/FFFFFF?text=Фрунзе',
    'https://via.placeholder.com/800x400/008C5D/FFFFFF?text=ТЦ+Фрунзе',
    '+996 312 234567',
    'info@frunze.kg',
    'https://frunze.kg',
    15.0,
    3.0,
    3.0,
    true,
    true,
    42.8765,
    74.5800,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Фрунзе')

UNION ALL SELECT
    'Дордой Плаза',
    'Современный торговый комплекс с магазинами одежды, электроники и продуктов',
    'Торговый центр',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Бишкек' LIMIT 1),
    'https://via.placeholder.com/200x200/00C97B/FFFFFF?text=Дордой',
    'https://via.placeholder.com/800x400/00C97B/FFFFFF?text=Дордой+Плаза',
    '+996 312 345678',
    'info@dordoi.kg',
    'https://dordoi.kg',
    12.0,
    2.5,
    2.5,
    true,
    true,
    42.8800,
    74.6000,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Дордой Плаза')

UNION ALL SELECT
    'Кофейня Navat',
    'Уютная кофейня с ароматным кофе, свежей выпечкой и приятной атмосферой',
    'Кафе',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Бишкек' LIMIT 1),
    'https://via.placeholder.com/200x200/8B4513/FFFFFF?text=Navat',
    'https://via.placeholder.com/800x400/8B4513/FFFFFF?text=Кофейня+Navat',
    '+996 312 456789',
    'info@navat.kg',
    'https://navat.kg',
    20.0,
    5.0,
    5.0,
    true,
    true,
    42.8700,
    74.5700,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Кофейня Navat')

UNION ALL SELECT
    'Ресторан Faiza',
    'Ресторан национальной кухни с традиционными кыргызскими и узбекскими блюдами',
    'Ресторан',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Бишкек' LIMIT 1),
    'https://via.placeholder.com/200x200/DC143C/FFFFFF?text=Faiza',
    'https://via.placeholder.com/800x400/DC143C/FFFFFF?text=Ресторан+Faiza',
    '+996 312 567890',
    'info@faiza.kg',
    'https://faiza.kg',
    25.0,
    7.0,
    7.0,
    true,
    true,
    42.8750,
    74.5750,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Ресторан Faiza')

UNION ALL SELECT
    'Спорткомплекс',
    'Современный фитнес-центр с тренажёрным залом, бассейном и групповыми занятиями',
    'Спорт и отдых',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Бишкек' LIMIT 1),
    'https://via.placeholder.com/200x200/FF6347/FFFFFF?text=Спорт',
    'https://via.placeholder.com/800x400/FF6347/FFFFFF?text=Спорткомплекс',
    '+996 312 678901',
    'info@sport.kg',
    'https://sport.kg',
    15.0,
    4.0,
    4.0,
    true,
    true,
    42.8800,
    74.5800,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Спорткомплекс')

UNION ALL SELECT
    'Аптека 24/7',
    'Круглосуточная аптека с широким ассортиментом лекарств и медицинских товаров',
    'Аптека',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Бишкек' LIMIT 1),
    'https://via.placeholder.com/200x200/32CD32/FFFFFF?text=Аптека',
    'https://via.placeholder.com/800x400/32CD32/FFFFFF?text=Аптека+24/7',
    '+996 312 789012',
    'info@apteka.kg',
    'https://apteka.kg',
    10.0,
    3.0,
    3.0,
    true,
    true,
    42.8650,
    74.5650,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Аптека 24/7')

UNION ALL SELECT
    'Салон красоты Elegance',
    'Премиальный салон красоты с услугами парикмахера, маникюра, педикюра и косметолога',
    'Салоны красоты',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Бишкек' LIMIT 1),
    'https://via.placeholder.com/200x200/FF69B4/FFFFFF?text=Elegance',
    'https://via.placeholder.com/800x400/FF69B4/FFFFFF?text=Салон+Elegance',
    '+996 312 890123',
    'info@elegance.kg',
    'https://elegance.kg',
    30.0,
    8.0,
    8.0,
    true,
    true,
    42.8700,
    74.5800,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Салон красоты Elegance')

UNION ALL SELECT
    'Ош Базар',
    'Крупный рынок в городе Ош с продуктами питания, одеждой и товарами народного потребления',
    'Рынок',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Ош' LIMIT 1),
    'https://via.placeholder.com/200x200/FFA500/FFFFFF?text=Ош+Базар',
    'https://via.placeholder.com/800x400/FFA500/FFFFFF?text=Ош+Базар',
    '+996 322 123456',
    'info@oshbazar.kg',
    'https://oshbazar.kg',
    5.0,
    1.5,
    1.5,
    true,
    true,
    40.5151,
    72.8083,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Ош Базар')

UNION ALL SELECT
    'Книжный магазин Кыргызстан',
    'Книжный магазин с широким выбором литературы на кыргызском, русском и английском языках',
    'Книги',
    (SELECT "Id" FROM "Cities" WHERE "Name" = 'Бишкек' LIMIT 1),
    'https://via.placeholder.com/200x200/4169E1/FFFFFF?text=Книги',
    'https://via.placeholder.com/800x400/4169E1/FFFFFF?text=Книжный+магазин',
    '+996 312 901234',
    'info@books.kg',
    'https://books.kg',
    15.0,
    3.5,
    3.5,
    true,
    true,
    42.8750,
    74.5700,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM partners WHERE "Name" = 'Книжный магазин Кыргызстан');

-- Вставка товаров для партнёров
INSERT INTO partner_products ("PartnerId", "Name", "Price", "Category", "Description", "IsAvailable", "DiscountPercent", "SortOrder", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'Хлеб белый',
    50.00,
    'food',
    'Свежий белый хлеб',
    true,
    0.0,
    0,
    NOW(),
    NOW()
FROM partners p WHERE p."Name" = 'Глобус'
ON CONFLICT DO NOTHING;

INSERT INTO partner_products ("PartnerId", "Name", "Price", "Category", "Description", "IsAvailable", "DiscountPercent", "SortOrder", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'Молоко 1л',
    80.00,
    'food',
    'Свежее молоко 1 литр',
    true,
    0.0,
    0,
    NOW(),
    NOW()
FROM partners p WHERE p."Name" = 'Глобус'
ON CONFLICT DO NOTHING;

INSERT INTO partner_products ("PartnerId", "Name", "Price", "Category", "Description", "IsAvailable", "DiscountPercent", "SortOrder", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'Капучино',
    150.00,
    'drink',
    'Ароматный капучино',
    true,
    0.0,
    0,
    NOW(),
    NOW()
FROM partners p WHERE p."Name" = 'Кофейня Navat'
ON CONFLICT DO NOTHING;

INSERT INTO partner_products ("PartnerId", "Name", "Price", "Category", "Description", "IsAvailable", "DiscountPercent", "SortOrder", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'Чизкейк',
    250.00,
    'food',
    'Нежный чизкейк',
    true,
    0.0,
    0,
    NOW(),
    NOW()
FROM partners p WHERE p."Name" = 'Кофейня Navat'
ON CONFLICT DO NOTHING;

INSERT INTO partner_products ("PartnerId", "Name", "Price", "Category", "Description", "IsAvailable", "DiscountPercent", "SortOrder", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'Плов',
    350.00,
    'food',
    'Традиционный узбекский плов',
    true,
    0.0,
    0,
    NOW(),
    NOW()
FROM partners p WHERE p."Name" = 'Ресторан Faiza'
ON CONFLICT DO NOTHING;

INSERT INTO partner_products ("PartnerId", "Name", "Price", "Category", "Description", "IsAvailable", "DiscountPercent", "SortOrder", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'Шашлык',
    450.00,
    'food',
    'Шашлык из баранины',
    true,
    0.0,
    0,
    NOW(),
    NOW()
FROM partners p WHERE p."Name" = 'Ресторан Faiza'
ON CONFLICT DO NOTHING;

-- Проверка результатов
SELECT 'Города:' as info, COUNT(*) as count FROM "Cities"
UNION ALL
SELECT 'Партнёры:', COUNT(*) FROM partners
UNION ALL
SELECT 'Товары:', COUNT(*) FROM partner_products;
