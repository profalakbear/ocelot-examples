var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/whois", (HttpContext context) =>
{
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
    var username = context.Request.Headers["X-Username"].FirstOrDefault();

    return Results.Ok(new
    {
        UserId = userId,
        Username = username
    });
})
.WithName("GetWeatherForecast");


var adlar = new[]
{
    "Ali Ekber", "Namik", "Onur", "Beytullah"
};

var soyadlar = new[]
{
    "Yılmaz", "Kaya", "Demir", "Çelik"
};

app.MapGet("/calisanlar", () =>
{
    var calisanlar = Enumerable.Range(1, 5).Select(_ =>
        new LettraCalisani(
            adlar[Random.Shared.Next(adlar.Length)],
            soyadlar[Random.Shared.Next(soyadlar.Length)],
            Random.Shared.Next(20, 65)
        ))
        .ToArray();
        Console.WriteLine("calisanlar fonksiyonu calistiriliyor");
    return calisanlar;
})
.WithName("CalisanlariGetir");


app.MapGet("/sunucunumarasigetir", () =>
{
    var val = Environment.GetEnvironmentVariable("SERVER_NUMBER") ?? "0";
    Console.WriteLine("sunucunumarasigetir fonksiyon calistiriliyor");
    return Results.Text(val + " Numarali Sunucu");
})
.WithName("SunucuNumarasiniGetir");


app.Run();

record LettraCalisani(string Ad, string Soyad, int Yas);
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);