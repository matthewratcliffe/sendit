using SendIt.Core.Ai;
using SendIt.Core.Commits;
using SendIt.Core.Configuration;
using SendIt.Core.Git;
using SendIt.Core.Interaction;
using SendIt.Core.Testing;
using SendIt.Core.Validation;
using Serilog;

namespace SendIt.Core;

/// <summary>
/// Drives the end-to-end workflow described by SPEC.md sections 11-23: repository/branch
/// validation, ticket detection, staging, test running, AI commit generation with manual
/// fallback, commit review, commit, and push.
/// </summary>
public class WorkflowRunner
{
    private readonly GitService _git;
    private readonly SenditConfig _config;
    private readonly IUserInteraction _ui;
    private readonly ILogger _logger;
    private readonly WorkflowOptions _options;

    public WorkflowRunner(GitService git, SenditConfig config, IUserInteraction ui, ILogger logger, WorkflowOptions options)
    {
        _git = git;
        _config = config;
        _ui = ui;
        _logger = logger;
        _options = options;
    }

    public async Task<ExitCode> RunAsync(CancellationToken cancellationToken = default)
    {
        var repoValidator = new RepositoryValidator(_git);
        var issues = repoValidator.Validate();
        if (issues.Count > 0)
        {
            foreach (var issue in issues) _ui.Error(issue.Message);
            if (!_options.Force) return ExitCode.ValidationFailed;
        }

        await EnsureGitIgnoreAsync(cancellationToken);

        if (_git.HasMergeConflict() && !_options.Force)
        {
            _ui.Error("Resolve merge conflicts before continuing (use -force to override).");
            return ExitCode.ValidationFailed;
        }

        if (_config.General.AutoStageFiles)
        {
            var stageResult = _git.StageAll();
            if (!stageResult.Success)
            {
                _ui.Error($"git add failed: {stageResult.StdErr}");
                return ExitCode.GitFailure;
            }
        }

        if (!_git.HasStagedChanges())
        {
            _ui.Error("Nothing to commit.");
            return ExitCode.NothingToCommit;
        }

        var branch = _git.GetCurrentBranch();
        var branchValidator = new BranchValidator(_config.Git.AllowedBranchPrefixes);
        if (!branchValidator.IsValid(branch) && !_options.Force)
        {
            var choice = _ui.Choose(
                $"Branch '{branch}' does not match an allowed prefix.", "Rename branch", "Continue");
            if (choice == ThreeWayChoice.Cancel) return ExitCode.UserCancelled;
            if (choice == ThreeWayChoice.Primary)
            {
                var renameChoice = _ui.Choose("How would you like to rename the branch?", "Automatically", "Manually");
                string newName;
                if (renameChoice == ThreeWayChoice.Cancel) return ExitCode.UserCancelled;
                newName = renameChoice == ThreeWayChoice.Primary
                    ? branchValidator.SuggestRename(branch, _config.General.DefaultBranchPrefix)
                    : _ui.Prompt("New branch name", branchValidator.SuggestRename(branch, _config.General.DefaultBranchPrefix));

                var renameResult = _git.Run($"branch -m {newName}");
                if (!renameResult.Success)
                {
                    _ui.Error($"Failed to rename branch: {renameResult.StdErr}");
                    return ExitCode.GitFailure;
                }
                branch = newName;
                _ui.Success($"Branch renamed to '{branch}'.");
            }
        }

        var ticketDetector = new TicketDetector(_config.Git.TicketPatterns);
        var ticket = ticketDetector.Detect(branch);
        if (ticket is null)
        {
            var entered = _ui.Prompt("Ticket number (leave blank for No Ticket)", "");
            ticket = string.IsNullOrWhiteSpace(entered) ? null : entered;
        }

        if (_git.HasUnpushedCommits() && !_options.Force)
        {
            var choice = _ui.Choose("Unpushed commits already exist on this branch.", "Push existing", "Continue");
            if (choice == ThreeWayChoice.Cancel) return ExitCode.UserCancelled;
            if (choice == ThreeWayChoice.Primary)
            {
                var pushResult = _git.Push();
                if (!pushResult.Success)
                {
                    _ui.Error($"Push failed: {pushResult.StdErr}");
                    return ExitCode.PushFailed;
                }
                _ui.Success("Existing commits pushed.");
            }
        }

        var stat = _git.DiffStat();
        var largeCommitDetector = new LargeCommitDetector(_config.Git);
        var stats = ParseDiffStat(stat);
        var warnings = largeCommitDetector.Evaluate(stats);
        foreach (var warning in warnings) _ui.Warning(warning);
        if (warnings.Count > 0 && !_options.Force)
        {
            if (!_ui.Confirm("Continue despite large-commit warnings?", defaultValue: false))
                return ExitCode.UserCancelled;
        }

        if (!_options.SkipTests && _config.Tests.Commands.Count > 0)
        {
            var testRunner = new TestRunner(_config.Tests, Directory.GetCurrentDirectory());
            var results = await testRunner.RunAsync(cancellationToken);
            foreach (var result in results)
            {
                if (result.Success) _ui.Success($"PASS: {result.Command}");
                else _ui.Error($"FAIL: {result.Command}\n{result.Output}");
            }
            if (results.Any(r => !r.Success) && !_options.Force)
                return ExitCode.TestsFailed;
        }

        var context = new CommitContext(
            branch, ticket, _git.Status(), _git.Diff(), _git.DiffCached(), stat, _git.Log(10),
            _git.RepositoryName(), _git.CurrentUser(), _git.RemoteUrl());

        var commitMessage = await ResolveCommitMessageAsync(context, cancellationToken);
        if (commitMessage is null) return ExitCode.UserCancelled;

        var commitResult = _git.Commit(commitMessage);
        if (!commitResult.Success)
        {
            _ui.Error($"Commit failed: {commitResult.StdErr}");
            return ExitCode.GitFailure;
        }
        var hash = _git.Run("rev-parse --short HEAD").StdOut;
        _ui.Success($"Committed {hash}: {commitMessage}");

        if (_config.General.AutoPush)
        {
            var pushResult = _git.Push();
            if (!pushResult.Success)
            {
                _ui.Error($"Push failed: {pushResult.StdErr}");
                return ExitCode.PushFailed;
            }
            _ui.Success("Pushed to remote.");
        }

        return ExitCode.Success;
    }

    private async Task EnsureGitIgnoreAsync(CancellationToken cancellationToken)
    {
        var repositoryRoot = _git.Run("rev-parse --show-toplevel").StdOut;
        if (string.IsNullOrEmpty(repositoryRoot)) return;

        var gitIgnore = new GitIgnoreService(AiProviderFactoryOrNull(), repositoryRoot);
        if (gitIgnore.Exists()) return;
        if (!_ui.Confirm("No .gitignore found. Generate one automatically based on the detected project type?", defaultValue: true))
            return;

        string? contents;
        try
        {
            contents = await gitIgnore.GenerateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to generate .gitignore");
            contents = null;
        }

        if (contents is null)
        {
            _ui.Warning("Could not generate a .gitignore (AI provider unavailable).");
            return;
        }

        gitIgnore.Write(contents);
        _ui.Success(".gitignore created.");
    }

    private IAiProvider AiProviderFactoryOrNull()
    {
        try { return AiProviderFactory.Create(_config.Ai); }
        catch { return new NullAiProvider(); }
    }

    private async Task<string?> ResolveCommitMessageAsync(CommitContext context, CancellationToken cancellationToken)
    {
        IAiProvider? provider = null;
        try
        {
            provider = AiProviderFactory.Create(_config.Ai);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to construct AI provider");
        }

        while (true)
        {
            string candidate;

            if (provider is not null)
            {
                var generator = new CommitMessageGenerator(provider, _config.Ai);
                var result = await generator.GenerateAsync(context, cancellationToken);
                if (result.Success)
                {
                    candidate = result.Message;
                }
                else
                {
                    _ui.Warning($"AI provider unavailable ({result.Error}). Falling back to manual entry.");
                    var manual = _ui.Prompt("Enter commit message", "");
                    if (string.IsNullOrWhiteSpace(manual)) return null;
                    return CommitFormatter.NormalizeManual(manual, context.Ticket);
                }
            }
            else
            {
                var manual = _ui.Prompt("Enter commit message", "");
                if (string.IsNullOrWhiteSpace(manual)) return null;
                return CommitFormatter.NormalizeManual(manual, context.Ticket);
            }

            var choice = ReviewMenu(candidate);
            switch (choice)
            {
                case CommitReviewChoice.Accept:
                    return candidate;
                case CommitReviewChoice.Edit:
                    return _ui.EditText(candidate);
                case CommitReviewChoice.Regenerate:
                    continue;
                default:
                    return null;
            }
        }
    }

    private enum CommitReviewChoice { Accept, Edit, Regenerate, Cancel }

    private CommitReviewChoice ReviewMenu(string candidate)
    {
        _ui.Info($"Proposed commit message:\n{candidate}");
        var choice = _ui.Choose("What would you like to do?", "Accept", "Edit");
        if (choice == ThreeWayChoice.Primary) return CommitReviewChoice.Accept;
        if (choice == ThreeWayChoice.Secondary) return CommitReviewChoice.Edit;

        var second = _ui.Choose("Regenerate instead?", "Regenerate", "Cancel");
        return second == ThreeWayChoice.Primary ? CommitReviewChoice.Regenerate : CommitReviewChoice.Cancel;
    }

    private static Validation.CommitStats ParseDiffStat(string diffStat)
    {
        var lines = diffStat.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var fileLines = lines.Count(l => l.Contains('|'));
        var lastLine = lines.LastOrDefault() ?? "";
        var linesChanged = System.Text.RegularExpressions.Regex.Matches(lastLine, @"\d+")
            .Select(m => int.Parse(m.Value)).Sum();
        return new Validation.CommitStats(fileLines, linesChanged, 0, 0);
    }
}
