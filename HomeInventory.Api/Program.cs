using HomeInventory.Api;
using HomeInventory.Api.Endpoints;
using HomeInventory.Api.Extensions;
using HomeInventory.Application;
using HomeInventory.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "frontend";
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "The 'Default' connection string was not found. Configure it in appsettings.Development.json or user-secrets.");

// Layers (composition root): the API is the only place with concrete DI.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT bearer authentication + authorization.
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS for the frontend in development.
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Postgres connectivity health check.
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["db", "postgres"]);

// Swagger / OpenAPI with Bearer support.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token (without the 'Bearer' prefix).",
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = [],
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteResponse,
});

app.MapAuthEndpoints();
app.MapHouseholdEndpoints();

app.Run();
