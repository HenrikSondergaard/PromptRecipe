# Prompt Recipe

## Vad projektet gör och vilket problem det löser

Utvecklare som använder AI-kodagenter (Claude Code, Codex m.fl.) får sämre 
resultat när de ger vaga eller ofullständiga promptar. Det är svårt att komma 
ihåg alla detaljer som en agent behöver: kontext, constraints, format, vad som 
INTE ska göras, osv.

Prompt Recipe är ett trestegsverktyg:

1. **Recept** — Användaren startar ett nytt recept och svarar på guidade frågor,
   ELLER klistrar in en befintlig bristfällig prompt och svarar på kompletterande 
   frågor. Appen säkerställer att inga viktiga ingredienser saknas.

2. **Inköpskorg** — Appen sammanställer svaren till en strukturerad 
   "prompt-inköpskorg": en tydlig lista med alla ingredienser som behövs för 
   en komplett prompt. Användaren kopierar korgen med ett klick.

3. **Bygg** — Användaren klistrar in korgen i sin AI-chatt (Claude.ai, ChatGPT 
   m.fl.) som i sin tur formulerar den kompletta, välstrukturerade prompten. 
   Den färdiga prompten används sedan i kodagenten (Claude Code, Codex m.fl.).

Appen genererar alltså INTE prompten själv — den samlar in rätt information 
och låter användarens valda AI göra formuleringen.

## Teknikstack

- **Blazor WebAssembly (.NET 9)** — C# i webbläsaren via WebAssembly, deploybar
  statiskt till GitHub Pages utan server.
- **GitHub Pages** — hosting via gh-pages-branch med GitHub Actions workflow.
- Inga externa beroenden, inget API, inga nycklar — helt statisk app.

Motivering: Projektägaren är .NET/C#-utvecklare. Blazor WASM ger rätt balans 
mellan välkänd teknik och lärande utan ny backend-stack. Ingen server behövs 
eftersom appen enbart hanterar lokal state och textgenerering.

## Arkitekturprinciper

- **Flödet är linjärt och explicit** — tre tydliga steg (Recept → Korg → Klar) 
  med tydligt state för vilket steg användaren befinner sig i.
- **All logik för frågor och sammanställning lever i en service** 
  (`Services/RecipeService.cs`). Komponenter hanterar enbart UI.
- **Varje steg är en egen Razor-komponent** — RecipeForm, ShoppingCart, 
  DoneView. Ingen komponent känner till de andra.
- **Ingen global state** — state skickas nedåt via parametrar och uppåt via 
  EventCallback.
- **Frågorna är datadrivna** — listan med frågor definieras som data 
  (lista av objekt), inte som hårdkodad markup. Lätt att lägga till/ändra frågor.

## Saker agenten INTE ska göra

- **Integrera inget externt API** — inga AI-anrop, inga API-nycklar, 
  ingen autentisering. Appen är och förblir helt statisk.
- **Lägg inte till NuGet-paket** utan att fråga och motivera varför.
- **Byt inte UI-bibliotek** utan att fråga — håll dig till standard Blazor + CSS.
- **Skapa inte globala variabler eller Singleton-services med tillstånd.**
- **Generera inte den färdiga prompten** — appens ansvar slutar vid inköpskorgen.
- **Introducera inte JavaScript interop** om problemet kan lösas i C#.
- **Ändra inte GitHub Actions-workflow** utan att förklara vad som ändras och varför.