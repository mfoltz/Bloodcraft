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

0. **Reassemble Argos models for each language.** Rebuild and install the translation models before running any command that depends on them (translation, token fixing, or `check-translations`). Models are not persisted across sessions; use the reconstruction snippet below to restore them.
1. **Refresh the English source.**
   `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- generate-messages`
2. **Propagate new hashes.** Copy the refreshed `English.json` entries into each `Resources/Localization/Messages/<Language>.json` while preserving numeric hashes.
   Use `--overwrite` when translating after propagating hashes so English text is replaced by its translation.
3. **Translate missing entries.**
   `python Tools/translate_argos.py Resources/Localization/Messages/<Language>.json --to <iso-code> --batch-size 100 --max-retries 3 --log-level INFO --overwrite`
   Outputs are written to `translations/<iso-code>/<timestamp>/` (override with `--run-dir`). Verify the Argos model is installed before running translations: `argos-translate --from en --to tr - < /dev/null` (substitute the target code for `tr`).
   `--log-level` helps pinpoint skipped or untranslated strings. Any hashes listed in `skipped.csv` within the run directory must be manually translated and the script re‑run to confirm they are handled.
4. **Fix tokens after manual translation.**
   `python Tools/fix_tokens.py Resources/Localization/Messages/Turkish.json --mismatches-file translations/tr/<timestamp>/token_mismatches.json`
   Run with `--check-only` (`python Tools/fix_tokens.py --check-only`) to fail fast if tokens were altered. Review `token_mismatches.json` with `python Tools/analyze_skip_report.py translations/tr/<timestamp>/skipped.csv` before proceeding. After fixing tokens, re‑run the translator to ensure no hashes remain in `skipped.csv`.
5. **Verify translations.**
   `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations --show-text`
   This ensures every hash is present and no English text remains.

For debug flags and metrics file examples, see
[Docs/Localization.md#debugging--metrics](Docs/Localization.md#debugging--metrics).

Before translating, reassemble and install the Argos model for each language under `Resources/Localization/Models` for every session:
```bash
cd Resources/Localization/Models/<LANG>
cat translate-*.z[0-9][0-9] translate-*.zip > model.zip
unzip -o model.zip
unzip -p translate-*.argosmodel */metadata.json | jq '.from_code, .to_code'
argos-translate install translate-*.argosmodel
```
Reassemble these split archives each time you start a new session; models are not persisted.
Confirm `from_code`/`to_code` in `metadata.json` match the directory's language pair. Repeat this check whenever models are added or updated so scripts reference the correct language pair.
The translation script hides `<...>` tags and `{...}` placeholders as `[[TOKEN_n]]` tokens. Tokens must be preserved but may be reordered if grammar requires; the script warns when order differs. Lines consisting only of tokens are given a `[[TOKEN_SENTINEL]]` suffix so Argos will process them. `fix_tokens.py` restores any altered tokens afterward. **DO NOT** edit text inside these tokens, tags, or variables.

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
