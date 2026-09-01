using PromptRecipe.Models;
using PromptRecipe.Services;
using Xunit;
#pragma warning disable CA1861 // avoid constant arrays in attributes — test data is fine here

namespace PromptRecipe.Tests;

public class ParseMultiSelectTests
{
    [Fact]
    public void NullInput_ReturnsEmpty()
    {
        var result = RecipeService.ParseMultiSelect(null);
        Assert.Empty(result);
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        var result = RecipeService.ParseMultiSelect("");
        Assert.Empty(result);
    }

    [Fact]
    public void WhitespaceOnly_ReturnsEmpty()
    {
        var result = RecipeService.ParseMultiSelect("   ");
        Assert.Empty(result);
    }

    [Fact]
    public void SingleValue_ReturnsSingleElement()
    {
        var result = RecipeService.ParseMultiSelect("React");
        Assert.Equal(new[] { "React" }, result);
    }

    [Fact]
    public void TwoValues_ReturnsTwoElements()
    {
        var result = RecipeService.ParseMultiSelect("React||Vue");
        Assert.Equal(new[] { "React", "Vue" }, result);
    }

    [Fact]
    public void ValuesWithWhitespace_Trims()
    {
        var result = RecipeService.ParseMultiSelect("  React  ||  Vue  ");
        Assert.Equal(new[] { "React", "Vue" }, result);
    }

    [Fact]
    public void SinglePipeCharacter_IsNotSplit()
    {
        var result = RecipeService.ParseMultiSelect("A|B");
        Assert.Equal(new[] { "A|B" }, result);
    }
}

public class JoinMultiSelectTests
{
    [Fact]
    public void EmptySequence_ReturnsEmpty()
    {
        var result = RecipeService.JoinMultiSelect(Array.Empty<string>());
        Assert.Equal("", result);
    }

    [Fact]
    public void SingleItem_ReturnsItemOnly()
    {
        var result = RecipeService.JoinMultiSelect(new[] { "React" });
        Assert.Equal("React", result);
    }

    [Fact]
    public void MultipleItems_JoinsWithSeparator()
    {
        var result = RecipeService.JoinMultiSelect(new[] { "React", "Vue", "Angular" });
        Assert.Equal("React||Vue||Angular", result);
    }

    [Fact]
    public void RoundTrip_ParseThenJoin_PreservesValues()
    {
        var original = new[] { "React", "Vue", "Angular" };
        var joined = RecipeService.JoinMultiSelect(original);
        var parsed = RecipeService.ParseMultiSelect(joined);
        Assert.Equal(original, parsed);
    }
}

public class FormatMultiSelectTests
{
    [Fact]
    public void Null_ReturnsEmpty()
    {
        var result = RecipeService.FormatMultiSelect(null);
        Assert.Equal("", result);
    }

    [Fact]
    public void SingleValue_ReturnsValueDirectly()
    {
        var result = RecipeService.FormatMultiSelect("React");
        Assert.Equal("React", result);
    }

    [Fact]
    public void TwoValues_CommaSeparated()
    {
        var result = RecipeService.FormatMultiSelect("React||Vue");
        Assert.Equal("React, Vue", result);
    }

    [Fact]
    public void ThreeValues_AllCommaSeparated()
    {
        var result = RecipeService.FormatMultiSelect("React||Vue||Angular");
        Assert.Equal("React, Vue, Angular", result);
    }
}

public class GetRecommendedOutputFormatTests
{
    private static readonly RecipeService Svc = new();

    [Fact]
    public void ExplainCode_ReturnsExplainFirst()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.TaskType] = "Explain code"
        };
        Assert.Equal("Explain first, then confirm before coding", Svc.GetRecommendedOutputFormat(answers));
    }

    [Fact]
    public void BugFix_ReturnsCodePlusBrief()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.TaskType] = "Bug fix"
        };
        Assert.Equal("Code + brief explanation", Svc.GetRecommendedOutputFormat(answers));
    }

    [Fact]
    public void NewFeature_SingleArea_ReturnsDefault()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.TaskType] = "New feature",
            [QuestionKeys.Area] = "Frontend / UI"
        };
        Assert.Equal("Code + brief explanation", Svc.GetRecommendedOutputFormat(answers));
    }

    [Fact]
    public void NewFeature_MultipleAreas_ReturnsStepByStep()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.TaskType] = "New feature",
            [QuestionKeys.Area] = "Frontend / UI||Backend / API"
        };
        Assert.Equal("Step-by-step with explanations", Svc.GetRecommendedOutputFormat(answers));
    }

    [Fact]
    public void UnknownTaskType_ReturnsDefault()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.TaskType] = "Something else"
        };
        Assert.Equal("Code + brief explanation", Svc.GetRecommendedOutputFormat(answers));
    }
}

public class GetOptionsForTests
{
    private static readonly RecipeService Svc = new();

    [Fact]
    public void TechStack_FrontendArea_ContainsFrontendTechs()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.Area] = "Frontend / UI"
        };
        var options = Svc.GetOptionsFor(QuestionKeys.TechStack, answers);
        Assert.Contains("React", options);
        Assert.Contains("Blazor", options);
    }

    [Fact]
    public void TechStack_NoArea_ReturnsEmpty()
    {
        var options = Svc.GetOptionsFor(QuestionKeys.TechStack, new Dictionary<string, string>());
        Assert.Empty(options);
    }

    [Fact]
    public void Constraints_BackendArea_ContainsGeneralAndAreaSpecific()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.Area] = "Backend / API"
        };
        var options = Svc.GetOptionsFor(QuestionKeys.Constraints, answers);
        Assert.Contains("Don't modify existing tests", options);
        Assert.Contains("Don't change public API contracts", options);
    }

    [Fact]
    public void OtherKey_ReturnsBaseOptions()
    {
        var options = Svc.GetOptionsFor(QuestionKeys.TaskType, new Dictionary<string, string>());
        Assert.Contains("New feature", options);
        Assert.Contains("Bug fix", options);
    }
}

public class AssembleCartTests
{
    private static readonly RecipeService Svc = new();

    [Fact]
    public void AllFields_ContainsHeaderAndFooter()
    {
        var answers = MinimalAnswers();
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("=== PROMPT RECIPE CART ===", cart);
        Assert.Contains("==========================", cart);
    }

    [Fact]
    public void TaskType_IsIncludedInOutput()
    {
        var answers = MinimalAnswers();
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("Task type: New feature", cart);
    }

    [Fact]
    public void WhatToDo_IsIncluded()
    {
        var answers = MinimalAnswers();
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("What to do: Add a login page", cart);
    }

    [Fact]
    public void MissingOptionalFields_NotIncluded()
    {
        var answers = MinimalAnswers();
        var cart = Svc.AssembleCart(answers);
        Assert.DoesNotContain("Relevant files:", cart);
        Assert.DoesNotContain("Extra context:", cart);
    }

    [Fact]
    public void MultiSelectFields_FormattedWithCommas()
    {
        var answers = MinimalAnswers();
        answers[QuestionKeys.Area] = "Frontend / UI||Backend / API";
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("Area(s): Frontend / UI, Backend / API", cart);
    }

    private static Dictionary<string, string> MinimalAnswers() => new()
    {
        [QuestionKeys.TaskType] = "New feature",
        [QuestionKeys.Area] = "Frontend / UI",
        [QuestionKeys.WhatToDo] = "Add a login page",
        [QuestionKeys.OutputFormat] = "Code + brief explanation"
    };

    [Fact]
    public void AssembleCart_ContainsDiscernmentSection()
    {
        var cart = Svc.AssembleCart(MinimalAnswers());
        Assert.Contains("--- WHAT TO CHECK IN THE OUTPUT ---", cart);
        Assert.Contains("• Verify the output matches what you asked for", cart);
    }
}

public class GetDiscernmentItemsTests
{
    private static readonly RecipeService Svc = new();

    [Fact]
    public void AlwaysIncludesBaseVerification()
    {
        var items = Svc.GetDiscernmentItems(new Dictionary<string, string>());
        Assert.Contains("Verify the output matches what you asked for", items);
    }

    [Fact]
    public void BugFix_AddsReproductionCheck()
    {
        var answers = new Dictionary<string, string> { [QuestionKeys.TaskType] = "Bug fix" };
        var items = Svc.GetDiscernmentItems(answers);
        Assert.Contains(items, i => i.Contains("Reproduce the bug"));
    }

    [Fact]
    public void AuthArea_AddsSecurityCheck()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.Area] = "Auth / Security"
        };
        var items = Svc.GetDiscernmentItems(answers);
        Assert.Contains(items, i => i.Contains("authentication"));
    }

    [Fact]
    public void PreserveWhat_AddsVerificationItem()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.PreserveWhat] = "Login flow must keep working"
        };
        var items = Svc.GetDiscernmentItems(answers);
        Assert.Contains(items, i => i.Contains("Login flow must keep working"));
    }

    [Fact]
    public void NoSensitiveAreas_NoSecurityCheck()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.Area] = "Documentation"
        };
        var items = Svc.GetDiscernmentItems(answers);
        Assert.DoesNotContain(items, i => i.Contains("authentication"));
    }
}

public class GetDiligenceItemsTests
{
    private static readonly RecipeService Svc = new();

    [Fact]
    public void AlwaysIncludesDiffReview()
    {
        var items = Svc.GetDiligenceItems(new Dictionary<string, string>(), new Dictionary<string, string>());
        Assert.Contains("Review the complete code diff before applying", items);
    }

    [Fact]
    public void BugFix_AddsRunTests()
    {
        var recipe = new Dictionary<string, string> { [QuestionKeys.TaskType] = "Bug fix" };
        var items = Svc.GetDiligenceItems(recipe, new Dictionary<string, string>());
        Assert.Contains(items, i => i.Contains("test suite"));
    }

    [Fact]
    public void HighRisk_AddsSecondReview()
    {
        var delegation = new Dictionary<string, string>
        {
            [DelegationKeys.Risk] = "High — touches sensitive or production systems"
        };
        var items = Svc.GetDiligenceItems(new Dictionary<string, string>(), delegation);
        Assert.Contains(items, i => i.Contains("second review"));
    }

    [Fact]
    public void DatabaseArea_AddsBackupReminder()
    {
        var recipe = new Dictionary<string, string> { [QuestionKeys.Area] = "Database" };
        var items = Svc.GetDiligenceItems(recipe, new Dictionary<string, string>());
        Assert.Contains(items, i => i.Contains("Back up data"));
    }

    [Fact]
    public void HaventDecided_AddsWarning()
    {
        var delegation = new Dictionary<string, string>
        {
            [DelegationKeys.ReviewPlan] = "Haven't decided yet"
        };
        var items = Svc.GetDiligenceItems(new Dictionary<string, string>(), delegation);
        Assert.Contains(items, i => i.Contains("Decide your validation"));
    }
}

public class QuestionsTests
{
    [Fact]
    public void Questions_ContainsExactlyFourteenQuestions()
    {
        Assert.Equal(14, RecipeService.Questions.Count);
    }

    [Fact]
    public void Questions_AreInExpectedOrder()
    {
        var keys = RecipeService.Questions.Select(q => q.Key).ToArray();
        var expected = new[]
        {
            QuestionKeys.TaskType,
            QuestionKeys.Area,
            QuestionKeys.TechStack,
            QuestionKeys.RelevantFiles,
            QuestionKeys.Constraints,
            QuestionKeys.DoNotTouch,
            QuestionKeys.AcceptanceCriteria,
            QuestionKeys.OutputFormat,
            QuestionKeys.PreserveWhat,
            QuestionKeys.Verification,
            QuestionKeys.Environment,
            QuestionKeys.Milestones,
            QuestionKeys.StopAndAsk,
            QuestionKeys.ExtraContext
        };
        Assert.Equal(expected, keys);
    }

    [Fact]
    public void NewQuestions_HaveExpectedTypes()
    {
        var byKey = RecipeService.Questions.ToDictionary(q => q.Key);
        Assert.Equal(QuestionType.MultiSelectWithOther, byKey[QuestionKeys.DoNotTouch].Type);
        Assert.Equal(QuestionType.FreeText, byKey[QuestionKeys.AcceptanceCriteria].Type);
        Assert.Equal(QuestionType.MultiSelectWithOther, byKey[QuestionKeys.Verification].Type);
        Assert.Equal(QuestionType.MultiSelectWithOther, byKey[QuestionKeys.Environment].Type);
        Assert.Equal(QuestionType.FreeText, byKey[QuestionKeys.Milestones].Type);
        Assert.Equal(QuestionType.SingleChoice, byKey[QuestionKeys.StopAndAsk].Type);
    }
}

public class NewAgenticOptionsTests
{
    private static readonly RecipeService Svc = new();

    [Fact]
    public void DoNotTouch_NoArea_ReturnsFourGeneralOptions()
    {
        var options = Svc.GetOptionsFor(QuestionKeys.DoNotTouch, new Dictionary<string, string>());
        Assert.Equal(4, options.Count);
        Assert.Contains("Files outside the task's scope", options);
        Assert.Contains("CI/CD workflow files", options);
        Assert.Contains("Config / secrets / environment files", options);
        Assert.Contains("Database schema / migrations", options);
    }

    [Fact]
    public void DoNotTouch_BackendArea_AddsAreaOptionsWithoutDuplicates()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.Area] = "Backend / API"
        };
        var options = Svc.GetOptionsFor(QuestionKeys.DoNotTouch, answers);
        Assert.Contains("Public API contracts", options);
        Assert.Contains("Auth logic", options);
        Assert.Equal(options.Count, options.Distinct().Count());
    }

    [Fact]
    public void DoNotTouch_DatabaseAndInfra_AddsBothAreas()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.Area] = "Database||Infra / DevOps"
        };
        var options = Svc.GetOptionsFor(QuestionKeys.DoNotTouch, answers);
        Assert.Contains("Schema / migrations", options);
        Assert.Contains("CI/CD pipelines", options);
        Assert.Equal(options.Count, options.Distinct().Count());
    }

    [Fact]
    public void Verification_NoArea_ReturnsFourGeneralOptions()
    {
        var options = Svc.GetOptionsFor(QuestionKeys.Verification, new Dictionary<string, string>());
        Assert.Equal(4, options.Count);
        Assert.Contains("Run the existing test suite", options);
        Assert.Contains("Write new tests for the change", options);
        Assert.Contains("Project must build without errors/warnings", options);
        Assert.Contains("Run linter / formatter", options);
    }

    [Fact]
    public void Verification_TestingArea_AddsAreaOptionsWithoutDuplicates()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.Area] = "Testing / QA"
        };
        var options = Svc.GetOptionsFor(QuestionKeys.Verification, answers);
        Assert.Contains("All existing tests still pass", options);
        Assert.Equal(options.Count, options.Distinct().Count());
    }

    [Fact]
    public void Verification_FrontendArea_AddsBrowserCheck()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.Area] = "Frontend / UI"
        };
        var options = Svc.GetOptionsFor(QuestionKeys.Verification, answers);
        Assert.Contains("Manual check in the browser", options);
    }
}

public class NewAgenticCartSectionTests
{
    private static readonly RecipeService Svc = new();

    private static Dictionary<string, string> MinimalAnswers() => new()
    {
        [QuestionKeys.TaskType] = "New feature",
        [QuestionKeys.Area] = "Frontend / UI",
        [QuestionKeys.WhatToDo] = "Add a login page",
        [QuestionKeys.OutputFormat] = "Code + brief explanation"
    };

    [Fact]
    public void EmptyNewAnswers_NoNewSections()
    {
        var cart = Svc.AssembleCart(MinimalAnswers());
        Assert.DoesNotContain("Do not touch:", cart);
        Assert.DoesNotContain("Acceptance criteria:", cart);
        Assert.DoesNotContain("Verification:", cart);
        Assert.DoesNotContain("Environment requirements:", cart);
        Assert.DoesNotContain("Milestones (in order):", cart);
        Assert.DoesNotContain("Stop and ask when:", cart);
    }

    [Fact]
    public void DoNotTouch_IncludedWithExactLabel()
    {
        var answers = MinimalAnswers();
        answers[QuestionKeys.DoNotTouch] = "CI/CD workflow files||Config / secrets / environment files";
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("Do not touch: CI/CD workflow files, Config / secrets / environment files", cart);
    }

    [Fact]
    public void AcceptanceCriteria_IncludedWithExactLabel()
    {
        var answers = MinimalAnswers();
        answers[QuestionKeys.AcceptanceCriteria] = "Login page renders\nTests pass";
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("Acceptance criteria:", cart);
        Assert.Contains("Login page renders", cart);
    }

    [Fact]
    public void Verification_IncludedWithExactLabel()
    {
        var answers = MinimalAnswers();
        answers[QuestionKeys.Verification] = "Run the existing test suite||Run linter / formatter";
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("Verification: Run the existing test suite, Run linter / formatter", cart);
    }

    [Fact]
    public void Environment_IncludedWithExactLabel()
    {
        var answers = MinimalAnswers();
        answers[QuestionKeys.Environment] = "Create a feature branch first";
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("Environment requirements: Create a feature branch first", cart);
    }

    [Fact]
    public void Milestones_IncludedWithExactLabel()
    {
        var answers = MinimalAnswers();
        answers[QuestionKeys.Milestones] = "1. Scaffold 2. Style 3. Wire up";
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("Milestones (in order): 1. Scaffold 2. Style 3. Wire up", cart);
    }

    [Fact]
    public void StopAndAsk_IncludedWithExactLabel()
    {
        var answers = MinimalAnswers();
        answers[QuestionKeys.StopAndAsk] = "Before committing / pushing";
        var cart = Svc.AssembleCart(answers);
        Assert.Contains("Stop and ask when: Before committing / pushing", cart);
    }
}

public class NewAgenticDiscernmentTests
{
    private static readonly RecipeService Svc = new();

    [Fact]
    public void AcceptanceCriteria_OneItemPerLine()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.AcceptanceCriteria] = "Login page renders\nRedirect works\n\nTests pass"
        };
        var items = Svc.GetDiscernmentItems(answers);
        Assert.Contains("Verify criterion: Login page renders", items);
        Assert.Contains("Verify criterion: Redirect works", items);
        Assert.Contains("Verify criterion: Tests pass", items);
        Assert.Equal(3, items.Count(i => i.StartsWith("Verify criterion:", StringComparison.Ordinal)));
    }

    [Fact]
    public void DoNotTouch_OneItemPerChoice()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.DoNotTouch] = "CI/CD workflow files||Database schema / migrations"
        };
        var items = Svc.GetDiscernmentItems(answers);
        Assert.Contains("Confirm CI/CD workflow files were not modified", items);
        Assert.Contains("Confirm Database schema / migrations were not modified", items);
    }

    [Fact]
    public void StopAndAsk_AddsPauseItem()
    {
        var answers = new Dictionary<string, string>
        {
            [QuestionKeys.StopAndAsk] = "Before any destructive operation (deletes, migrations, force-push)"
        };
        var items = Svc.GetDiscernmentItems(answers);
        Assert.Contains("Agent should pause and ask when: Before any destructive operation (deletes, migrations, force-push)", items);
    }

    [Fact]
    public void NoNewAnswers_OnlyExistingItems()
    {
        var items = Svc.GetDiscernmentItems(new Dictionary<string, string>());
        Assert.DoesNotContain(items, i => i.StartsWith("Verify criterion:", StringComparison.Ordinal));
        Assert.DoesNotContain(items, i => i.StartsWith("Confirm ", StringComparison.Ordinal));
        Assert.DoesNotContain(items, i => i.StartsWith("Agent should pause", StringComparison.Ordinal));
    }
}

public class NewAgenticDiligenceTests
{
    private static readonly RecipeService Svc = new();

    [Fact]
    public void RunExistingTestSuite_AddsFullTestSuite_EvenForNewFeature()
    {
        var recipe = new Dictionary<string, string>
        {
            [QuestionKeys.TaskType] = "New feature",
            [QuestionKeys.Verification] = "Run the existing test suite"
        };
        var items = Svc.GetDiligenceItems(recipe, new Dictionary<string, string>());
        Assert.Contains("Run the full test suite", items);
    }

    [Fact]
    public void WriteNewTests_AddsCoverageItem()
    {
        var recipe = new Dictionary<string, string>
        {
            [QuestionKeys.TaskType] = "New feature",
            [QuestionKeys.Verification] = "Write new tests for the change"
        };
        var items = Svc.GetDiligenceItems(recipe, new Dictionary<string, string>());
        Assert.Contains("Verify the new tests cover the change", items);
    }

    [Fact]
    public void OtherVerificationChoices_AddVerifyItems()
    {
        var recipe = new Dictionary<string, string>
        {
            [QuestionKeys.TaskType] = "New feature",
            [QuestionKeys.Verification] = "Project must build without errors/warnings||Run linter / formatter"
        };
        var items = Svc.GetDiligenceItems(recipe, new Dictionary<string, string>());
        Assert.Contains("Verify: Project must build without errors/warnings", items);
        Assert.Contains("Verify: Run linter / formatter", items);
    }

    [Fact]
    public void RunExistingTestSuite_DoesNotAlsoAddVerifyItem()
    {
        var recipe = new Dictionary<string, string>
        {
            [QuestionKeys.TaskType] = "New feature",
            [QuestionKeys.Verification] = "Run the existing test suite"
        };
        var items = Svc.GetDiligenceItems(recipe, new Dictionary<string, string>());
        Assert.DoesNotContain(items, i => i.StartsWith("Verify: Run the existing test suite", StringComparison.Ordinal));
    }

    [Fact]
    public void NoVerification_NoNewItems()
    {
        var recipe = new Dictionary<string, string> { [QuestionKeys.TaskType] = "Explain code" };
        var items = Svc.GetDiligenceItems(recipe, new Dictionary<string, string>());
        Assert.DoesNotContain("Run the full test suite", items);
        Assert.DoesNotContain(items, i => i.StartsWith("Verify: ", StringComparison.Ordinal));
    }
}
