namespace SendIt.Core.Commits;

/// <summary>Normalises manually entered commit messages into the "[Ticket] - message" convention.</summary>
public static class CommitFormatter
{
    public static string NormalizeManual(string message, string? ticket)
    {
        var trimmed = message.Trim();
        return string.IsNullOrWhiteSpace(ticket) ? trimmed : $"[{ticket}] - {trimmed}";
    }
}
