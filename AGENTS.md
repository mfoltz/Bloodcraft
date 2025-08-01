## Codex Keywords & Workflow

The Codex system uses the following keywords:

* **CreatePrd** – create a Product Requirements Document.
* **CreateTasks** – generate task lists from the PRD.
* **TaskMaster** – execute tasks and update their status.
* **ClosePrd** – finalize and archive completed PRDs.

0. Codex-related workflow items belong in `AGENTS.md`, NOT `README.md`!
1. Run `.codex/install.sh` once to install dependencies. This will also generate
   `Resources/secrets.json` with default development secrets.
2. Build and deploy locally with `./dev_init.sh`
3. Update message hashes when needed using `Tools/GenerateMessageTranslations`:
   `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- generate-messages`
4. Use the keywords (**CreatePrd**, **CreateTasks**, **TaskMaster**, **ClosePrd**) to manage PRDs and tasks

### Translation Workflow

1. Run the generator to refresh `English.json`:
   `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- generate-messages .`
2. Copy `English.json` to `<Language>.json` and translate each value while keeping numeric hashes.
3. Automatically translate missing strings with Argos:
   `python Tools/translate.py Resources/Localization/Messages/<Language>.json --to <iso-code> --batch-size 20 --max-retries 3`
4. The script hides `<...>` tags and `{...}` placeholders as `[[TOKEN_n]]` tokens. Lines consisting only of tokens are given a dummy `TRANSLATE` suffix so Argos will process them.
   **DO NOT** edit text inside these tokens, tags, or variables.
5. After translation, run the checker to ensure nothing remains in English:
   `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations .`

Current PRDs and task lists are stored in `.project-management/current-prd/`, while completed items are moved to `.project-management/closed-prd/`. Codex is expected to own as much as it feels capable of doing so in terms of completion, furthering goals, and being generally proactive with managing tasks to completion of state project goals. Compile with .NET 8 to build with preview features, though do note VRising runs on the .NET 6 runtime as the .csproj implies; be mindful to stay within the .NET 6 API surface to avoid surprises.

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
