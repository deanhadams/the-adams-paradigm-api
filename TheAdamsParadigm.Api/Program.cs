using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL support
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Register database seeding service
builder.Services.AddScoped<DatabaseSeedService>();

builder.Services.Configure<YocoSettings>(
    builder.Configuration.GetSection("Yoco"));

builder.Services.AddHttpClient<YocoService>(client =>
{
    client.BaseAddress = new Uri("https://payments.yoco.com/");
});

builder.Services.AddSingleton<ProcessedWebhookStore>();

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

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
