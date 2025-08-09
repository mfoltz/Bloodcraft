# Codex Workflow Contract

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

1. Run the generator to refresh `English.json`:
   `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- generate-messages`
2. Copy `English.json` to `<Language>.json` and translate each value while keeping numeric hashes.
3. Reassemble and install the Argos model before running translation scripts.
   For each language directory under `Resources/Localization/Models` run:
   ```bash
   cd Resources/Localization/Models/<LANG>
   cat translate-*.z[0-9][0-9] translate-*.zip > model.zip
   unzip -o model.zip
   argos-translate install translate-*.argosmodel
   ```
   Translation scripts require the model to be installed beforehand.
4. Automatically translate missing strings with Argos:
   `python Tools/translate_argos.py Resources/Localization/Messages/<Language>.json --to <iso-code> --batch-size 100 --max-retries 3 --verbose --log-file translate.log --report-file skipped.csv`
   `--verbose`, `--log-file`, and `--report-file` help pinpoint skipped or untranslated strings. The report lists skipped hashes, reasons, and the original English for manual follow-up. `translate.py` still exists but prints a deprecation warning.
5. The script hides `<...>` tags and `{...}` placeholders as `[[TOKEN_n]]` tokens. Tokens must be preserved but may be reordered if grammar requires; the script will warn when order differs. Lines consisting only of tokens are given a `[[TOKEN_SENTINEL]]` suffix so Argos will process them. The placeholder is stripped afterward and `fix_tokens.py` restores any altered tokens.
   **DO NOT** edit text inside these tokens, tags, or variables.
6. After translation, review `skipped.csv` for any hashes requiring manual work, then run the checker to ensure nothing remains in English:
   `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations`
   Use `--show-text` to also print the English string and existing translation alongside each hash.

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
