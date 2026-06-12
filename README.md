# Prompt Recipe

![CI](https://github.com/HenrikSondergaard/PromptRecipe/actions/workflows/ci.yml/badge.svg)

A guided tool that helps developers write better AI coding prompts by collecting the right information before they talk to an agent.

## What it does

AI coding agents like Claude Code and Codex produce poor results when given vague or incomplete prompts. It's easy to forget important details — context, constraints, output format, what not to change.

Prompt Recipe walks you through a short set of guided questions and assembles a structured **prompt shopping cart**: a complete list of ingredients for a well-specified prompt. You copy the cart, paste it into your AI chat tool (Claude.ai, ChatGPT, etc.), and let that AI formulate the final prompt — which you then use in your coding agent.

**The app does not generate the final prompt itself.** It collects the right information and lets your chosen AI do the formulation.

## Three steps

1. **Recipe** — answer a short series of guided questions about your task
2. **Cart** — review the assembled prompt ingredients and copy with one click
3. **Build** — paste the cart into Claude.ai or ChatGPT to get a complete, well-structured prompt, then use that prompt in Claude Code or Codex

## Use it

**[Open Prompt Recipe →](https://henriksondergaard.github.io/PromptRecipe/)**

No account required. Nothing is stored. Everything runs in your browser.

## Run locally

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
git clone https://github.com/HenrikSondergaard/PromptRecipe.git
cd PromptRecipe
dotnet run
```

Then open the URL shown in the terminal (typically `https://localhost:5001`).

## Run tests

```bash
dotnet test PromptRecipe.Tests/PromptRecipe.Tests.csproj
```

## Tech stack

- **Blazor WebAssembly (.NET 9)** — C# running in the browser via WebAssembly, no server required
- **GitHub Pages** — fully static hosting via the `gh-pages` branch
- **GitHub Actions** — CI (build, lint, test) on every push and PR; deploy on merge to main

## Contributing

See [CLAUDE.md](CLAUDE.md) for constraints that apply when using AI coding agents on this project.

For human contributors: open a PR targeting `main`. CI must pass before merge. Keep the app static — no external APIs, no server, no secrets.

## License

[MIT](LICENSE)
