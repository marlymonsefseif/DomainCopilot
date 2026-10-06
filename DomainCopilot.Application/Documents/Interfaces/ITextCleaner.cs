using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Application.Documents.Interfaces
{
    public interface ITextCleaner
    {
        string Clean(string text);
    }
}
