# JobLens

**An AI-powered analyzer for Japanese job postings.** Paste a job posting (or its URL) in
Japanese, English, or Spanish, and JobLens uses an LLM to translate and summarize it,
extract structured requirements, and score how well it matches your profile — with the
analysis rendered in the output language you choose.

> ⚠️ **Work in progress.** The core flow works end-to-end (input → language detection →
> LLM extraction & match scoring → local history), but several planned features are not
> done yet — see the [Roadmap](#roadmap) below. This is a portfolio project, not a
> finished product.

> Built with AI assistance; architecture and design decisions are my own.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![Blazor](https://img.shields.io/badge/UI-Blazor%20Server-5C2D91)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

---

## What it does today

- **Trilingual analysis (ES / EN / JA).** Input language is auto-detected; you pick the
  output language. Translation, summary, extracted fields, and match explanation all render
  in the chosen language.
- **Structured extraction.** Tech stack, seniority, years of experience, required Japanese
  (JLPT) and English (CEFR) levels, work style, salary, and visa-sponsorship mentions —
  extracted as validated JSON, not free text.
- **Match scoring.** A 0–100 score against your profile, with strengths, gaps, and 2–3
  concrete suggestions for framing your application.
- **Text or URL input.** Paste the posting text directly, or a URL (scraped with
  `HttpClient` + AngleSharp — no headless browser; JavaScript-rendered pages are handled
  gracefully).
- **Local history.** Past analyses are saved to a local SQLite database.

## Tech stack

| Area | Choice |
|---|---|
| Runtime | .NET 10, ASP.NET Core, Blazor Server |
| LLM provider | Google Gemini 3.5 Flash (via REST, no SDK) |
| Web scraping | `HttpClient` + AngleSharp |
| Persistence | SQLite via Entity Framework Core |
| Tests | xUnit |

The solution is layered — `Domain` (no external dependencies) ← `Application` (use cases)
← `Infrastructure` (LLM, scraping, persistence) — with `Web` as the UI and composition
root. The app depends on interfaces (`ILlmProvider`, `IJobPostingFetcher`,
`IAnalysisRepository`), never on a concrete provider directly.

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and a free
[Google Gemini API key](https://aistudio.google.com/apikey).

```powershell
git clone https://github.com/Kelastian/joblens.git
cd joblens

# Provide your Gemini API key (kept out of the repo, in user-secrets)
dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY_HERE" --project src/JobLens.Web

dotnet run --project src/JobLens.Web
```

Then open the URL shown in the console (e.g. `http://localhost:5106`).

```powershell
dotnet test
```

## Roadmap

- [x] Trilingual extraction and match scoring (Gemini)
- [x] Text and URL input
- [x] Local history (SQLite)
- [x] Editable profile
- [ ] **Groq (Llama 3.3 70B) fallback provider + `ResilientLlmClient`** — automatic
      failover when Gemini fails or hits rate limits, behind the same `ILlmProvider`
      interface. This primary + fallback pattern is a planned architectural highlight.
- [ ] UI localization (ES / EN / JA interface labels, not just the analysis content)
- [ ] Sample postings for a one-click demo
- [ ] Screenshots and a full design-decisions writeup

## License

[MIT](LICENSE)
