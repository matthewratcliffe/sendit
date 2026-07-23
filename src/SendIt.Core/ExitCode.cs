namespace SendIt.Core;

public enum ExitCode
{
    Success = 0,
    ValidationFailed = 1,
    GitFailure = 2,
    TestsFailed = 3,
    AiUnavailable = 4,
    ConfigurationError = 5,
    UserCancelled = 6,
    PushFailed = 7,
    NothingToCommit = 8
}
