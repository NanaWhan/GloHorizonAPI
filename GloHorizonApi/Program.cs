using System.Reflection;
using System.Text;
using Akka.Actor;
using GloHorizonApi.Actors;
using GloHorizonApi.Data;
using GloHorizonApi.Extensions;
using GloHorizonApi.Services;
using GloHorizonApi.Services.Interfaces;
using GloHorizonApi.Services.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Supabase;

var builder = WebApplication.CreateBuilder(args);
var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.XML";
var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
string corsPolicyName = "GloHorizon.PolicyName";

// Configure logging first
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllers();

// Configure Supabase client
var url = "https://gkwzymyjlxmlmabjzlid.supabase.co";
var supabsekey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imdrd3p5bXlqbHhtbG1hYmp6bGlkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTM2NzUyNTksImV4cCI6MjA2OTI1MTI1OX0.H71oyLLuPeoMyLE9dm2JY-mJvlrPCEGV969GgP8svdo";

var options = new SupabaseOptions()
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true,
};

// Register Supabase client
builder.Services.AddSingleton(provider => new Supabase.Client(url, supabsekey, options));

// Configure database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnectionString")));

// Redis removed - using database-only OTP approach

// Configure HttpClient
builder.Services.AddHttpClient();

// Register application services
builder.Services.AddScoped<IPayStackPaymentService, PayStackPaymentService>();
builder.Services.AddScoped<JwtTokenGenerator>();

// Resend client setup is handled in the ResendEmailService directly

// Register notification services as singleton for actor usage
builder.Services.AddSingleton<ISmsService, MnotifySmsService>();
builder.Services.AddSingleton<IEmailService, ResendEmailService>();

// Register OTP service (Database only)
builder.Services.AddScoped<IOtpService, DatabaseOtpService>();

// Register image upload service (using Supabase Storage)
builder.Services.AddScoped<IImageUploadService, SupabaseImageUploadService>();

// Register background services
// Temporarily disabled until database schema is updated
// builder.Services.AddHostedService<PaymentVerificationService>();

// Register OTP cleanup service
builder.Services.AddHostedService<OtpCleanupService>();

// Configure Akka.NET actor system with dependency injection
builder.Services.AddSingleton<ActorSystem>(provider =>
{
    var actorSystem = ActorSystem.Create("glohorizon-actor-system");
    return actorSystem;
});

// Create dedicated instances for actors (singleton lifetime to avoid scope issues)
builder.Services.AddSingleton<ActorNotificationServices>(provider =>
{
    return new ActorNotificationServices(
        provider.GetRequiredService<ISmsService>(),
        provider.GetRequiredService<IEmailService>(),
        provider.GetRequiredService<IConfiguration>(),
        provider.GetRequiredService<ILogger<QuoteNotificationActor>>()
    );
});

// Create the notification actors once at startup
builder.Services.AddSingleton<IActorRef>(provider =>
{
    var actorSystem = provider.GetRequiredService<ActorSystem>();
    var notificationServices = provider.GetRequiredService<ActorNotificationServices>();
    
    // Create the quote notification actor
    var quoteActor = actorSystem.ActorOf(QuoteNotificationActor.Props(notificationServices), "quote-notification-actor");
    
    // Create the booking notification actor
    actorSystem.ActorOf(BookingNotificationActor.Props(
        notificationServices.SmsService,
        notificationServices.EmailService,
        notificationServices.Configuration,
        provider.GetRequiredService<ILogger<BookingNotificationActor>>()
    ), "booking-notification-actor");
    
    // Return the quote actor ref for DI (the main one being used)
    return quoteActor;
});

// Configure JWT Authentication
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true);

var key = builder.Configuration.GetValue<string>("ApiSettings:Secret");
if (string.IsNullOrEmpty(key))
    throw new InvalidOperationException("JWT Secret key is not configured in appsettings");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

// Configure CORS
builder.Services.AddCors(options =>
    options.AddPolicy(corsPolicyName, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Configure Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Global Horizons Travel API",
        Version = "v1",
        Description = "API for Global Horizons Travel booking system"
    });
});

var app = builder.Build();

// Apply database migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("🔄 Checking for pending migrations...");
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"📋 Found {pendingMigrations.Count()} pending migrations: {string.Join(", ", pendingMigrations)}");
            
            // Apply pending migrations with error handling
            logger.LogInformation("🚀 Applying database migrations...");
            try
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("✅ Database migrations applied successfully!");
            }
            catch (Exception migrationEx)
            {
                logger.LogWarning(migrationEx, "⚠️ Migration warning (continuing anyway): {Message}", migrationEx.Message);
                // Continue execution - don't fail startup due to migration conflicts
            }
        }
        else
        {
            logger.LogInformation("✅ Database is up to date - no pending migrations");
        }
        
        // Seed the database with initial data
        logger.LogInformation("🌱 Seeding admin data...");
        await DatabaseSeeder.SeedAdminAsync(dbContext);
        logger.LogInformation("✅ Admin data seeded successfully!");
        
        logger.LogInformation("🌱 Seeding travel packages...");
        await DatabaseSeeder.SeedTravelPackagesAsync(dbContext);
        logger.LogInformation("✅ Travel packages seeded successfully!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during database migration/seeding");
        logger.LogWarning("⚠️ Continuing startup despite database issues - API may have limited functionality");
        // Don't re-throw - allow startup to continue
    }
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Global Horizons Travel API v1");
    c.RoutePrefix = string.Empty; // Makes Swagger the default page
});

// Only use HTTPS redirection outside development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors(corsPolicyName);

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
