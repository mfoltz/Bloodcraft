# Codex Workflow Contract

> For any localization work, follow the Translation Workflow section below.

## 1. Glossary — Command Index

| Keyword                                   | Purpose                                                    |
|-------------------------------------------|------------------------------------------------------------|
| `CreatePRD <id> {context}`                | Start a new Project Requirement Document.                  |
| `CreateSubPRD <parent> <child> {context}` | Nest a PRD inside another.                                 |
| `AddTasks <id>`                           | Autogenerate task list for a PRD.                          |
| `RunTasks <id>`                           | Execute pending tasks and update status.                   |
| `SummarizePRD <id>`                       | Produce a progress report.                                 |
| `ClosePRD <id>`                           | Finalise and archive a completed PRD.                      |

> **ID conventions:** Lower-case, hyphenated (`feature-x-api`). Duplicate IDs **MUST** error.

## 2. Lifecycle — Canonical Flow

### 1. Scaffold goals
CreatePRD improve-codex-workflow
CreateSubPRD improve-codex-workflow networking-enhancements  # >5 tasks rule

### 2. Plan work
AddTasks improve-codex-workflow
AddTasks networking-enhancements

### 3. Execute & iterate
RunTasks improve-codex-workflow
RunTasks networking-enhancements

### 4. Inspect
SummarizePRD improve-codex-workflow
SummarizePRD networking-enhancements

### 5. Close out
ClosePRD networking-enhancements
ClosePRD improve-codex-workflow

Current PRDs and task lists are stored in `.project-management/current-prd/`, while completed items are moved to `.project-management/closed-prd/`. Codex is expected to own as much as it feels capable of doing so in terms of completion, furthering goals, and being generally proactive with managing tasks to completion of state project goals. Compile with .NET 8 to build with preview features, though do note VRising runs on the .NET 6 runtime as the .csproj implies; be mindful to stay within the .NET 6 API surface to avoid surprises.

---

### Translation Workflow

1. **Refresh the English source.**
   `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- generate-messages`
2. **Propagate new hashes.** Copy the refreshed `English.json` entries into each `Resources/Localization/Messages/<Language>.json` while preserving numeric hashes.
3. **Translate missing entries.** Use manual translation, Codex-assisted drafts, or another approved backend.
4. **Fix and verify tokens.**
   `python Tools/fix_tokens.py --check-only Resources/Localization/Messages/<Language>.json`
5. **Verify translations.**
   `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations --show-text`

Routine localization updates and translation QA do **not** require Argos model reconstruction. `Tools/ensure_argos_model.py` and `Tools/translate_argos.py` remain optional legacy fallbacks outside the default CI path; see [Docs/Localization-Argos-Legacy.md](Docs/Localization-Argos-Legacy.md) only if you intentionally choose Argos.

---

# Universal Principles for CSharp and General Programming

This guide provides universal, intelligent principles and patterns to master C# coding effectively, applicable across any project or repository.

## 🧠 Structured Reasoning

* **Single Responsibility Principle (SRP)**: Every class and method should have one clearly defined responsibility.
* **Explicit Intent**: Write self-descriptive methods and classes, avoiding ambiguous or overly general names.
* **Readability First**: Prioritize readability over cleverness. The intent of your code should be immediately clear to others.

## 🎯 Clarity & Explicitness

* Clearly specify the purpose, parameters, and return values of each method using XML documentation comments.
* Choose names that explicitly describe intent (e.g., `CalculateTotalPrice()` instead of `Calculate()` or `CalcTP()`).
* Favor descriptive variable names (`customerAge` rather than `ca`).
* Do not explicitly use 'private', it's implied well-enough as the default accessibility modifier.

## 🔄 Iterative Improvement

* Implement incremental changes and continuously validate with unit tests.
* Regularly refactor code to simplify complexity and improve maintainability.
* Conduct periodic peer reviews to integrate diverse perspectives and catch overlooked issues.

## 🛡️ Robustness & Safety Nets

* Write unit tests covering critical paths and edge cases to ensure code stability and correctness.
* Leverage static code analysis tools like Roslyn analyzers and StyleCop to maintain high-quality standards.
* Use assertions liberally to document and enforce assumptions in code logic.

## 🏗️ Universal Design Patterns

* **Factory & Abstract Factory**: For managing object creation and reducing direct dependencies.
* **Strategy Pattern**: To encapsulate varying algorithms and make behaviors interchangeable.
* **Repository Pattern**: For abstracting data layer logic and enhancing testability.
* **Dependency Injection**: Use constructor injection to clearly define dependencies and improve modularity.

## ♻️ Maintainable Code Habits

* Avoid magic numbers; define constants or configuration settings instead.
* Keep methods short (ideally fewer than 30 lines) to enhance readability and testability.
* Organize methods logically within classes (constructors first, public methods next, followed by private methods).

## 🚦 Consistent Coding Style

* Adhere to established naming conventions:

  * Methods & Variables: `PascalCase`
  * Private fields & parameters: `camelCase`
  * Constants: `UPPERCASE_WITH_UNDERSCORES`
* Consistently format your code using tools like `.editorconfig`.

## 📈 Performance Awareness

* Understand the performance implications of collections (prefer using `Dictionary` for key-value lookups over lists).
* Minimize object allocations, especially within loops or performance-critical paths.
* Favor efficient data structures and algorithms suited to the task at hand (e.g., HashSets for uniqueness checks).

## 🛠️ Continuous Learning & Reflection

* Periodically review code written previously to identify opportunities for improvement.
* Stay updated on language features and industry best practices.
* Learn from established open-source C# projects and communities.

By internalizing these universal principles, you build a solid foundation to become a proficient and thoughtful C# developer.
