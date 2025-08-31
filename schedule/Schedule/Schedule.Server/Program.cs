using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using Schedule.Infra.Data.DependencyInjection;
using Schedule.Infra.Data.DependencyInjection.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// -------------------- LOG --------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/schedule-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// -------------------- MVC/JSON --------------------
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        o.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        o.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// -------------------- Swagger --------------------
// Em Docker (Production) queremos Swagger ligado por config: EnableSwagger=true
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new() { Title = "Schedule API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Cole APENAS o token (sem 'Bearer ')"
    });
    opt.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// -------------------- CORS --------------------
// Origem do front no Nginx: http://localhost (porta 80)
var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                     ?? new[] { "http://localhost" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// -------------------- Options (Configuration Binding) --------------------
builder.Services.Configure<DbOptions>(
    builder.Configuration.GetSection("ConnectionStrings"));

// ATENÇÃO: a seção é "Jwt" (logo, use variáveis de ambiente Jwt__Issuer, Jwt__Audience, etc.)
builder.Services.Configure<JWTOptions>(
    builder.Configuration.GetSection("Jwt"));

// -------------------- Infra / Auth --------------------
builder.Services.AddInfraData(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Se esta API NÃO vai servir arquivos estáticos/SPAs, você pode remover as 2 linhas abaixo.
// Elas não quebram, mas também não são necessárias quando a SPA está no Nginx.
// app.UseDefaultFiles();
// app.UseStaticFiles();

// -------------------- Swagger ligado por flag --------------------
var enableSwagger = builder.Configuration.GetValue<bool>("EnableSwagger", false);
if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -------------------- CORS sempre (para o front no Nginx) --------------------
app.UseCors("Web");

// HTTPS opcional por flag
var enableHttpsRedirect = builder.Configuration.GetValue<bool>("EnableHttpsRedirect", false);
if (enableHttpsRedirect)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Se a API não serve SPA, pode remover a linha abaixo.
// app.MapFallbackToFile("/index.html");

// aplica o schema automaticamente na subida do app
using (var scope = app.Services.CreateScope())
{
    var init = scope.ServiceProvider.GetRequiredService<ISchemaInitializer>();
    await init.EnsureCreatedAsync();
}

app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();

/// -------------------- Converters auxiliares --------------------
public sealed class DateOnlyJsonConverter : System.Text.Json.Serialization.JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";
    public override DateOnly Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        => DateOnly.ParseExact(reader.GetString()!, Format);
    public override void Write(System.Text.Json.Utf8JsonWriter writer, DateOnly value, System.Text.Json.JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format));
}

public sealed class TimeOnlyJsonConverter : System.Text.Json.Serialization.JsonConverter<TimeOnly>
{
    private const string Format = "HH:mm";
    public override TimeOnly Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        => TimeOnly.ParseExact(reader.GetString()!, Format);
    public override void Write(System.Text.Json.Utf8JsonWriter writer, TimeOnly value, System.Text.Json.JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format));
}
