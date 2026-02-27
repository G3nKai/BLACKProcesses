using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using UserService.Data;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();

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
        Description = "Введите только JWT токен (без префикса Bearer)"
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
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserManagementService, UserManagementService>();

builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AuthServiceUrl"] ?? "http://localhost:5001/api/");
});

builder.Services.AddHttpClient<ICoreServiceClient, CoreServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CoreServiceUrl"] ?? "http://localhost:5000/api/");
});

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "black.auth";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "black.api";
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? builder.Configuration["Jwt:SigningKey"]
    ?? "JTD1EH4cfUatNJhTcqhiCdimMTMtK46W3XNEEORDfJl";

var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authorization = context.Request.Headers.Authorization.ToString();
                if (string.IsNullOrWhiteSpace(authorization))
                {
                    return Task.CompletedTask;
                }

                const string bearerPrefix = "Bearer ";
                var token = authorization.Trim();

                if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    token = token[bearerPrefix.Length..].Trim();
                }

                context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentException => ((int)HttpStatusCode.BadRequest, "Некорректные данные запроса"),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Ресурс не найден"),
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Доступ запрещён"),
            HttpRequestException httpEx when httpEx.StatusCode.HasValue => ((int)httpEx.StatusCode.Value, "Ошибка внешнего сервиса"),
            HttpRequestException => ((int)HttpStatusCode.BadGateway, "Внешний сервис недоступен"),
            _ => ((int)HttpStatusCode.BadRequest, "Ошибка обработки запроса")
        };

        if (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new
            {
                title,
                status = statusCode,
                detail = exception.Message,
                traceId = context.TraceIdentifier
            });
        }
    }
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
