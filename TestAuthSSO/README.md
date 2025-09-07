# TestAuthSSO - JWT Authentication Service

Bu proje, .NET 9 ile geliştirilmiş basit bir JWT tabanlı authentication servisidir. PostgreSQL veritabanı kullanır ve YARP gateway ile entegre çalışır.

## Özellikler

- ✅ Kullanıcı kaydı (Register)
- ✅ Kullanıcı girişi (Login)
- ✅ JWT Access Token
- ✅ JWT Refresh Token
- ✅ Refresh Token Rotation
- ✅ Token iptali (Revoke)
- ✅ Şifre değiştirme
- ✅ Şifre sıfırlama
- ✅ Kullanıcı bilgileri
- ✅ Token validasyonu
- ✅ YARP Gateway entegrasyonu
- ✅ PostgreSQL veritabanı
- ✅ Rate limiting
- ✅ Docker support

## Teknolojiler

- .NET 9
- Entity Framework Core
- PostgreSQL
- JWT Bearer Authentication
- BCrypt (Password hashing)
- YARP (Gateway)
- Docker & Docker Compose

## Kurulum

### 1. Docker Compose ile Çalıştırma

```bash
# Tüm servisleri başlat
docker-compose up -d

# Logları takip et
docker-compose logs -f
```

### 2. Manuel Kurulum

```bash
# PostgreSQL'i çalıştır
docker run -d \
  --name postgres-auth \
  -e POSTGRES_DB=testauth \
  -e POSTGRES_USER=testuser \
  -e POSTGRES_PASSWORD=testpass123 \
  -p 5432:5432 \
  postgres:15

# Auth servisini çalıştır
cd TestAuthSSO
dotnet run

# Gateway'i çalıştır
cd ../TestGateway
dotnet run
```

## PostgreSQL Konfigürasyonu

PostgreSQL tamamen docker-compose.yml içinde yapılandırılmıştır:

- **Image**: postgres:15
- **Database**: testauth
- **User**: testuser / testpass123
- **Port**: 5432
- **Init Scripts**: `postgres-init/` klasöründe
- **Volume**: Persistent data storage
- **Health Check**: Otomatik sağlık kontrolü

## API Endpoints

### Authentication Endpoints (Gateway üzerinden)

- `POST /auth/register` - Kullanıcı kaydı
- `POST /auth/login` - Kullanıcı girişi
- `POST /auth/refresh` - Token yenileme
- `GET /auth/me` - Kullanıcı bilgileri (Auth gerekli)
- `POST /auth/change-password` - Şifre değiştirme (Auth gerekli)
- `POST /auth/reset-password` - Şifre sıfırlama
- `POST /auth/revoke` - Token iptali (Auth gerekli)
- `POST /auth/revoke-all` - Tüm tokenları iptal et (Auth gerekli)
- `POST /auth/validate` - Token validasyonu
- `GET /auth/user-info` - Gateway için kullanıcı bilgileri (Auth gerekli)

### Gateway Endpoints

- `GET /health` - Gateway health check
- `GET /auth-health` - Auth service health check
- `GET /auth-status` - Authentication durumu (Auth gerekli)

### API Endpoints

- `GET /api/weatherforecast` - Normal API endpoint
- `GET /secure-api/weatherforecast` - JWT korumalı API endpoint

## Kullanım Örnekleri

### 1. Kullanıcı Kaydı

```bash
curl -X POST http://localhost:8080/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "password123"
  }'
```

### 2. Giriş Yapma

```bash
curl -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usernameOrEmail": "testuser",
    "password": "password123"
  }'
```

Response:
```json
{
  "success": true,
  "message": "Giriş başarılı",
  "data": {
    "accessToken": "eyJ0eXAiOiJKV1Q...",
    "refreshToken": "base64-encoded-token",
    "expiresAt": "2025-09-07T12:15:00Z",
    "user": {
      "id": 1,
      "username": "testuser",
      "email": "test@example.com",
      "createdAt": "2025-09-07T12:00:00Z"
    }
  }
}
```

### 3. Korumalı Endpoint'e Erişim

```bash
curl -X GET http://localhost:8080/secure-api/weatherforecast \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### 4. Token Yenileme

```bash
curl -X POST http://localhost:8080/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN"
  }'
```

## Gateway Konfigürasyonu

YARP Gateway şu şekilde yapılandırılmıştır:

- `/auth/*` - Auth servisine yönlendirme
- `/api/*` - Normal API endpoints (3 instance, round-robin)
- `/secure-api/*` - JWT korumalı API endpoints

Korumalı endpoint'lere erişim sırasında JWT token'dan kullanıcı bilgileri çıkarılır ve aşağıdaki header'lar backend servislerine gönderilir:

- `X-User-Id`: Kullanıcı ID'si
- `X-Username`: Kullanıcı adı
- `X-User-Email`: Kullanıcı email'i

## Güvenlik Özellikleri

- **Password Hashing**: BCrypt ile güçlü şifre hashleme
- **JWT Security**: HS256 algoritması ile imzalama
- **Token Rotation**: Refresh token her kullanımda yenilenir
- **Token Expiration**: Access token 15 dakika, refresh token 7 gün
- **Rate Limiting**: Auth endpoints için özel rate limiting
- **CORS**: Cross-origin istekler için yapılandırılmış

## Veritabanı Yapısı

### Users Tablosu
- `Id` (Primary Key)
- `Username` (Unique)
- `Email` (Unique)
- `PasswordHash`
- `CreatedAt`
- `LastLoginAt`
- `IsActive`

### RefreshTokens Tablosu
- `Id` (Primary Key)
- `Token` (Unique)
- `UserId` (Foreign Key)
- `ExpiresAt`
- `CreatedAt`
- `IsRevoked`
- `RevokedReason`
- `ReplacedByToken`

## Portlar

- **Gateway**: http://localhost:8080
- **Auth Service**: http://localhost:8081
- **PostgreSQL**: localhost:5432

## Geliştirme

### Test Dosyası

`TestAuthSSO.http` dosyasını kullanarak API'yi test edebilirsiniz. VS Code REST Client extension'ı gereklidir.

### Veritabanı Migration'ları

Entity Framework Code First yaklaşımı kullanılmaktadır. Uygulama başlatıldığında otomatik olarak veritabanı oluşturulur.

```bash
# Manuel migration (gerektiğinde)
cd TestAuthSSO
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Troubleshooting

### PostgreSQL Bağlantı Sorunu
```bash
# Container'ın çalıştığını kontrol et
docker ps | grep postgres

# Bağlantıyı test et
docker exec -it postgres-auth psql -U testuser -d testauth -c "SELECT 1;"
```

### JWT Token Sorunu
- Token süresinin dolmadığından emin olun
- Bearer prefix'ini kullandığınızdan emin olun
- Secret key'in tüm servislerde aynı olduğunu kontrol edin

### Rate Limiting
- Auth endpoints: 10 istek/60 saniye
- API endpoints: 5 istek/15 saniye

## Production Hazırlığı

Production ortamı için:

1. **Environment Variables**: Hassas bilgileri environment variable'lardan alın
2. **Secret Key**: Güçlü, rastgele bir secret key kullanın
3. **HTTPS**: SSL sertifikası kullanın
4. **Email Service**: Şifre sıfırlama için gerçek email servisi entegre edin
5. **Logging**: Detaylı loglama sistemi kurun
6. **Monitoring**: Health check'ler ve metrics ekleyin
7. **Database**: Production PostgreSQL instance'ı kullanın
