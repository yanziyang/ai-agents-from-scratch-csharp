# Contributing Guidelines

Thank you for considering contributing to AI Agents from Scratch!

## Project Philosophy

This repository teaches AI agent fundamentals by building from scratch in C# with .NET 10. Every contribution should support this learning mission.

**Core Principles:**
- **Clarity over cleverness** — code should be easy to understand
- **Fundamentals first** — no black boxes or magic
- **Progressive learning** — each example builds on the previous
- **Minimal dependencies** — prefer the shared `AiAgents.Core` helpers and the OpenAI .NET SDK

## Types of Contributions

### Bug Reports
Found something broken? Open an issue with:
- Which example (`src/ChapterXX`)
- What you expected vs. what happened
- Your environment (.NET SDK version, OS, model used)
- Steps to reproduce

### Documentation Improvements
- Typos and grammar fixes
- Clearer explanations
- Better code comments
- Additional examples in documentation
- Mermaid diagrams and visualizations

### New Examples
Want to add a new agent pattern? Great! Please:
1. **Open an issue first** — let's discuss if it fits
2. Follow the existing structure:
   - `src/ChapterNN/` — working .NET console app
   - `examples/NN_pattern-name/CODE.md` — detailed code walkthrough
   - `examples/NN_pattern-name/CONCEPT.md` — why it matters, use cases
3. Keep it simple and well-commented
4. Add `appsettings.json`, `appsettings.Secrets.example.json`, and a git-ignored `appsettings.Secrets.json`
5. Test thoroughly with DeepSeek V4 Flash (or document the model you used)

### Code Improvements
- Performance optimizations (with benchmarks)
- Better error handling
- Clearer variable names
- More helpful console output

## What We're Not Looking For

- Heavy framework integrations (LangChain, Semantic Kernel, etc.) — this repo teaches what they do
- Production features (monitoring, scaling) — this is educational
- Complex abstractions — keep it beginner-friendly

## Contribution Process

1. **Fork** the repository
2. **Create a branch**: `git checkout -b fix/issue-description`
3. **Make changes** and test thoroughly (`dotnet build` should pass)
4. **Commit** with clear messages: `git commit -m "Fix: clarify ReAct loop explanation"`
5. **Push**: `git push origin fix/issue-description`
6. **Open a Pull Request** with:
   - Clear title
   - Description of what changed and why
   - Which issue it addresses (if any)

## Code Standards

- Use clear, descriptive variable names
- Add comments explaining *why*, not just *what*
- Follow existing code style
- Keep examples self-contained where possible
- Target `net10.0`
- Never commit secrets; use `appsettings.Secrets.json` (git-ignored) and provide an `appsettings.Secrets.example.json` template

## Documentation Standards

- Use clear, simple language
- Explain concepts before code
- Include Mermaid diagrams where helpful
- Provide real-world use cases
- Link to related examples

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.
