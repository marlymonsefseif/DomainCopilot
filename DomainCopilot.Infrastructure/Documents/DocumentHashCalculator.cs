using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Infrastructure.Documents
{
    public static class DocumentHashCalculator
    {
        public static string Calculate(byte[] content)
        {
            var hash = SHA256.HashData(content);

            return Convert.ToHexString(hash);
        }
    }
}
