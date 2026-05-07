# Prompt Recipe

## What the project does and what problem it solves

Developers using AI coding agents (Claude Code, Codex, etc.) get poor results
when they provide vague or incomplete prompts. It is hard to remember all the
details an agent needs: context, constraints, output format, what NOT to do, etc.

Prompt Recipe is a three-step tool:

1. **Recipe** — The user starts a new recipe and answers a series of guided
   questions, OR pastes an existing weak prompt and answers follow-up questions.
   The app ensures no important ingredients are missing.

2. **Shopping cart** — The app assembles the answers into a structured
   "prompt shopping cart": a clear list of all the ingredients needed for a
   complete prompt. The user copies the cart with one click.

3. **Build** — The user pastes the cart into their AI chat tool (Claude.ai,
   ChatGPT, etc.), which then formulates the complete, well-structured prompt.
   That final prompt is used in the coding agent (Claude Code, Codex, etc.).

The app does NOT generate the final prompt itself — it collects the right
information and lets the user's chosen AI do the formulation.

## Tech stack

- **Blazor WebAssembly (.NET 9)** — C# running in the browser via WebAssembly,
  deployable as a fully static site to GitHub Pages with no server required.
- **GitHub Pages** — hosting via a gh-pages branch with a GitHub Actions workflow.
- No external dependencies, no API calls, no keys — completely static app.

Rationale: The project owner is a .NET/C# developer. Blazor WASM provides the
right balance between familiar technology and new learning, without requiring a
new backend stack. No server is needed because the app only manages local state
and text assembly.

## Architecture principles

- **The flow is linear and explicit** — three clearly defined steps
  (Recipe → Cart → Done) with explicit state tracking for the current step.
- **All question logic and assembly logic lives in one service**
  (`Services/RecipeService.cs`). Components handle UI only.
- **Each step is its own Razor component** — RecipeForm, ShoppingCart, DoneView.
  No component knows about the others.
- **No global state** — state is passed down via parameters and up via
  EventCallback.
- **Questions are data-driven** — the question list is defined as data
  (a list of objects), not as hardcoded markup. Easy to add or change questions
  without touching UI code.

## What the agent must NOT do

- **Do not integrate any external API** — no AI calls, no API keys, no
  authentication. The app is and remains completely static.
- **Do not add NuGet packages** without asking and explaining why.
- **Do not switch UI libraries** without asking — stick to standard Blazor + CSS.
- **Do not create global variables or stateful Singleton services.**
- **Do not generate the final prompt** — the app's responsibility ends at the
  shopping cart.
- **Do not introduce JavaScript interop** if the problem can be solved in C#.
- **Do not modify the GitHub Actions workflow** without explaining what changes
  and why.