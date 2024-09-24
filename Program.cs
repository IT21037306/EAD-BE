using EAD_BE.Config;
using AspNetCore.Identity.MongoDbCore.Models;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using DotNetEnv;
using MongoDbSettings = EAD_BE.Config.MongoDbSettings;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

// Add services to the container.
builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("MONGO_URL");
    options.DatabaseName = Environment.GetEnvironmentVariable("DB_NAME");
});

builder.Services.AddSingleton<IMongoDbSettings>(sp =>
    sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);

builder.Services.AddIdentity<MongoIdentityUser<Guid>, MongoIdentityRole<Guid>>()
    .AddMongoDbStores<MongoIdentityUser<Guid>, MongoIdentityRole<Guid>, Guid>(
        Environment.GetEnvironmentVariable("MONGO_URL"),
        Environment.GetEnvironmentVariable("DB_NAME"))
    .AddDefaultTokenProviders();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.Run();