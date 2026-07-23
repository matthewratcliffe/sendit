using SendIt.Core.Interaction;
using Spectre.Console;

namespace SendIt.Cli;

public class SpectreUserInteraction : IUserInteraction
{
    public void Info(string message) => AnsiConsole.MarkupLineInterpolated($"[grey]{message}[/]");
    public void Warning(string message) => AnsiConsole.MarkupLineInterpolated($"[yellow]:warning: {message}[/]");
    public void Error(string message) => AnsiConsole.MarkupLineInterpolated($"[red]:cross_mark: {message}[/]");
    public void Success(string message) => AnsiConsole.MarkupLineInterpolated($"[green]:check_mark: {message}[/]");

    public bool Confirm(string message, bool defaultValue = true)
        => AnsiConsole.Confirm(message, defaultValue);

    public ThreeWayChoice Choose(string message, string primaryLabel, string secondaryLabel, string cancelLabel = "Cancel")
    {
        var options = new List<string> { primaryLabel, secondaryLabel, cancelLabel };
        var selection = AnsiConsole.Prompt(new SelectionPrompt<string>().Title(message).AddChoices(options));
        if (selection == primaryLabel) return ThreeWayChoice.Primary;
        if (selection == secondaryLabel) return ThreeWayChoice.Secondary;
        return ThreeWayChoice.Cancel;
    }

    public string Prompt(string message, string? defaultValue = null)
    {
        var prompt = new TextPrompt<string>(message).AllowEmpty();
        if (!string.IsNullOrEmpty(defaultValue)) prompt.DefaultValue(defaultValue);
        return AnsiConsole.Prompt(prompt);
    }

    public string EditText(string initialText)
    {
        AnsiConsole.MarkupLine("[grey]Edit the message (single line, Enter to submit):[/]");
        return AnsiConsole.Prompt(new TextPrompt<string>(">").DefaultValue(initialText));
    }
}
