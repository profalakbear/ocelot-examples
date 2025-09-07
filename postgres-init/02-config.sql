-- PostgreSQL Auth Database SQL Initialization
-- Bu dosya 01-init-db.sh'dan sonra çalışır

-- Timezone ayarı
SET timezone = 'UTC';

-- Default schema ayarları
SET search_path TO public;

-- Performance için bazı ayarlar
ALTER SYSTEM SET shared_preload_libraries = 'pg_stat_statements';
ALTER SYSTEM SET pg_stat_statements.track = 'all';

-- İsteğe bağlı: Örnek admin kullanıcısı eklemek için
-- (Bu normalde Entity Framework tarafından yapılır ama acil durum için)
/*
-- Sadece test amaçlı, production'da kullanma!
-- Password: admin123 (BCrypt hash)
INSERT INTO "Users" ("Username", "Email", "PasswordHash", "CreatedAt", "IsActive") 
VALUES (
    'admin', 
    'admin@test.com', 
    '$2a$11$9rjG2.Qj2LzKz0YlZJ1OMuyqJgZdNWzJ4XzMPNlGDYx6qNQf0OKK6', 
    NOW(), 
    true
) ON CONFLICT (Username) DO NOTHING;
*/

-- Database hazır mesajı
DO $$
BEGIN
    RAISE NOTICE 'Auth database SQL initialization completed!';
END $$;
