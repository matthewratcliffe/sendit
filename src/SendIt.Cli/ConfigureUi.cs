using SendIt.Core.Ai;
using SendIt.Core.Configuration;
using Terminal.Gui;

namespace SendIt.Cli;

/// <summary>Terminal.Gui based configuration screen (SPEC.md section 7): keyboard-navigable tabs
/// for General, AI, Git, Tests and Advanced settings.</summary>
public static class ConfigureUi
{
    public static void Run(ConfigManager manager, SenditConfig config)
    {
        Application.Init();
        var top = Application.Top;

        var win = new Window("SendIt Configuration (Ctrl+S save, Esc quit)")
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
        };
        top.Add(win);

        var tabView = new TabView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() - 1 };

        tabView.AddTab(new TabView.Tab("General", BuildGeneralTab(config)), true);
        tabView.AddTab(new TabView.Tab("AI", BuildAiTab(config)), false);
        tabView.AddTab(new TabView.Tab("Git", BuildGitTab(config)), false);
        tabView.AddTab(new TabView.Tab("Tests", BuildTestsTab(config)), false);
        tabView.AddTab(new TabView.Tab("Advanced", BuildAdvancedTab(config)), false);

        win.Add(tabView);

        var statusBar = new StatusBar(new StatusItem[]
        {
            new(Key.CtrlMask | Key.S, "~^S~ Save (user)", () => { manager.SaveUser(config); MessageBox.Query("SendIt", "Saved to user profile.", "OK"); }),
            new(Key.CtrlMask | Key.R, "~^R~ Save (repo)", () => { manager.SaveRepo(config); MessageBox.Query("SendIt", "Saved to repository (no secrets).", "OK"); }),
            new(Key.Esc, "~Esc~ Quit", () => Application.RequestStop()),
        });
        top.Add(statusBar);

        Application.Run();
        Application.Shutdown();
    }

    private static View BuildGeneralTab(SenditConfig config)
    {
        var view = new View { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        var y = 1;

        AddLabelAndField(view, ref y, "Default branch prefix:", config.General.DefaultBranchPrefix,
            v => config.General.DefaultBranchPrefix = v);
        AddLabelAndField(view, ref y, "Default ticket type:", config.General.DefaultTicketType,
            v => config.General.DefaultTicketType = v);
        AddCheckbox(view, ref y, "Automatically stage files", config.General.AutoStageFiles,
            v => config.General.AutoStageFiles = v);
        AddCheckbox(view, ref y, "Automatically push", config.General.AutoPush,
            v => config.General.AutoPush = v);
        AddLabelAndField(view, ref y, "Colour theme (Dark/Light/Auto):", config.General.ColourTheme,
            v => config.General.ColourTheme = v);
        AddCheckbox(view, ref y, "Verbose logging", config.General.VerboseLogging,
            v => config.General.VerboseLogging = v);

        return view;
    }

    private static View BuildAiTab(SenditConfig config)
    {
        var view = new View { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        var y = 1;

        view.Add(new Label(0, y, "Provider:"));
        var providerNames = Enum.GetNames<AiProviderKind>();
        var providerList = new RadioGroup(20, y, providerNames.Select(n => NStack.ustring.Make(n)).ToArray())
        {
            SelectedItem = Array.IndexOf(providerNames, config.Ai.Provider.ToString())
        };
        providerList.SelectedItemChanged += args =>
            config.Ai.Provider = Enum.Parse<AiProviderKind>(providerNames[args.SelectedItem]);
        view.Add(providerList);
        y += providerNames.Length + 2;

        AddLabelAndField(view, ref y, "Endpoint:", config.Ai.Endpoint, v => config.Ai.Endpoint = v);
        AddLabelAndField(view, ref y, "Model:", config.Ai.Model, v => config.Ai.Model = v);
        AddLabelAndField(view, ref y, "API Key:", config.Ai.ApiKey, v => config.Ai.ApiKey = v, secret: true);
        AddLabelAndField(view, ref y, "Temperature:", config.Ai.Temperature.ToString(),
            v => config.Ai.Temperature = double.TryParse(v, out var d) ? d : config.Ai.Temperature);
        AddLabelAndField(view, ref y, "Timeout (s):", config.Ai.TimeoutSeconds.ToString(),
            v => config.Ai.TimeoutSeconds = int.TryParse(v, out var i) ? i : config.Ai.TimeoutSeconds);
        AddLabelAndField(view, ref y, "Max tokens:", config.Ai.MaxTokens.ToString(),
            v => config.Ai.MaxTokens = int.TryParse(v, out var i) ? i : config.Ai.MaxTokens);
        AddLabelAndField(view, ref y, "Retry count:", config.Ai.RetryCount.ToString(),
            v => config.Ai.RetryCount = int.TryParse(v, out var i) ? i : config.Ai.RetryCount);
        AddLabelAndField(view, ref y, "CLI executable:", config.Ai.CommandExecutable, v => config.Ai.CommandExecutable = v);
        AddLabelAndField(view, ref y, "CLI arguments:", config.Ai.CommandArguments, v => config.Ai.CommandArguments = v);

        var testButton = new Button(20, y + 1, "Test Connection");
        var testStatusLabel = new Label(20, y + 2, "") { AutoSize = true };
        view.Add(testStatusLabel);

        testButton.Clicked += () =>
        {
            testButton.Enabled = false;
            testStatusLabel.Text = "Test in progress, please wait..";
            view.SetNeedsDisplay();

            var provider = AiProviderFactory.Create(config.Ai);
            Task.Run(async () =>
            {
                try
                {
                    var ok = await provider.TestConnectionAsync();
                    Application.MainLoop.Invoke(() =>
                    {
                        testStatusLabel.Text = "";
                        testButton.Enabled = true;
                        MessageBox.Query("Test Connection", ok ? $"{provider.Name}: connected." : $"{provider.Name}: not reachable.", "OK");
                    });
                }
                catch (Exception ex)
                {
                    Application.MainLoop.Invoke(() =>
                    {
                        testStatusLabel.Text = "";
                        testButton.Enabled = true;
                        MessageBox.ErrorQuery("Test Connection", ex.Message, "OK");
                    });
                }
            });
        };
        view.Add(testButton);

        return view;
    }

    private static View BuildGitTab(SenditConfig config)
    {
        var view = new View { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        view.Add(new Label(0, 1, "Allowed branch prefixes (comma separated):"));
        var prefixField = new TextField(string.Join(", ", config.Git.AllowedBranchPrefixes)) { X = 0, Y = 2, Width = Dim.Fill() };
        prefixField.TextChanged += _ => config.Git.AllowedBranchPrefixes =
            prefixField.Text.ToString()!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        view.Add(prefixField);

        view.Add(new Label(0, 4, "Ticket regex patterns (comma separated):"));
        var ticketField = new TextField(string.Join(", ", config.Git.TicketPatterns)) { X = 0, Y = 5, Width = Dim.Fill() };
        ticketField.TextChanged += _ => config.Git.TicketPatterns =
            ticketField.Text.ToString()!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        view.Add(ticketField);

        var y = 7;
        AddLabelAndField(view, ref y, "Large commit file threshold:", config.Git.LargeCommitFileThreshold.ToString(),
            v => config.Git.LargeCommitFileThreshold = int.TryParse(v, out var i) ? i : config.Git.LargeCommitFileThreshold);
        AddLabelAndField(view, ref y, "Large commit line threshold:", config.Git.LargeCommitLineThreshold.ToString(),
            v => config.Git.LargeCommitLineThreshold = int.TryParse(v, out var i) ? i : config.Git.LargeCommitLineThreshold);
        AddLabelAndField(view, ref y, "Max file size (MB):", (config.Git.LargeCommitMaxFileSizeBytes / (1024 * 1024)).ToString(),
            v => config.Git.LargeCommitMaxFileSizeBytes = long.TryParse(v, out var l) ? l * 1024 * 1024 : config.Git.LargeCommitMaxFileSizeBytes);
        AddLabelAndField(view, ref y, "Max binary file count:", config.Git.LargeCommitMaxBinaryCount.ToString(),
            v => config.Git.LargeCommitMaxBinaryCount = int.TryParse(v, out var i) ? i : config.Git.LargeCommitMaxBinaryCount);

        return view;
    }

    private static View BuildTestsTab(SenditConfig config)
    {
        var view = new View { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        view.Add(new Label(0, 1, "Test commands (one per line):"));
        var textView = new TextView
        {
            X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill() - 3,
            Text = string.Join("\n", config.Tests.Commands)
        };
        textView.TextChanged += () => config.Tests.Commands = textView.Text.ToString()!
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        view.Add(textView);

        var stopCheck = new CheckBox(0, 0, "Stop on failure") { Checked = config.Tests.StopOnFailure, Y = Pos.Bottom(textView) };
        stopCheck.Toggled += _ => config.Tests.StopOnFailure = stopCheck.Checked;
        view.Add(stopCheck);

        return view;
    }

    private static View BuildAdvancedTab(SenditConfig config)
    {
        var view = new View { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        var y = 1;
        AddLabelAndField(view, ref y, "Log level (Error/Warning/Info/Verbose/Debug):", config.Advanced.LogLevel,
            v => config.Advanced.LogLevel = v);
        AddLabelAndField(view, ref y, "Retained log files:", config.Advanced.LogRetainedFileCount.ToString(),
            v => config.Advanced.LogRetainedFileCount = int.TryParse(v, out var i) ? i : config.Advanced.LogRetainedFileCount);
        AddCheckbox(view, ref y, "Require ticket", config.Advanced.RequireTicket, v => config.Advanced.RequireTicket = v);
        return view;
    }

    private static void AddLabelAndField(View view, ref int y, string label, string value, Action<string> onChange, bool secret = false)
    {
        view.Add(new Label(0, y, label));
        var field = new TextField(value) { X = 40, Y = y, Width = Dim.Fill(), Secret = secret };
        field.TextChanged += _ => onChange(field.Text.ToString() ?? "");
        view.Add(field);
        y += 2;
    }

    private static void AddCheckbox(View view, ref int y, string label, bool value, Action<bool> onChange)
    {
        var checkBox = new CheckBox(0, y, label) { Checked = value };
        checkBox.Toggled += _ => onChange(checkBox.Checked);
        view.Add(checkBox);
        y += 2;
    }
}
