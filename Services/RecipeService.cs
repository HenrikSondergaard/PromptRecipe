using PromptRecipe.Models;

namespace PromptRecipe.Services;

public class RecipeService
{
    private const string MultiSelectSeparator = "||";
    private const string OtherPrefix = "Other: ";
    private const string RunExistingTestsOption = "Run the existing test suite";
    private const string WriteNewTestsOption = "Write new tests for the change";

    public static readonly IReadOnlyList<RecipeQuestion> Questions = new List<RecipeQuestion>
    {
        new(QuestionKeys.TaskType,
            "What kind of task is this?",
            QuestionType.SingleChoice,
            new[] { "New feature", "Bug fix", "Refactor / Clean up", "Explain code", "Write tests" }),

        new(QuestionKeys.Area,
            "Which area(s) does this involve?",
            QuestionType.MultiSelectWithFreeText,
            new[]
            {
                "Frontend / UI", "Backend / API", "Database", "Auth / Security",
                "Infra / DevOps", "Testing / QA", "Documentation", "Mobile", "CLI / Scripts"
            },
            FreeTextLabel: "What exactly do you want the agent to do?",
            FreeTextKey: QuestionKeys.WhatToDo),

        new(QuestionKeys.TechStack,
            "What tech stack is involved?",
            QuestionType.MultiSelectWithOther,
            Array.Empty<string>()),

        new(QuestionKeys.RelevantFiles,
            "Which files, folders, or areas are relevant? (optional)",
            QuestionType.FreeText,
            Array.Empty<string>()),

        new(QuestionKeys.Constraints,
            "What should the agent NOT do?",
            QuestionType.MultiSelectWithOther,
            Array.Empty<string>()),

        new(QuestionKeys.DoNotTouch,
            "Which files, folders or systems must the agent NOT modify?",
            QuestionType.MultiSelectWithOther,
            Array.Empty<string>()),

        new(QuestionKeys.AcceptanceCriteria,
            "When is this task done? List the acceptance criteria.",
            QuestionType.FreeText,
            Array.Empty<string>()),

        new(QuestionKeys.OutputFormat,
            "How should the agent present its work?",
            QuestionType.SingleChoice,
            new[]
            {
                "Code only",
                "Code + brief explanation",
                "Step-by-step with explanations",
                "Explain first, then confirm before coding"
            }),

        new(QuestionKeys.PreserveWhat,
            "What existing behaviour must NOT break?",
            QuestionType.FreeText,
            Array.Empty<string>()),

        new(QuestionKeys.Verification,
            "How should the agent verify its own work?",
            QuestionType.MultiSelectWithOther,
            Array.Empty<string>()),

        new(QuestionKeys.Environment,
            "Any tool, command or environment requirements?",
            QuestionType.MultiSelectWithOther,
            new[]
            {
                "Create a feature branch first",
                "Use the project's existing lint/format commands",
                "Never install new dependencies without asking",
                "Don't touch the main branch"
            }),

        new(QuestionKeys.Milestones,
            "Long task? Which sub-goals should the agent deliver, in order?",
            QuestionType.FreeText,
            Array.Empty<string>()),

        new(QuestionKeys.StopAndAsk,
            "When should the agent pause and ask you instead of guessing?",
            QuestionType.SingleChoice,
            new[]
            {
                "If requirements are ambiguous",
                "Before any destructive operation (deletes, migrations, force-push)",
                "Before committing / pushing",
                "Never — make reasonable assumptions and document them"
            }),

        new(QuestionKeys.ExtraContext,
            "Anything else the agent should know? (optional)",
            QuestionType.FreeText,
            Array.Empty<string>())
    };

    private static readonly Dictionary<string, string[]> TechByArea = new()
    {
        ["Frontend / UI"] = new[] { "React", "Next.js", "Vue", "Angular", "Svelte", "Blazor", "HTML / CSS", "TypeScript", "JavaScript", "Tailwind CSS" },
        ["Backend / API"] = new[] { "Node.js / Express", "Python / Django / FastAPI", ".NET / C#", "Java / Spring", "Go", "Ruby on Rails", "PHP / Laravel", "Rust" },
        ["Database"] = new[] { "PostgreSQL", "MySQL / MariaDB", "SQLite", "MongoDB", "Redis", "SQL Server", "Firebase / Firestore" },
        ["Auth / Security"] = new[] { "OAuth / OpenID", "JWT", "Auth0", "Firebase Auth", "ASP.NET Identity" },
        ["Infra / DevOps"] = new[] { "Docker", "Kubernetes", "GitHub Actions", "Terraform", "AWS", "Azure", "GCP" },
        ["Mobile"] = new[] { "React Native", "Flutter", "Swift / SwiftUI", "Kotlin / Android", ".NET MAUI" },
        ["Testing / QA"] = new[] { "Jest", "Vitest", "xUnit / NUnit", "Playwright", "Cypress", "Selenium", "PyTest" },
        ["CLI / Scripts"] = new[] { "Bash / Shell", "PowerShell", "Python scripts", "Node scripts" },
        ["Documentation"] = new[] { "Markdown", "OpenAPI / Swagger", "JSDoc / XML docs" }
    };

    private static readonly string[] GeneralConstraints =
    {
        "Don't modify existing tests",
        "Don't add new packages / dependencies",
        "Don't refactor unrelated code",
        "Keep changes minimal (no scope creep)"
    };

    private static readonly Dictionary<string, string[]> ConstraintsByArea = new()
    {
        ["Frontend / UI"] = new[] { "Don't change UI design / styling", "Don't add CSS libraries", "Don't break existing components", "Don't change routing" },
        ["Backend / API"] = new[] { "Don't change public API contracts", "Don't change auth logic", "Don't add middleware", "Don't change error handling" },
        ["Database"] = new[] { "Don't change the schema", "Don't add migrations", "Don't change existing queries" },
        ["Auth / Security"] = new[] { "Don't change permission models", "Don't modify token handling" },
        ["Infra / DevOps"] = new[] { "Don't change CI/CD pipelines", "Don't modify environment variables" },
        ["Testing / QA"] = new[] { "Don't remove existing test cases", "Don't change test helpers / fixtures", "Don't add new test frameworks" },
        ["Mobile"] = new[] { "Don't change navigation structure", "Don't modify platform-specific code", "Don't add new app permissions" },
        ["CLI / Scripts"] = new[] { "Don't change argument / flag interfaces", "Don't modify existing output format", "Don't break backward compatibility" },
        ["Documentation"] = new[] { "Don't change document structure / headings", "Don't modify existing code examples", "Don't change tone or style" }
    };

    private static readonly string[] GeneralDoNotTouch =
    {
        "Files outside the task's scope",
        "CI/CD workflow files (.github/)",
        "Config / secrets / environment files",
        "Schema / migration files"
    };

    private static readonly string[] GeneralVerification =
    {
        RunExistingTestsOption,
        WriteNewTestsOption,
        "Project must build without errors/warnings",
        "Run linter / formatter"
    };

    private static readonly Dictionary<string, string[]> DoNotTouchByArea = new()
    {
        ["Frontend / UI"] = new[] { "UI style files (CSS / theme)" },
        ["Backend / API"] = new[] { "Public API surface files (controllers / DTOs)", "Auth source files" },
        ["Auth / Security"] = new[] { "Auth source files", "Token / secret files" },
        ["Testing / QA"] = new[] { "Existing test files", "Test helpers / fixtures" },
        ["Mobile"] = new[] { "Platform-specific source files", "App permissions / manifests" },
        ["CLI / Scripts"] = new[] { "Argument / flag interfaces", "Existing output format" },
        ["Documentation"] = new[] { "Document structure / headings", "Code examples in docs" }
    };

    private static readonly Dictionary<string, string[]> VerificationByArea = new()
    {
        ["Frontend / UI"] = new[] { "Manual check in the browser" },
        ["Backend / API"] = new[] { "Endpoints return expected responses" },
        ["Database"] = new[] { "Migrations are reversible", "No data loss on existing tables" },
        ["Auth / Security"] = new[] { "No secrets committed", "Login / permission flows tested" },
        ["Infra / DevOps"] = new[] { "No secrets in workflow logs", "Pipeline passes on a clean checkout" },
        ["Testing / QA"] = new[] { "All existing tests still pass" },
        ["Documentation"] = new[] { "Examples compile / are accurate" },
        ["Mobile"] = new[] { "App builds for both platforms", "Navigation flows still work" },
        ["CLI / Scripts"] = new[] { "Script exits non-zero on failure", "Works after a clean checkout" }
    };

    public IReadOnlyList<string> GetOptionsFor(string key, Dictionary<string, string> currentAnswers)
    {
        return key switch
        {
            QuestionKeys.TechStack => GetCombinedOptions(Array.Empty<string>(), TechByArea, currentAnswers),
            QuestionKeys.Constraints => GetCombinedOptions(GeneralConstraints, ConstraintsByArea, currentAnswers),
            QuestionKeys.DoNotTouch => GetCombinedOptions(GeneralDoNotTouch, DoNotTouchByArea, currentAnswers),
            QuestionKeys.Verification => GetCombinedOptions(GeneralVerification, VerificationByArea, currentAnswers),
            _ => Questions.FirstOrDefault(q => q.Key == key)?.BaseOptions ?? Array.Empty<string>()
        };
    }

    public string GetRecommendedOutputFormat(Dictionary<string, string> currentAnswers)
    {
        var taskType = currentAnswers.GetValueOrDefault(QuestionKeys.TaskType, "");
        var areas = ParseMultiSelect(currentAnswers.GetValueOrDefault(QuestionKeys.Area, ""));

        return taskType switch
        {
            "Explain code" => "Explain first, then confirm before coding",
            "Bug fix" => "Code + brief explanation",
            "New feature" when areas.Count > 1 => "Step-by-step with explanations",
            _ => "Code + brief explanation"
        };
    }

    public string AssembleCart(Dictionary<string, string> answers)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== PROMPT RECIPE CART ===");
        sb.AppendLine();

        AppendSection(sb, "Task type", answers.GetValueOrDefault(QuestionKeys.TaskType));
        AppendSection(sb, "Area(s)", FormatMultiSelect(answers.GetValueOrDefault(QuestionKeys.Area)));
        AppendSection(sb, "What to do", answers.GetValueOrDefault(QuestionKeys.WhatToDo));
        AppendSection(sb, "Tech stack", FormatMultiSelect(answers.GetValueOrDefault(QuestionKeys.TechStack)));
        AppendSection(sb, "Relevant files", answers.GetValueOrDefault(QuestionKeys.RelevantFiles));
        AppendSection(sb, "Constraints (do NOT)", FormatMultiSelect(answers.GetValueOrDefault(QuestionKeys.Constraints)));
        AppendSection(sb, "Do not touch", FormatMultiSelect(answers.GetValueOrDefault(QuestionKeys.DoNotTouch)));
        AppendSection(sb, "Acceptance criteria", answers.GetValueOrDefault(QuestionKeys.AcceptanceCriteria));
        AppendSection(sb, "Output format", answers.GetValueOrDefault(QuestionKeys.OutputFormat));
        AppendSection(sb, "Must not break", answers.GetValueOrDefault(QuestionKeys.PreserveWhat));
        AppendSection(sb, "Verification", FormatMultiSelect(answers.GetValueOrDefault(QuestionKeys.Verification)));
        AppendSection(sb, "Environment requirements", FormatMultiSelect(answers.GetValueOrDefault(QuestionKeys.Environment)));
        AppendSection(sb, "Milestones (in order)", answers.GetValueOrDefault(QuestionKeys.Milestones));
        AppendSection(sb, "Stop and ask when", answers.GetValueOrDefault(QuestionKeys.StopAndAsk));
        AppendSection(sb, "Extra context", answers.GetValueOrDefault(QuestionKeys.ExtraContext));

        sb.AppendLine();
        sb.AppendLine("--- WHAT TO CHECK IN THE OUTPUT ---");
        foreach (var item in GetDiscernmentItems(answers))
            sb.AppendLine($"• {item}");

        sb.AppendLine("==========================");
        return sb.ToString();
    }

    public IReadOnlyList<string> GetDiscernmentItems(Dictionary<string, string> answers)
    {
        var items = new List<string>();
        var taskType = answers.GetValueOrDefault(QuestionKeys.TaskType, "");
        var areas = ParseMultiSelect(answers.GetValueOrDefault(QuestionKeys.Area, ""));
        var constraints = ParseMultiSelect(answers.GetValueOrDefault(QuestionKeys.Constraints, ""));
        var preserveWhat = answers.GetValueOrDefault(QuestionKeys.PreserveWhat, "");
        var doNotTouch = ParseMultiSelect(answers.GetValueOrDefault(QuestionKeys.DoNotTouch, ""));
        var acceptanceCriteria = SplitLines(answers.GetValueOrDefault(QuestionKeys.AcceptanceCriteria, ""));
        var stopAndAsk = answers.GetValueOrDefault(QuestionKeys.StopAndAsk, "");
        var milestones = SplitLines(answers.GetValueOrDefault(QuestionKeys.Milestones, ""));

        items.Add("Verify the output matches what you asked for");

        if (taskType == "Bug fix")
            items.Add("Reproduce the bug first — confirm the fix actually resolves it");

        if (taskType == "Refactor / Clean up")
            items.Add("Behaviour must be unchanged — verify all tests still pass");

        if (areas.Contains("Auth / Security"))
            items.Add("Review every change to authentication and authorisation logic");

        if (areas.Contains("Database"))
            items.Add("Verify no unintended schema or data changes");

        if (areas.Contains("Backend / API"))
            items.Add("Confirm no public API contracts were changed");

        if (areas.Contains("Infra / DevOps"))
            items.Add("Check CI/CD and environment variable changes carefully");

        foreach (var c in constraints.Where(c => !string.IsNullOrWhiteSpace(c)))
            items.Add($"Constraint respected: \"{StripOtherPrefix(c)}\"");

        if (!string.IsNullOrWhiteSpace(preserveWhat))
            items.Add($"Verify this still works: {preserveWhat}");

        if (acceptanceCriteria.Count > 0)
            items.Add("Verify every acceptance criterion listed in the cart above");

        foreach (var choice in doNotTouch.Where(choice => !string.IsNullOrWhiteSpace(choice)))
            items.Add($"Confirm this was not modified: {StripOtherPrefix(choice)}");

        foreach (var milestone in milestones)
            items.Add($"Confirm milestone delivered (in order): {milestone}");

        if (!string.IsNullOrWhiteSpace(stopAndAsk))
            items.Add($"Agent should pause and ask when: {stopAndAsk}");

        return items;
    }

    public IReadOnlyList<string> GetDiligenceItems(
        Dictionary<string, string> recipeAnswers,
        Dictionary<string, string> delegationAnswers)
    {
        var items = new List<string>();
        var taskType = recipeAnswers.GetValueOrDefault(QuestionKeys.TaskType, "");
        var areas = ParseMultiSelect(recipeAnswers.GetValueOrDefault(QuestionKeys.Area, ""));
        var preserveWhat = recipeAnswers.GetValueOrDefault(QuestionKeys.PreserveWhat, "");
        var verification = ParseMultiSelect(recipeAnswers.GetValueOrDefault(QuestionKeys.Verification, ""));
        var environment = ParseMultiSelect(recipeAnswers.GetValueOrDefault(QuestionKeys.Environment, ""));
        var risk = delegationAnswers.GetValueOrDefault(DelegationKeys.Risk, "");
        var reviewPlan = ParseMultiSelect(delegationAnswers.GetValueOrDefault(DelegationKeys.ReviewPlan, ""));

        items.Add("Review the complete code diff before applying");

        if (reviewPlan.Contains("Run the test suite")
            || verification.Contains(RunExistingTestsOption)
            || taskType is "Bug fix" or "Refactor / Clean up" or "Write tests")
            items.Add("Run the full test suite");

        if (reviewPlan.Contains("Manual testing"))
            items.Add("Test the affected functionality manually");

        if (!string.IsNullOrWhiteSpace(preserveWhat))
            items.Add($"Verify this still works: {preserveWhat}");

        if (risk.Contains("High") || areas.Contains("Auth / Security"))
            items.Add("Get a second review — this is a high-risk change");

        if (areas.Contains("Database"))
            items.Add("Back up data before running any migrations");

        if (areas.Contains("Infra / DevOps"))
            items.Add("Verify no secrets or environment variables are exposed");

        if (reviewPlan.Contains("Get a colleague to review") && !risk.Contains("High"))
            items.Add("Share the diff with a colleague for review");

        if (reviewPlan.Contains("Haven't decided yet"))
            items.Add("⚠ Decide your validation approach before applying the changes");

        if (verification.Contains(WriteNewTestsOption))
            items.Add("Verify the new tests cover the change");

        foreach (var choice in verification.Where(choice =>
                     !string.IsNullOrWhiteSpace(choice)
                     && choice != RunExistingTestsOption
                     && choice != WriteNewTestsOption))
            items.Add($"Verify: {StripOtherPrefix(choice)}");

        foreach (var choice in environment)
        {
            if (string.IsNullOrWhiteSpace(choice)) continue;

            switch (choice)
            {
                case "Don't touch the main branch":
                    items.Add("Confirm nothing was committed or pushed to the main branch");
                    break;
                case "Create a feature branch first":
                    items.Add("Confirm work happened on a feature branch, not main");
                    break;
                case "Never install new dependencies without asking":
                    items.Add("Confirm no new dependencies were added without asking");
                    break;
                default:
                    items.Add($"Confirm: {StripOtherPrefix(choice)}");
                    break;
            }
        }

        return items;
    }

    private static void AppendSection(System.Text.StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var lines = SplitLines(value);
        if (lines.Count == 0) return;

        if (lines.Count == 1)
        {
            sb.AppendLine($"{label}: {lines[0]}");
            return;
        }

        sb.AppendLine($"{label}:");
        foreach (var line in lines)
            sb.AppendLine($"  - {line}");
    }

    private static List<string> SplitLines(string? value) =>
        SplitTrimmed(value, new[] { "\r\n", "\n", "\r" });

    private static List<string> SplitTrimmed(string? value, string[] separators)
    {
        if (string.IsNullOrWhiteSpace(value)) return new List<string>();
        return value.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();
    }

    private static IReadOnlyList<string> GetCombinedOptions(
        string[] generalOptions,
        Dictionary<string, string[]> optionsByArea,
        Dictionary<string, string> answers)
    {
        var areas = ParseMultiSelect(answers.GetValueOrDefault(QuestionKeys.Area, ""));
        var options = new List<string>(generalOptions);
        foreach (var area in areas)
            if (optionsByArea.TryGetValue(area, out var areaOptions))
                foreach (var o in areaOptions)
                    if (!options.Contains(o)) options.Add(o);
        return options;
    }

    public static string FormatMultiSelect(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var items = ParseMultiSelect(raw);
        return items.Count == 1 ? items[0] : string.Join(", ", items);
    }

    public static List<string> ParseMultiSelect(string? value) =>
        SplitTrimmed(value, new[] { MultiSelectSeparator });

    /// <summary>
    /// Removes the "Other: " prefix that RecipeForm stores on custom
    /// multi-select values, so interpolated sentences read naturally
    /// (e.g. "Confirm this was not modified: my-secret-folder").
    /// </summary>
    public static string StripOtherPrefix(string? value) =>
        value is not null && value.StartsWith(OtherPrefix, StringComparison.Ordinal)
            ? value[OtherPrefix.Length..]
            : value ?? string.Empty;

    public static string JoinMultiSelect(IEnumerable<string> values) =>
        string.Join(MultiSelectSeparator, values);
}
