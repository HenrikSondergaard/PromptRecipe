using PromptRecipe.Models;

namespace PromptRecipe.Services;

public class RecipeService
{
    private const string MultiSelectSeparator = "||";

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

        new(QuestionKeys.ExtraContext,
            "Anything else the agent should know? (optional)",
            QuestionType.FreeText,
            Array.Empty<string>())
    };

    private static readonly Dictionary<string, string[]> TechByArea = new()
    {
        ["Frontend / UI"]   = new[] { "React", "Next.js", "Vue", "Angular", "Svelte", "Blazor", "HTML / CSS", "TypeScript", "JavaScript", "Tailwind CSS" },
        ["Backend / API"]   = new[] { "Node.js / Express", "Python / Django / FastAPI", ".NET / C#", "Java / Spring", "Go", "Ruby on Rails", "PHP / Laravel", "Rust" },
        ["Database"]        = new[] { "PostgreSQL", "MySQL / MariaDB", "SQLite", "MongoDB", "Redis", "SQL Server", "Firebase / Firestore" },
        ["Auth / Security"] = new[] { "OAuth / OpenID", "JWT", "Auth0", "Firebase Auth", "ASP.NET Identity" },
        ["Infra / DevOps"]  = new[] { "Docker", "Kubernetes", "GitHub Actions", "Terraform", "AWS", "Azure", "GCP" },
        ["Mobile"]          = new[] { "React Native", "Flutter", "Swift / SwiftUI", "Kotlin / Android", ".NET MAUI" },
        ["Testing / QA"]    = new[] { "Jest", "Vitest", "xUnit / NUnit", "Playwright", "Cypress", "Selenium", "PyTest" },
        ["CLI / Scripts"]   = new[] { "Bash / Shell", "PowerShell", "Python scripts", "Node scripts" },
        ["Documentation"]   = new[] { "Markdown", "OpenAPI / Swagger", "JSDoc / XML docs" }
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
        ["Frontend / UI"]   = new[] { "Don't change UI design / styling", "Don't add CSS libraries", "Don't break existing components", "Don't change routing" },
        ["Backend / API"]   = new[] { "Don't change public API contracts", "Don't change auth logic", "Don't add middleware", "Don't change error handling" },
        ["Database"]        = new[] { "Don't change the schema", "Don't add migrations", "Don't change existing queries" },
        ["Auth / Security"] = new[] { "Don't change permission models", "Don't modify token handling" },
        ["Infra / DevOps"]  = new[] { "Don't change CI/CD pipelines", "Don't modify environment variables" }
    };

    public IReadOnlyList<string> GetOptionsFor(string key, Dictionary<string, string> currentAnswers)
    {
        return key switch
        {
            QuestionKeys.TechStack   => GetTechOptions(currentAnswers),
            QuestionKeys.Constraints => GetConstraintOptions(currentAnswers),
            _ => Questions.FirstOrDefault(q => q.Key == key)?.BaseOptions ?? Array.Empty<string>()
        };
    }

    public string GetRecommendedOutputFormat(Dictionary<string, string> currentAnswers)
    {
        var taskType = currentAnswers.GetValueOrDefault(QuestionKeys.TaskType, "");
        var areas    = ParseMultiSelect(currentAnswers.GetValueOrDefault(QuestionKeys.Area, ""));

        return taskType switch
        {
            "Explain code" => "Explain first, then confirm before coding",
            "Bug fix"      => "Code + brief explanation",
            "New feature" when areas.Count > 1 => "Step-by-step with explanations",
            _ => "Code + brief explanation"
        };
    }

    public string AssembleCart(Dictionary<string, string> answers)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== PROMPT RECIPE CART ===");
        sb.AppendLine();

        AppendSection(sb, "Task type",            answers.GetValueOrDefault(QuestionKeys.TaskType));
        AppendSection(sb, "Area(s)",              FormatMultiSelect(answers.GetValueOrDefault(QuestionKeys.Area)));
        AppendSection(sb, "What to do",           answers.GetValueOrDefault(QuestionKeys.WhatToDo));
        AppendSection(sb, "Tech stack",           FormatMultiSelect(answers.GetValueOrDefault(QuestionKeys.TechStack)));
        AppendSection(sb, "Relevant files",       answers.GetValueOrDefault(QuestionKeys.RelevantFiles));
        AppendSection(sb, "Constraints (do NOT)", FormatMultiSelect(answers.GetValueOrDefault(QuestionKeys.Constraints)));
        AppendSection(sb, "Output format",        answers.GetValueOrDefault(QuestionKeys.OutputFormat));
        AppendSection(sb, "Must not break",       answers.GetValueOrDefault(QuestionKeys.PreserveWhat));
        AppendSection(sb, "Extra context",        answers.GetValueOrDefault(QuestionKeys.ExtraContext));

        sb.AppendLine("==========================");
        return sb.ToString();
    }

    private static void AppendSection(System.Text.StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            sb.AppendLine($"{label}: {value}");
    }

    private IReadOnlyList<string> GetTechOptions(Dictionary<string, string> answers)
    {
        var areas = ParseMultiSelect(answers.GetValueOrDefault(QuestionKeys.Area, ""));
        var options = new List<string>();
        foreach (var area in areas)
            if (TechByArea.TryGetValue(area, out var techs))
                foreach (var tech in techs)
                    if (!options.Contains(tech)) options.Add(tech);
        return options;
    }

    private IReadOnlyList<string> GetConstraintOptions(Dictionary<string, string> answers)
    {
        var areas = ParseMultiSelect(answers.GetValueOrDefault(QuestionKeys.Area, ""));
        var options = new List<string>(GeneralConstraints);
        foreach (var area in areas)
            if (ConstraintsByArea.TryGetValue(area, out var constraints))
                foreach (var c in constraints)
                    if (!options.Contains(c)) options.Add(c);
        return options;
    }

    public static string FormatMultiSelect(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var items = ParseMultiSelect(raw);
        return items.Count == 1 ? items[0] : string.Join(", ", items);
    }

    public static List<string> ParseMultiSelect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new List<string>();
        return value.Split(MultiSelectSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();
    }

    public static string JoinMultiSelect(IEnumerable<string> values) =>
        string.Join(MultiSelectSeparator, values);
}
