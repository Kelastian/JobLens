# JobLens — Project Context

> This file is the project brief for **JobLens**, a portfolio project by Sebastián Pizarro.
> Placed at the repo root as `CLAUDE.md` so Claude Code loads it automatically each session.
> If Claude Code doesn't pick it up, start the session with: "Read CLAUDE.md and follow it."

---

## 1. Purpose & audience

Portfolio project for **Sebastián Pizarro**, a computer engineer applying for **junior /
entry software roles in Japan** (SES agencies and direct-hire — TokyoDev, Japan Dev, Daijob,
Wantedly). Reviewers are technical recruiters and engineers.

**What this project demonstrates to a reviewer:**
- Solid **C# / .NET** fundamentals (their bread-and-butter stack in Japan).
- **AI integration as a real feature** — not "built with AI assistance", but AI *inside* the
  product (LLM calls, structured output, prompt engineering, provider fallback).
- **Trilingual capability** (ES / EN / JA), which is Sebastián's actual profile advantage.
- Clean layered architecture, tests, and honest documentation of design decisions —
  the same standard as the previous portfolio piece (Kantan Connect).

**Non-goals:** SaaS, multi-user, deployment to production, mobile app. This is a portfolio
piece, not a product.

## 2. What we are building

**JobLens — an AI-powered analyzer for Japanese job postings.**

The user pastes the text of a Japanese (or English) job posting, **or a URL**. JobLens uses an
LLM to:

1. **Translate & summarize** the posting into the user's preferred language (ES / EN / JA).
2. **Extract structured requirements** as JSON: tech stack, seniority, years of experience,
   Japanese level required, English level required, work style (onsite / hybrid / remote),
   salary if listed, visa sponsorship mentions.
3. **Score the match** against the user's profile (0–100) with a written explanation of
   strengths, gaps, and 2–3 concrete suggestions for how to frame the application.
4. **Save the analysis** to a local history so the user can revisit and compare postings later.

### Trilingual behaviour — the core UX rule

Every input × output language combination must work seamlessly:

| Input (posting) → | Output (analysis in) |
|--------------------|----------------------|
| Japanese           | ES / EN / JA         |
| English            | ES / EN / JA         |
| Spanish            | ES / EN / JA         |

The user picks the **output language** in the UI; **input language is auto-detected**. The
analysis (translation, summary, extracted fields' labels, match explanation, suggestions) is
fully rendered in the chosen output language. This trilingual capability is the product's
differentiator and must be a first-class concern in prompts, tests, and UI copy — not an
afterthought.

### Sebastián's profile (used for match scoring)

Ship a default profile that matches Sebastián's real CV (C# / .NET, Next.js / React,
PostgreSQL / Supabase, VBA / SQL / Alteryx automation at J.P. Morgan, computer engineering
degree, ES native / EN professional / JA conversational ≈ N4, based in Greater Tokyo).
Store it as an editable JSON file so any user can adapt it to themselves.

## 3. Tech stack (decisions already made)

- **Backend: ASP.NET Core Web API on .NET 10 (LTS)**, latest C#. Same runtime as Kantan
  Connect, for consistency across Sebastián's portfolio.
- **Frontend: Blazor Server** in the same solution (single-project deployment, no separate
  Node build; a reviewer can `dotnet run` and see the app immediately). If Blazor turns out to
  be a friction point during build, fall back to a plain Razor Pages UI — do **not** introduce
  a separate JS build pipeline for this project.
- **Input: paste text OR paste URL** (URL scraping is in scope from day one).
- **Persistence: SQLite via Entity Framework Core** — zero setup, single file, portable.
- **LLM provider: Google Gemini 2.5 Flash** as primary (1M-token context, native Japanese,
  free tier ~1,500 req/day, no credit card).
  - **Fallback: Groq (Llama 3.3 70B)**, wired behind the same interface. If Gemini fails or
    hits rate limits, JobLens automatically retries via Groq. This is a *design decision worth
    demonstrating*, not just resilience for its own sake.
- **Web scraping:** `HttpClient` + **AngleSharp** for HTML parsing. Simple, no headless
  browser — if a site requires JavaScript rendering, the app tells the user "paste the text
  instead" gracefully.
- **Tests: xUnit.**
- **Secrets:** API keys via `dotnet user-secrets` in development; documented in the README
  for anyone cloning the repo.

## 4. Architecture — layered / clean separation

```
JobLens.sln
├─ src/
│  ├─ JobLens.Domain              (net10.0)    ← zero external dependencies
│  │   ├─ Models/                 JobPosting, ExtractedRequirements, UserProfile,
│  │   │                          MatchResult, AnalysisRecord
│  │   ├─ Enums/                  Language (Es, En, Ja), WorkStyle, SeniorityLevel
│  │   ├─ Abstractions/           ILlmProvider, IJobPostingFetcher, IAnalysisRepository
│  │   └─ Exceptions/             LlmResponseException, JobFetchException — live here,
│  │                              not in Application, so Infrastructure (which throws
│  │                              them) can reference them without depending on Application
│  │
│  ├─ JobLens.Application         (net10.0)    ← use cases
│  │   ├─ Analysis/               AnalysisService — orchestrates fetch → LLM → persist
│  │   ├─ Prompts/                PromptBuilder — trilingual, per-language templates
│  │   ├─ Matching/               MatchScorer — invokes LLM, validates the JSON returned
│  │   └─ Languages/              LanguageDetector — heuristic (hiragana/katakana ratio,
│  │                              Spanish diacritics), no external service. Lives here
│  │                              (not Infrastructure) because AnalysisService calls it
│  │                              directly and Application cannot depend on Infrastructure
│  │
│  ├─ JobLens.Infrastructure      (net10.0)    ← implementations
│  │   ├─ Llm/
│  │   │   ├─ GeminiProvider      ILlmProvider — Google Gemini API
│  │   │   ├─ GroqProvider        ILlmProvider — Groq (Llama 3.3 70B) fallback
│  │   │   └─ ResilientLlmClient  wraps a primary + fallback, handles retries
│  │   ├─ Scraping/               HtmlJobFetcher — HttpClient + AngleSharp
│  │   └─ Persistence/            EF Core SQLite context + AnalysisRepository
│  │
│  └─ JobLens.Web                 (net10.0, Blazor Server) ← UI + composition root
│      ├─ Pages/                  Home, Analyze, History, ProfileEditor
│      ├─ Components/             InputPanel, ResultCard, MatchGauge, HistoryList
│      ├─ Localization/           .resx or JSON per language (ES/EN/JA UI strings)
│      └─ Program.cs              DI wiring — register the resilient client here
│
└─ tests/
   ├─ JobLens.Application.Tests   (xUnit) — PromptBuilder per language, MatchScorer
   │                                        JSON validation, service orchestration
   └─ JobLens.Infrastructure.Tests (xUnit) — LlmProvider parsing/error handling
                                             (against captured JSON, not real API),
                                             SQLite repository, language detection
```

**Dependency rule:** Web → Application → Domain; Infrastructure → Domain; Web references
Infrastructure only for DI wiring in `Program.cs`. **Domain depends on nothing.** This is
the same rule as Kantan Connect — reviewers who look at both should see one consistent style.

## 5. Design principles to demonstrate

(These are the reviewer-facing signals. Make them visible in the code and the README.)

- **SOLID**, especially **dependency inversion**: the app depends on `ILlmProvider` and
  `IJobPostingFetcher`, never on Gemini or Groq specifically.
- **The primary + fallback LLM pattern** is *the* architectural highlight of the project.
  Design it deliberately: primary provider fails or rate-limits → fall back to secondary.
  Document why in the README.
- **Structured LLM output** — always request JSON with a schema, then **validate** it in code
  (`System.Text.Json` with a schema check or POCO deserialization). Never trust raw text.
- **Prompt engineering as code**: prompts are objects built by `PromptBuilder`, parameterized
  by language and profile — not string literals scattered around.
- **Guardrails against LLM failure modes**: malformed JSON, hallucinated fields, empty
  responses. Show these as explicit `try/catch` paths, not silent fallbacks.
- **Constructor injection** everywhere; no `new`-ing services inside services.
- **Immutability** where natural (records for domain models).
- **No business logic in Blazor pages** — pages call `AnalysisService` and render.

## 6. Two branches (same rule as Kantan Connect)

- **`main`** — the clean version. Minimal comments, only where they add real value.
  Production-style code.
- **`annotated`** — identical behavior, thorough teaching comments. For each non-obvious
  decision, a `// WHY:` comment explaining the reasoning (why an interface here, why this
  fallback strategy, why validate the JSON this way, why immutable, etc.). This is
  Sebastián's study copy.

**Workflow:** finish `main` clean version first, then branch `annotated` from it and add
only comments. **Do not change behavior between branches.**

## 7. README requirements (in English, with a JA summary at the end)

- One-paragraph description + 1–2 screenshots (input page, results page with a real analysis
  in Japanese → Spanish, for example — shows the trilingual feature immediately).
- **The line, verbatim:** *"Built with AI assistance; architecture and design decisions are
  my own."* One mention, no long apology.
- **Architecture** section: short explanation of each layer + a dependency diagram
  (ASCII or mermaid).
- **Design decisions** section — the part reviewers actually read. Cover:
  - Why Gemini 2.5 Flash as primary (1M context, native JA, free tier).
  - Why a fallback provider at all, and how the interface makes it swappable.
  - Why structured JSON output with schema validation instead of parsing free text.
  - How prompts are built per-language (and why prompts are not scattered strings).
  - How the language detector works and why it's a heuristic, not another LLM call.
- **How to run**: `dotnet user-secrets` steps for API keys, then `dotnet run --project
  src/JobLens.Web` and `dotnet test`.
- **Tech stack** table.
- **A short 日本語 summary** at the end (following the same pattern as Kantan Connect).

## 8. Suggested build order

1. Scaffold the solution + 5 projects; set references and DI wiring.
2. Domain models + enums + interfaces.
3. `LanguageDetector` + its tests (unblocks everything downstream).
4. `PromptBuilder` per language + its tests (ES/EN/JA templates).
5. `GeminiProvider` implementing `ILlmProvider`. Test against captured JSON responses.
6. `HtmlJobFetcher` (URL scraping with AngleSharp) + tests against saved HTML fixtures.
7. `AnalysisService` orchestrating fetch → LLM → validated result.
8. EF Core + SQLite persistence + repository tests.
9. Blazor UI: input panel, results view, history, profile editor. Wire DI.
10. Add `GroqProvider` + `ResilientLlmClient` (primary + fallback pattern).
11. UI localization (ES / EN / JA strings).
12. Screenshots, README, JA summary.
13. Create the `annotated` branch and add `// WHY:` comments.

## 9. Scope guardrails

- Keep it tight — a portfolio piece, not a product. **No auth, no multi-user, no deployment
  to production.**
- Prefer doing less, but cleaner. If unsure, choose the simpler, more readable option.
- **The trilingual behaviour is the differentiator** — do not cut corners on it. Every
  language combination must be tested end-to-end at least once manually with a real posting
  before commit.
- Rate-limit handling and the primary/fallback pattern are **required** — they're the
  design story of the project.
- Commit in small, meaningful steps with clear messages (recruiters may read the commit
  history).
- **Do not add** a `Co-Authored-By: Claude` trailer or "Generated with Claude Code" line
  to commits or PRs.

## 10. Sample assets to ship

**A sample profile** (`profile.default.json`) matching Sebastián's CV, so the app runs
end-to-end out of the box. Keep it editable.

**Three real job postings** (or realistic anonymized ones) saved as text files under
`samples/`, one per language (ES / EN / JA), for a `Load sample` button. This makes the
demo instant for a reviewer who clones the repo.