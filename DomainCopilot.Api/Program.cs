using DomainCopilot.Application.Documents.Interfaces;
using DomainCopilot.Application.Documents.Services;
using DomainCopilot.Infrastructure.Documents;
using DomainCopilot.Infrastructure.Documents.Chunking;
using DomainCopilot.Infrastructure.Documents.Cleaning;
using DomainCopilot.Infrastructure.Documents.Extraction;
using DomainCopilot.Infrastructure.Documents.Repositories;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddScoped<DocumentIngestionService>();

builder.Services.AddScoped<IDocumentExtractor, PdfDocumentExtractor>();
builder.Services.AddScoped<IDocumentExtractor, DocxDocumentExtractor>();

builder.Services.AddSingleton<ITextCleaner, DocumentTextCleaner>();

builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
builder.Services.AddSingleton<IDocumentChunker, DocumentChunker>();
builder.Services.AddSingleton<IDocumentHashCalculator, Sha256DocumentHashCalculator>();// Add services to the container.

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
