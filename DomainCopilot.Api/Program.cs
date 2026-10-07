using DomainCopilot.Application.Documents.Interfaces;
using DomainCopilot.Application.Documents.Services;
using DomainCopilot.Infrastructure.Documents;
using DomainCopilot.Infrastructure.Documents.Chunking;
using DomainCopilot.Infrastructure.Documents.Cleaning;
using DomainCopilot.Infrastructure.Documents.Extraction;
using DomainCopilot.Infrastructure.Documents.Repositories;
using DomainCopilot.Application.Common.Interfaces;
using DomainCopilot.Application.Rag.Interfaces;
using DomainCopilot.Infrastructure.Data;
using DomainCopilot.Infrastructure.Providers;
using DomainCopilot.Infrastructure.Rag;
using Microsoft.EntityFrameworkCore;

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

// Vector Search Service
builder.Services.AddScoped<IVectorSearchService, PostgresVectorSearchService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
