namespace SendIt.Core.Interaction;

public enum ThreeWayChoice { Primary, Secondary, Cancel }

/// <summary>Abstraction over the console so SendIt.Core stays UI-framework agnostic.</summary>
public interface IUserInteraction
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Success(string message);

    bool Confirm(string message, bool defaultValue = true);

    /// <summary>Presents up to three named options and returns which one the user picked.</summary>
    ThreeWayChoice Choose(string message, string primaryLabel, string secondaryLabel, string cancelLabel = "Cancel");

    string Prompt(string message, string? defaultValue = null);

    /// <summary>Opens an embedded multi-line editor pre-populated with the given text.</summary>
    string EditText(string initialText);
}
