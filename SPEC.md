\# SendIt - Functional Specification



\*\*Version:\*\* 1.0

\*\*Target Framework:\*\* .NET 8 LTS

\*\*Application Type:\*\* Cross-platform Console Application with Terminal User Interface (TUI)

\*\*Primary Platform:\*\* Windows (PowerShell \& Command Prompt)

\*\*Secondary Platform:\*\* Linux/macOS



\---



\# 1. Purpose



SendIt is an AI-assisted Git workflow automation tool designed to standardise commits, enforce development conventions, improve commit quality, and automate repetitive developer tasks while remaining fully interactive.



It provides a single command:



```

sendit

```



which performs the entire developer workflow from validating the repository through to pushing the commit.



The application is designed to work with local LLMs, Codex, Claude Code, Ollama, OpenAI-compatible endpoints and future AI providers.



\---



\# 2. Goals



The application shall:



\* Reduce poor quality commit messages.

\* Enforce branch naming conventions.

\* Enforce ticket references.

\* Prevent accidental commits.

\* Detect common mistakes before committing.

\* Run project validation automatically.

\* Provide repository-specific configuration.

\* Be fast.

\* Never lose developer control.

\* Operate completely offline if desired.



\---



\# 3. Architecture



```

┌────────────────────┐

│ sendit.exe         │

└─────────┬──────────┘

&#x20;         │

&#x20;         ▼

&#x20;Configuration Manager

&#x20;         │

&#x20;         ▼

&#x20;Git Service

&#x20;         │

&#x20;         ▼

&#x20;Validation Pipeline

&#x20;         │

&#x20;         ▼

&#x20;AI Provider

&#x20;         │

&#x20;         ▼

&#x20;Commit Generator

&#x20;         │

&#x20;         ▼

&#x20;Push Service

```



Every subsystem must be independently testable.



\---



\# 4. Command Line Interface



\## Standard



```

sendit

```



Runs the complete workflow.



\---



\## Configure



```

sendit -configure

```



Launches the configuration interface.



\---



\## Doctor



```

sendit -doctor

```



Runs diagnostics.



\---



\## Reset



```

sendit -reset

```



Deletes user configuration.



\---



\## Skip Tests



```

sendit -skiptests

```



Skips project validation.



\---



\## Force



```

sendit -force

```



Overrides warnings.



\---



\## Version



```

sendit --version

```



Displays version.



\---



\## Help



```

sendit --help

```



Displays usage.



\---



\# 5. Startup Sequence



On launch SendIt shall:



1\. Locate Git.

2\. Verify repository.

3\. Load configuration.

4\. Load repository overrides.

5\. Detect AI provider.

6\. Detect GitHub CLI.

7\. Detect GitLab CLI.

8\. Detect installed SDKs.

9\. Load branch.

10\. Begin validation.



\---



\# 6. Configuration



Configuration exists in two locations.



\## User



```

%USERPROFILE%\\.sendit.json

```



Global defaults.



\## Repository



```

<repo>\\.sendit.json

```



Repository overrides.



Repository configuration always overrides user configuration.



\---



\# 7. Configuration UI



```

sendit -configure

```



Launches a Terminal.Gui based interface.



The interface must support keyboard navigation only.



Mouse support is optional.



The UI shall contain the following tabs.



\* General

\* AI

\* Git

\* Tests

\* Advanced



\---



\# 8. General Settings



Configurable values include:



\* Default branch prefix

\* Default ticket type

\* Automatically stage files

\* Automatically push

\* Colour theme

\* Verbose logging



\---



\# 9. AI Configuration



Supported providers:



\* OpenAI Compatible

\* Ollama

\* Claude CLI

\* Codex CLI

\* OpenCode

\* LM Studio

\* llama.cpp

\* Custom Command



Configurable properties:



Provider



Endpoint



Model



API Key



Temperature



Timeout



Maximum Tokens



Retry Count



System Prompt



Prompt Template



A Test Connection button shall verify connectivity.



\---



\# 10. Git Configuration



Allowed prefixes.



Example



```

feature/

bugfix/

fix/

hotfix/

release/

chore/

docs/

ci/

refactor/

test/

infrastructure/

```



Users may add unlimited prefixes.



\---



Ticket patterns.



Users may define unlimited regular expressions.



Example



```

HJI-\\d+



ABC-\\d+



JIRA-\\d+



DEV-\\d+



SDPR-\\d+

```



\---



\# 11. Repository Validation



The application shall verify:



Repository exists.



Current branch exists.



No merge conflict.



Working tree accessible.



Git version supported.



\---



\# 12. Branch Validation



If branch prefix invalid.



Display:



Rename branch



Continue



Cancel



Rename automatically.



Rename manually.



\---



\# 13. Ticket Detection



Ticket detection uses configurable regex.



Examples:



```

HJI-1606



ABC-123



JIRA-88



DEV-22

```



If missing.



Prompt.



```

Ticket number



Leave blank for No Ticket

```



\---



\# 14. Staging



Default:



```

git add -A

```



Future option:



Interactive staging.



\---



\# 15. Commit Analysis



Collect:



```

git status



git diff



git diff --cached



git diff --stat



git branch



git log -10

```



May optionally collect:



Repository name



Current user



Remote URL



Repository language



\---



\# 16. Large Commit Detection



Warn when exceeding configurable thresholds.



Defaults



Files



50



Changed lines



2000



Maximum file size



100MB



Maximum binary count



20



User may override.



\---



\# 17. Test Runner



Repository configuration determines tests.



Examples.



```

dotnet test

```



```

npm test

```



```

cargo test

```



```

pytest

```



Multiple commands supported.



Failure behaviour configurable.



\---



\# 18. AI Commit Generation



Prompt includes:



Branch



Ticket



Status



Diff



Recent commits



Repository metadata



Output must be:



Single Conventional Commit.



Never include ticket.



Never include markdown.



Never include explanation.



\---



\# 19. Manual Fallback



If AI unavailable.



Prompt.



```

Enter commit message

```



Normalize automatically.



```

\[Ticket] - message

```



\---



\# 20. Commit Review



Options.



Accept



Edit



Regenerate



Cancel



Edit opens embedded editor.



\---



\# 21. Commit



Execute.



```

git commit

```



Display hash.



\---



\# 22. Push



Detect upstream.



If missing.



```

git push -u origin

```



Otherwise.



```

git push

```



\---



\# 23. Existing Unpushed Commits



If commits exist before starting.



Display.



Push Existing



Continue



Cancel



\---



\# 24. Diagnostics



```

sendit -doctor

```



Checks:



Git



GitHub CLI



GitLab CLI



AI provider



Endpoint



Authentication



Repository



Ticket regex



Configuration



SDKs



Network



Displays pass/fail.



\---



\# 25. Logging



Configurable.



Levels.



Error



Warning



Info



Verbose



Debug



Log rotation supported.



\---



\# 26. Themes



Dark



Light



Auto



\---



\# 27. Security



API Keys encrypted using Windows DPAPI.



Never written to logs.



Never sent to Git.



Never stored in repositories.



Repository configs must not contain secrets.



\---



\# 28. Plugin Architecture



Future providers loaded dynamically.



```

Plugins/



Claude/



Codex/



OpenAI/



Ollama/

```



No core changes required.



\---



\# 29. Extensibility



Future commands.



```

sendit release



sendit changelog



sendit squash



sendit undo



sendit amend



sendit branch



sendit sync



sendit clean



sendit review



sendit explain

```



\---



\# 30. Exit Codes



```

0 Success



1 Validation failed



2 Git failure



3 Tests failed



4 AI unavailable



5 Configuration error



6 User cancelled



7 Push failed



8 Nothing to commit

```



\---



\# 31. Performance Requirements



\* Startup < 1 second (excluding AI requests).

\* Configuration load < 100 ms.

\* Git metadata collection < 500 ms on repositories up to 1 GB.

\* UI remains responsive during AI requests.

\* Support repositories with at least 250,000 tracked files.



\---



\# 32. Non-Functional Requirements



\* Self-contained .NET 10 executable.

\* No administrative privileges required.

\* Cross-platform where practical.

\* Offline-first design.

\* All operations cancellable.

\* AI provider abstraction to allow new providers without changes to workflow logic.

\* Comprehensive unit and integration test coverage for Git, configuration, validation, and AI provider interfaces.



