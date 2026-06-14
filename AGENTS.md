# AGENTS.md

Guidance for OpenCode sessions working in this .NET 10 / C# tutorial repo. Each item is a fact an agent would likely get wrong without help.

## Stack & layout

- **Solution:** `AiAgentsFromScratch.slnx` (the new XML solution format, not the legacy `.sln`).
- **Target framework:** `net10.0` on every project. Requires the **.NET 10 SDK** — there is no `global.json`, so SDK version is whatever is installed; `dotnet --list-sdks` should show a `10.x` entry before anything else is attempted.
- **Projects (16 total):** one shared library plus 15 standalone console apps, one per chapter.
  - `src/AiAgents.Core/` — shared helpers (`Client/`, `Exceptions/`, `Memory/`, `Parsing/`, `Tools/`, `Visualization/`). All chapter projects reference this.
  - `src/Chapter01/` … `src/Chapter15/` — one executable per chapter. Each is a single-`Program.cs` console app; do not split them.
- **Companion docs:** every chapter has a parallel `examples/NN_*/CODE.md` (walkthrough) and `CONCEPT.md` (concepts + diagrams). Keep code and these two docs in sync when a chapter changes.
- **Companion website:** `web/` holds a separate Next.js 16 app that renders the same content. It is **not** part of the .NET solution. See the [Companion website (`web/`)](#companion-website-web) section below before touching anything in there.
- **No tests, no CI, no lint/format config.** There is no `.editorconfig`, no `Directory.Build.props`, no `.github/workflows/`, no analyzers configured. Do not invent a lint or test step for the .NET side — `dotnet build` passing is the only verifiable gate. The web app similarly has no test runner; `npm run build` (which calls `next build`) is the gate.

## Build & run

- **Build the whole solution:** `dotnet build` (≈5s, 0 warnings).
- **Run a single chapter:** `dotnet run --project src/ChapterNN` from the repo root. The chapter's `appsettings*.json` files are read with `Directory.GetCurrentDirectory()` as the base, so do **not** run from inside `src/` — `ConfigurationFactory` will then look in the wrong place.
- **Run from inside a chapter:** `cd src/ChapterNN && dotnet run` is also valid (it is what the README shows).
- **No `--no-restore` needed;** NuGet restore runs implicitly and is fast.

## Secrets & config (this trips up every new agent)

- **Per-chapter config files:**
  - `src/ChapterNN/appsettings.json` — non-secret, **committed**. Contains `DeepSeek.BaseUrl`, `DeepSeek.ChatModel` (`deepseek-v4-flash`); Chapter 15 also has `Embeddings.ModelPath`.
  - `src/ChapterNN/appsettings.Secrets.example.json` — **committed** template with `DeepSeek.ApiKey: "your-deepseek-api-key-here"`.
  - `src/ChapterNN/appsettings.Secrets.json` — **git-ignored**, holds the real key. Must be created locally before the chapter can run.
- **One-time setup, all chapters** (PowerShell, run from repo root):
  ```powershell
  Get-ChildItem src/Chapter* | ForEach-Object {
      Copy-Item "$($_.FullName)/appsettings.Secrets.example.json" "$($_.FullName)/appsettings.Secrets.json" -Force
  }
  ```
  Then edit each `appsettings.Secrets.json` and paste the real `DeepSeek.ApiKey`.
- **Env-var fallback:** `ConfigurationFactory` also reads `AddEnvironmentVariables()`, so `DEEPSEEK__APIKEY=sk-...` works (double underscore for the nested section/key). Useful in CI or when you don't want a secrets file on disk.
- **Do not commit** `appsettings.Secrets.json` anywhere. The `.gitignore` already excludes it.
- **`.env.example` is a leftover from the JS tutorial port** — it mentions `OPENAI_API_KEY` and the .NET code does **not** read it. Ignore that file; use `appsettings.Secrets.json` or `DEEPSEEK__APIKEY` instead.
- **The README's `cp` example has a typo** (`appsrets.json`) — use the PowerShell snippet above, or copy manually to `appsettings.Secrets.json`.

## DeepSeek + OpenAI SDK specifics

- The repo uses the **official `OpenAI` .NET SDK** pointed at DeepSeek's OpenAI-compatible base URL (`https://api.deepseek.com`). Everything is an `OpenAI.Chat.ChatClient` — there is no DeepSeek-specific client class. Custom wiring lives in `src/AiAgents.Core/Client/DeepSeekClientFactory.cs`.
- Default model name is **`deepseek-v4-flash`**. Change it in `appsettings.json` under `DeepSeek.ChatModel` if needed; the SDK is called with that string verbatim.
- The OpenAI SDK package is referenced only by `AiAgents.Core`; chapter projects do not need to reference it directly — they use the helpers.

## Companion website (`web/`)

A separate, self-contained Next.js app lives in `web/`. It is the local source of the published companion site and renders the chapters straight out of this repo.

- **Stack:** Next.js **16.1** (App Router, static export — `next.config.ts` sets `output: "export"` and `trailingSlash: true`), React 19, Tailwind CSS 4, TypeScript 5, `tsx` for the extract script. Node modules are managed with **npm** (`package-lock.json` is committed).
- **Routes (App Router):** `src/app/page.tsx` redirects to `/timeline`. Real pages are `timeline/`, `layers/`, `compare/`, `[chapter]/` and `[chapter]/diff/`. Statically generated for every id in `CHAPTER_ORDER` (`src/lib/constants.ts`).
- **Scripts (run from `web/`):**
  - `npm run dev` — starts `next dev` on `http://localhost:3000`. `predev` runs `extract` first.
  - `npm run build` — static export to `web/out/`. `prebuild` runs `extract` first.
  - `npm run serve` — `npx serve out` for previewing the static export.
  - `npm run extract` — runs `tsx scripts/extract-content.ts`. **Always run this manually** if you edited chapters or `examples/` and want to test the site without going through `dev`/`build`.
- **Content pipeline (the part that surprises agents):** `web/scripts/extract-content.ts` walks `examples/NN_*/CODE.md` + `CONCEPT.md`, the matching `src/ChapterNN/Program.cs`, plus `src/AiAgents.Core/` and `diagrams/`. It writes two generated files:
  - `web/src/data/generated/chapters.json`
  - `web/src/data/generated/docs.json`

  Both are **git-ignored** (see `web/.gitignore`); they only exist after running `extract`. Components import them via `@/data/generated/...`, so if you open the project fresh and TypeScript complains about missing JSON, run `npm run extract`.
- **Adding a new chapter (or renaming one):** update `CHAPTER_ORDER` and `CHAPTER_META` in `web/src/lib/constants.ts` (and `LAYERS` if the layer mix changes) in the **same commit** as the new `src/ChapterNN/` and `examples/NN_*/`. The extract script picks up directories named `^\d{2}_` automatically, but the site will not link them without the constants entry.
- **Assets:** images referenced from `examples/` or `diagrams/` are copied into `web/public/course-assets/` by the extract script. Do not hand-edit that folder — it is regenerated every run.
- **`.gitignore`:** `web/.gitignore` excludes `node_modules/`, `.next/`, `out/`, `src/data/generated/`, and `*.tsbuildinfo`. The root `.gitignore` already covers `node_modules`, so the web folder needs no special handling at the root.
- **Do not** introduce server components that need a Node runtime at request time, API routes, or `getServerSideProps`-style data fetching — the site is exported statically. If you need new data, add it to the extract script.
- **Do not** confuse the chapter-level `appsettings*.json` (DeepSeek keys for the .NET console apps) with anything in `web/`. The web app never calls DeepSeek; it only reads markdown and `Program.cs` files at build time.

## Chapter 15 (local embeddings) — extra gotchas

- Adds two packages: `LLamaSharp` 0.21.0 and `LLamaSharp.Backend.Cpu` 0.21.0. Only this project references them.
- **Requires a local GGUF model** at the path set in `src/Chapter15/appsettings.json` → `Embeddings.ModelPath` (default: `models/bge-small-en-v1.5-q8_0.gguf`). The `models/` directory is git-ignored.
- If the model file is missing, Chapter 15 prints a helpful message and exits cleanly — that is the expected behavior, not a crash.
- Native binaries land in `src/Chapter15/bin/Debug/net10.0/runtimes/{win-x64,osx-arm64,osx-x64,linux-x64}/...` after first build; this is normal LLamaSharp output and is git-ignored.

## Conventions (from CONTRIBUTING.md and code style)

- Target `net10.0`, `<ImplicitUsings>enable</ImplicitUsings>`, `<Nullable>enable</Nullable>` — match the existing csproj style.
- Prefer the shared `AiAgents.Core` helpers over reinventing (e.g. use `DeepSeekClientFactory`, `ToolBox`, `MemoryManager`, `JsonParser`, `RetryHelper`).
- **No heavy frameworks** (LangChain, Semantic Kernel, etc.) — the repo's purpose is to teach what they do, not to use them.
- Clarity over cleverness. Self-contained examples; comments should explain *why*, not *what*.
- New example pattern requires three additions in lockstep: `src/ChapterNN/`, `examples/NN_*/CODE.md`, `examples/NN_*/CONCEPT.md`. Plus `appsettings.json` and `appsettings.Secrets.example.json` (never the real secrets file). If you want it to appear on the companion website, also add it to `CHAPTER_ORDER`/`CHAPTER_META` in `web/src/lib/constants.ts` in the same change.
- Branch naming: `fix/issue-description`. No release branching described.

## Things that look wrong but are intentional

- `bin/` and `obj/` exist on disk in this working tree even though `.gitignore` excludes them — leftover from a prior build, safe to delete with `dotnet clean` or `Get-ChildItem -Recurse -Include bin,obj | Remove-Item -Recurse -Force`.
- `.gitignore` has `internal`, `ui`, `frontend*` patterns — vestigial from the JS tutorial port, harmless.
- A `.git/opencode/opencode` binary exists; it is OpenCode's own scratch file inside the git dir, not part of the repo.
