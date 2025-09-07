#!/bin/bash
set -e

# PostgreSQL auth database initialization script
# Bu script PostgreSQL container başlatıldığında otomatik olarak çalışır

echo "Starting database initialization for testauth..."

# Database zaten environment variable ile oluşturuluyor
# Ek konfigürasyonlar burada yapılabilir

# Extension'ları ekle
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- UUID extension ekle (ihtiyaç olursa)
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    
    -- PostGIS extension (gerekirse)
    -- CREATE EXTENSION IF NOT EXISTS postgis;
    
    -- Crypto extension (gerekirse)
    CREATE EXTENSION IF NOT EXISTS pgcrypto;
    
    -- Database'in hazır olduğunu göster
    SELECT 'Database testauth initialized successfully!' as status;
EOSQL

echo "Database initialization completed successfully!"

# Entity Framework migration'ları otomatik olarak .NET uygulaması tarafından yapılacak
# Bu yüzden tablo oluşturmaya gerek yok
