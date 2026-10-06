using System.Collections.Concurrent;
using DomainCopilot.Application.Documents.Interfaces;
using DomainCopilot.Domain.Documents;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Infrastructure.Documents.Repositories
{
    public class InMemoryDocumentRepository : IDocumentRepository
    {
        private readonly ConcurrentDictionary<Guid, Document> _documents = new();

        public Task<Document?> GetByContentHashAsync(
            string contentHash,
            CancellationToken cancellationToken = default)
        {
            var document = _documents.Values
                .FirstOrDefault(x => x.ContentHash == contentHash);

            return Task.FromResult(document);
        }

        public Task AddAsync(
            Document document,
            CancellationToken cancellationToken = default)
        {
            _documents[document.Id] = document;

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Document document,
            CancellationToken cancellationToken = default)
        {
            _documents[document.Id] = document;

            return Task.CompletedTask;
        }

        public Task<Document?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _documents.TryGetValue(id, out var document);

            return Task.FromResult(document);
        }
    }
}
