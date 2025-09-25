using Microsoft.EntityFrameworkCore;
using Treasury.Api.Data;
using Treasury.Api.Services;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//(myComment)Register EF Core DbContext (reads ConnectionStrings:Default from appsettings.json) 
builder.Services.AddDbContext<TreasuryDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

//(myCommmeny)CORS for React dev server (Vite default http://localhost:5173)
builder.Services.AddCors(o => o.AddPolicy("AllowAll", b => b.AllowAnyOrigin()
                                                            .AllowAnyHeader()
                                                            .AllowAnyMethod()));

builder.Services.AddScoped<HashService>();
builder.Services.AddScoped<AutoMatchService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
