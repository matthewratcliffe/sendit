using System.Text.RegularExpressions;

namespace SendIt.Core.Validation;

public class TicketDetector
{
    private readonly IReadOnlyList<Regex> _patterns;

    public TicketDetector(IReadOnlyList<string> patterns)
        => _patterns = patterns.Select(p => new Regex(p, RegexOptions.Compiled)).ToList();

    /// <summary>Searches free text (e.g. branch name) for the first ticket reference matching any configured pattern.</summary>
    public string? Detect(string text)
    {
        foreach (var pattern in _patterns)
        {
            var match = pattern.Match(text);
            if (match.Success) return match.Value;
        }
        return null;
    }
}
