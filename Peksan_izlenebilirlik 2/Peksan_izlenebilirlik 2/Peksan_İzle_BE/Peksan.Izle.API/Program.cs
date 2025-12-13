using Microsoft.EntityFrameworkCore;
using Peksan.Izle.API.Data;
using Peksan.Izle.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Entity Framework yapılandırması
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS ayarları - React uygulaması için
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",  // Create React App default
                "http://localhost:5173",  // Vite default
                "http://localhost:5174",  // Vite alternative port
                "http://localhost:3001",  // Alternative port
                "http://localhost:8080"   // Alternative port
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Background Service ekle - 10 dakikada bir veri güncelleme
builder.Services.AddHostedService<DataRefreshService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// CORS'u etkinleştir
app.UseCors("AllowReactApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
