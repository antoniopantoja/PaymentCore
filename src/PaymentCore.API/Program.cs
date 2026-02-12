using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using PaymentCore.API.Middleware;
using PaymentCore.Application.Interfaces;
using PaymentCore.Application.Services;
using PaymentCore.Domain.Interfaces;
using PaymentCore.Infrastructure.BackgroundServices;
using PaymentCore.Infrastructure.Persistence;
using PaymentCore.Infrastructure.Repositories;
using PaymentCore.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "PaymentCore Transaction Processor API", 
        Version = "v1",
        Description = @"API para processamento de transações financeiras com suporte a:
        
**Operações Disponíveis:**
- **credit**: Adiciona saldo à conta (depósito)
- **debit**: Remove saldo da conta (compra/débito)
- **reserve**: Reserva saldo (pré-autorização)
- **capture**: Captura uma reserva
- **reversal**: Estorna uma transação
- **transfer**: Transfere saldo entre contas

**Valores em Centavos:**
Todos os valores de 'amount' devem ser informados em centavos.
Exemplo: R$ 100,00 = 10000 centavos

**Idempotência:**
Use 'reference_id' único para garantir que a mesma transação não seja processada duas vezes."
    });
    
    // Inclui XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    // Usa descrições dos atributos DataAnnotations
    c.UseInlineDefinitionsForEnums();
    c.DescribeAllParametersInCamelCase();
    
    // Configuração JWT no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: 'Bearer {token}'"
    });
    
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

// Database - SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<InMemoryEventPublisher>();
builder.Services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<InMemoryEventPublisher>());
builder.Services.AddSingleton<IAccountLockService, AccountLockService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Background Services
builder.Services.AddHostedService<EventProcessorBackgroundService>();

// Health Checks
builder.Services.AddHealthChecks();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Testando conexão com o banco de dados...");
        await dbContext.Database.CanConnectAsync();
        logger.LogInformation("Conexão bem-sucedida! Aplicando migrações...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Banco de dados configurado com sucesso!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao conectar ou configurar o banco de dados.");
        throw;
    }
}

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
