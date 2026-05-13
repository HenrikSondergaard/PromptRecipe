namespace PromptRecipe.Models;

public record RecipeQuestion(
    string Key,
    string Label,
    QuestionType Type,
    IReadOnlyList<string> BaseOptions,
    string? FreeTextLabel = null,
    string? FreeTextKey = null);
