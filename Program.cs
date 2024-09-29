// EAD-BE/Program.cs
using System.Text;
using EAD_BE.Config;
using AspNetCore.Identity.MongoDbCore.Models;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using DotNetEnv;
using EAD_BE.Config.User;
using EAD_BE.Config.Vendor;
using EAD_BE.Data;
using EAD_BE.Models.User.Common;
using EAD_BE.Models.Vendor.Product;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;
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

// builder.Services.AddIdentity<MongoIdentityUser<Guid>, MongoIdentityRole<Guid>>()
//     .AddMongoDbStores<MongoIdentityUser<Guid>, MongoIdentityRole<Guid>, Guid>(
//         Environment.GetEnvironmentVariable("MONGO_URL"),
//         Environment.GetEnvironmentVariable("DB_NAME"))
//     .AddDefaultTokenProviders();

builder.Services.AddSingleton<MongoDbContextProduct>();

builder.Services.AddIdentity<CustomApplicationUser, MongoIdentityRole<Guid>>()
    .AddMongoDbStores<CustomApplicationUser, MongoIdentityRole<Guid>, Guid>(
        Environment.GetEnvironmentVariable("MONGO_URL"),
        Environment.GetEnvironmentVariable("DB_NAME"))
    .AddDefaultTokenProviders();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo() { Title = "My API", Version = "v1" });

    // Add Bearer token authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddAuthorization();

// Register RoleInitializer as a service
builder.Services.AddScoped<RoleInitializer>();

// Ensure the database is created if it does not exist and check the connection
var mongoUrl = Environment.GetEnvironmentVariable("MONGO_URL");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");

if (!string.IsNullOrEmpty(mongoUrl) && !string.IsNullOrEmpty(dbName))
{
    var connectionChecker = new MongoDbConnectionChecker(mongoUrl, dbName);
    var isConnected = await connectionChecker.CheckConnectionAsync();

    if (!isConnected)
    {
        Console.WriteLine("Failed to connect to MongoDB. Please check your connection settings.");
        return;
    }

    var client = new MongoClient(mongoUrl);
    var database = client.GetDatabase(dbName);
    // This will create the database if it does not exist
    await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
    
    // Register the CategoryInitializer service
    var categoryCollection = database.GetCollection<CategoryModel>("Categories");

    builder.Services.AddSingleton(categoryCollection);
    builder.Services.AddTransient<CategoryInitializer>();
}



var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET");

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

var app = builder.Build();



// Initialize roles and categories
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleInitializer = services.GetRequiredService<RoleInitializer>();
    await roleInitializer.InitializeRoles();
    
    // Initialize categories
    var categoryInitializer = services.GetRequiredService<CategoryInitializer>();
    await categoryInitializer.InitializeCategories();
}

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI(c =>
//     {
//         c.SwaggerEndpoint(Environment.GetEnvironmentVariable("DEV_ENV_DEFAULT_URL"), Environment.GetEnvironmentVariable("DEV_ENV_API_NAME"));
//         c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
//     });
// }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();