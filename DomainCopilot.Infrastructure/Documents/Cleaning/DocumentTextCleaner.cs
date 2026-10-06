using System.Text.RegularExpressions;
using DomainCopilot.Application.Documents.Interfaces;

namespace DomainCopilot.Infrastructure.Documents.Cleaning;

public class DocumentTextCleaner : ITextCleaner
{
    public string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = text.Replace("\r\n", "\n");
        text = text.Replace("\r", "\n");

        text = Regex.Replace(
            text,
            @"[ \t]+",
            " ");

        text = Regex.Replace(
            text,
            @"\n{3,}",
            "\n\n");

        return text.Trim();
    }
}