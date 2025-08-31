var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


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
    return calisanlar;
})
.WithName("CalisanlariGetir");


app.MapGet("/sunucunumarasigetir", () =>
{
    var val = Environment.GetEnvironmentVariable("SERVER_NUMBER") ?? "0";
    return Results.Text(val + " Numarali Sunucu");
})
.WithName("SunucuNumarasiniGetir");


app.Run();

record LettraCalisani(string Ad, string Soyad, int Yas);