using PromptRecipe.Models;
using PromptRecipe.Services;
using Xunit;

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
        [QuestionKeys.TaskType]    = "New feature",
        [QuestionKeys.Area]        = "Frontend / UI",
        [QuestionKeys.WhatToDo]    = "Add a login page",
        [QuestionKeys.OutputFormat] = "Code + brief explanation"
    };
}
