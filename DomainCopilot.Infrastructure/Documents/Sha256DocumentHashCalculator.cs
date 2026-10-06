using System.Security.Cryptography;
using DomainCopilot.Application.Documents.Interfaces;

namespace DomainCopilot.Infrastructure.Documents;

public class Sha256DocumentHashCalculator : IDocumentHashCalculator
{
    public string Calculate(byte[] content)
    {
        var hash = SHA256.HashData(content);

        return Convert.ToHexString(hash);
    }
}