using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Services;
using TheAdamsParadigm.Api.Services.CloudCalendarService;
using TheAdamsParadigm.Api.Filters;
using TheAdamsParadigm.Api.Services.Payments;
using TheAdamsParadigm.Api.Services.Paystack;

var builder = WebApplication.CreateBuilder(args);

// Railway terminates TLS at its edge and forwards plain HTTP to the container.
// Without this, UseHttpsRedirection thinks every request is HTTP and 307s to
// https before CORS middleware runs, which strips CORS headers from preflight
// responses and gets them blocked by the browser.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add DbContext with PostgreSQL support
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));

// Persist Data Protection keys to Postgres rather than local disk, which is wiped on
// every Railway redeploy — losing the key ring would permanently break decryption of
// anything already encrypted with it (e.g. Client.ICloudPassword).
// Without an explicit ApplicationName, the discriminator used to derive keys from the
// shared key ring defaults to the content-root path — which differs between a local
// dev machine and the Railway container, so ciphertext encrypted in one environment
// can't be decrypted in the other even though both read the same persisted key ring.
builder.Services.AddDataProtection()
    .SetApplicationName("TheAdamsParadigm.Api")
    .PersistKeysToDbContext<ApplicationDbContext>();

builder.Services.AddSingleton<ClientCredentialProtector>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition(AdminKeyOperationFilter.SchemeId, new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = RequireAdminKeyAttribute.HeaderName,
        Description = "Admin key (Admin__ApiKey) for endpoints marked as admin-only."
    });
    options.OperationFilter<AdminKeyOperationFilter>();
});

const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://glorious-enchantment-production-22d8.up.railway.app",
                "https://theadamsparadigm.com")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Register database seeding service
builder.Services.AddScoped<DatabaseSeedService>();
builder.Services.AddSingleton<KnowledgeBaseService>();
builder.Services.AddSingleton<KnowledgeChunkBuilder>();
builder.Services.AddScoped<KnowledgeSearchService>();
builder.Services.AddSingleton<ProjectDiscoveryService>();
builder.Services.AddScoped<KnowledgeChunkSeedService>();

builder.Services.Configure<ICloudSettings>(
    builder.Configuration.GetSection("ICloud"));

builder.Services.Configure<BookingSettings>(
    builder.Configuration.GetSection("Booking"));

builder.Services.Configure<YocoSettings>(
    builder.Configuration.GetSection("Yoco"));

builder.Services.AddHttpClient<YocoService>(client =>
{
    client.BaseAddress = new Uri("https://payments.yoco.com/");
});

// Paystack runs alongside Yoco. Its settings are validated on first use (not here), so
// the app — and Yoco — still start when no Paystack environment variables are set.
builder.Services.Configure<PaystackSettings>(
    builder.Configuration.GetSection("Paystack"));

builder.Services.AddHttpClient<IPaystackService, PaystackService>((sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IOptions<PaystackSettings>>().Value.BaseUrl;
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        baseUrl = PaystackSettings.DefaultBaseUrl;
    }

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});

builder.Services.AddScoped<PaystackPaymentProcessor>();

// Which provider new checkouts use (DB setting, falling back to Payments__DefaultProvider),
// and the shared admin key protecting the endpoint that switches it.
builder.Services.Configure<PaymentsSettings>(
    builder.Configuration.GetSection("Payments"));

builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("Admin"));

builder.Services.AddScoped<PaymentProviderSettingService>();
builder.Services.AddSingleton<ICheckoutProviderGateway, CheckoutProviderGateway>();

builder.Services.AddSingleton<ProcessedWebhookStore>();
builder.Services.AddHttpClient<ICloudCalendarService>();
builder.Services.AddTransient<ICloudCalendarService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(ICloudCalendarService));
    var settings = sp.GetRequiredService<IOptions<ICloudSettings>>().Value;
    var dbContext = sp.GetRequiredService<ApplicationDbContext>();
    var credentialProtector = sp.GetRequiredService<ClientCredentialProtector>();
    return new ICloudCalendarService(httpClient, settings, dbContext, credentialProtector);
});

builder.Services.Configure<ResendSettings>(
    builder.Configuration.GetSection("Resend"));

builder.Services.AddHttpClient<ResendService>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});

builder.Services.Configure<AnthropicSettings>(
    builder.Configuration.GetSection("Anthropic"));

builder.Services.AddHttpClient<ClaudeService>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
});

builder.Services.AddHttpClient<MemoryExtractionService>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
});

builder.Services.Configure<VoyageSettings>(
    builder.Configuration.GetSection("Voyage"));

builder.Services.AddHttpClient<VoyageEmbeddingService>(client =>
{
    client.BaseAddress = new Uri("https://api.voyageai.com/");
});

builder.Services.Configure<KnowledgeSearchSettings>(
    builder.Configuration.GetSection("KnowledgeSearch"));

var app = builder.Build();

// Apply migrations and seed database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //var seedService = scope.ServiceProvider.GetRequiredService<DatabaseSeedService>();
    
    try
    {
        // Apply any pending migrations
        await dbContext.Database.MigrateAsync();

        // Seed the database
        //await seedService.SeedDatabaseAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying migrations or seeding the database");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
