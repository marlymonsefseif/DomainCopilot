using DomainCopilot.Application.Documents.Interfaces;
using DomainCopilot.Application.Documents.Services;
using DomainCopilot.Infrastructure.Documents;
using DomainCopilot.Infrastructure.Documents.Chunking;
using DomainCopilot.Infrastructure.Documents.Cleaning;
using DomainCopilot.Infrastructure.Documents.Extraction;
using DomainCopilot.Infrastructure.Documents.Repositories;
using DomainCopilot.Application.Agents;
using DomainCopilot.Application.Agents.Guardrails;
using DomainCopilot.Application.Common.Interfaces;
using DomainCopilot.Application.Rag.Interfaces;
using DomainCopilot.Infrastructure.Data;
using DomainCopilot.Infrastructure.Providers;
using DomainCopilot.Infrastructure.Rag;
using Microsoft.EntityFrameworkCore;
using System.Text;
using DomainCopilot.Application.Auth.Interfaces;
using DomainCopilot.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DocumentIngestionService>();

builder.Services.AddScoped<IDocumentExtractor, PdfDocumentExtractor>();
builder.Services.AddScoped<IDocumentExtractor, DocxDocumentExtractor>();

builder.Services.AddSingleton<ITextCleaner, DocumentTextCleaner>();

builder.Services.AddScoped<IDocumentRepository, PostgresDocumentRepository>();
builder.Services.AddSingleton<IDocumentChunker, DocumentChunker>();
builder.Services.AddSingleton<IDocumentHashCalculator, Sha256DocumentHashCalculator>();

// Embedding Providers & Fallback Chain (Twist T2)
builder.Services.AddHttpClient<HostedEmbeddingProvider>();
builder.Services.AddHttpClient<LocalEmbeddingProvider>();
builder.Services.AddSingleton<HostedEmbeddingProvider>();
builder.Services.AddSingleton<LocalEmbeddingProvider>();
builder.Services.AddSingleton<IEmbeddingProvider, ResilientEmbeddingProvider>();

// LLM Providers & Fallback Chain (Twist T2)
builder.Services.AddHttpClient<HostedLlmProvider>();
builder.Services.AddHttpClient<LocalLlmProvider>();
builder.Services.AddSingleton<HostedLlmProvider>();
builder.Services.AddSingleton<LocalLlmProvider>();
builder.Services.AddSingleton<ILlmProvider, ResilientLlmProvider>();

// Vector & Hybrid Search Services
builder.Services.AddScoped<IVectorSearchService, PostgresVectorSearchService>();
builder.Services.AddScoped<IHybridRetrievalService, PostgresHybridRetrievalService>();

// Multi-Agent System & Deterministic Safety Guardrail
builder.Services.AddSingleton<DeterministicSafetyGuardrail>();
builder.Services.AddScoped<SymptomMatcherAgent>();
builder.Services.AddScoped<DiagnosticPlannerAgent>();
builder.Services.AddSingleton<WorkOrderGeneratorAgent>();
builder.Services.AddScoped<MaintenanceCopilotOrchestrator>();

// Authentication & Authorization (JWT Roles: Technician vs Supervisor)
builder.Services.AddScoped<IAuthService, JwtAuthService>();

var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "ThisIsASecretKeyForDomainCopilotTaskITI2026SecureKey!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DomainCopilot";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DomainCopilotApp";

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DomainCopilot API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
