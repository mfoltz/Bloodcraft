## Environment & Dependency Awareness

* **Write for the actual build environment:**  
  Assume code will be compiled in Visual Studio (or the team’s IDE) with all dependencies and references as specified in the project’s `.csproj` or package manager.
* **Reference only what’s included:**  
  Use only libraries, types, and APIs present in the project dependencies. If a new dependency is required, document how to add it.

---

## Strong Typing & Domain Modeling

* **Favor classes and structs over loose data structures:**  
  Organize related data and behavior into well-defined types rather than scattering logic in dictionaries or primitive value collections.
* **Model the problem domain:**  
  Create types that reflect real-world entities, actions, and rules to improve clarity and reduce errors.
* **Encapsulate logic:**  
  Bundle methods and validation with the data they affect, not in standalone helper methods.

---

## “Should Compile” Mindset

* **Think like the compiler:**  
  Before reviewing logic or style, ensure code would build successfully with available dependencies.  
  - Check for missing `using` directives, typos, unclosed braces, mismatched signatures, and correct namespaces.
* **Resolve errors first:**  
  Treat build errors (“red squigglies”) as top priority—fix these before anything else to maintain a buildable codebase.

---

## Patterns & Knowledge

* **Interfaces & Abstraction:**  
  Define clear contracts with interfaces; use interface segregation for narrow-focus responsibilities. ([submain.com](https://blog.submain.com/c-interface-definition-examples/) :contentReference[oaicite:2]{index=2})
* **Events & Delegates:**  
  Use events (backed by delegates) to implement the observer pattern and keep components loosely coupled. Define a `protected virtual OnX()` raiser, and pass standard `(sender, EventArgs)` parameters. ([MS Learn](https://learn.microsoft.com) :contentReference[oaicite:3]{index=3}; [StackOverflow](https://stackoverflow.com) :contentReference[oaicite:4]{index=4})
* **Multicast Delegates:**  
  Prefer delegates over interfaces when you need to support multiple subscribers without explicit coupling. ([wiki Observer :contentReference[oaicite:5]{index=5})
* **Default Interface Methods:**  
  Use sparingly—primarily for versioning library interfaces, not for routine shared behavior. ([Medium](https://medium.com) :contentReference[oaicite:6]{index=6})

---

## Design & Readability

* **Naming & Formatting:**  
  Use PascalCase for types/members, camelCase for locals/params, and ALL_CAPS for constants. Apply consistent braces, indenting, and file layout. ([Microsoft Docs](https://learn.microsoft.com) :contentReference[oaicite:7]{index=7}; [dev.to](https://dev.to) :contentReference[oaicite:8]{index=8})
* **Single Responsibility & DRY:**  
  Keep classes and methods focused. Avoid duplication by refactoring shared behavior. ([dev.to](https://dev.to) :contentReference[oaicite:9]{index=9})
* **Modern Features:**  
  Use explicit typing with modern collection initialization, new(), and such instead of `var` with descriptive names and avoid excessive shorthanding; Refactor spaghetti code as able with LINQ, `?.`, pattern matching, records, interfaces, factory patterns, delegates, and other modern C# design patterns. ([Code Maze](https://code-maze.com) :contentReference[oaicite:10]{index=10})
* **Exceptions:**  
  Catch only what you can handle; avoid broad `catch (Exception)`. Filter exceptions specifically. ([dev.to](https://dev.to) :contentReference[oaicite:11]{index=11})

---

## Quality & Continuous Learning

* **Unit Testing & DI:**  
  Use dependency injection and interfaces to support testable code. Write unit tests with xUnit, NUnit, or MSTest. ([Code Maze](https://code-maze.com) :contentReference[oaicite:12]{index=12})
* **Static Analysis & Reviews:**  
  Use analyzers (FxCop, Roslyn, ReSharper) and perform regular code reviews. Reference *Framework Design Guidelines*. ([StackOverflow](https://stackoverflow.com) :contentReference[oaicite:13]{index=13})
* **Refactor & Cleanup:**  
  Regularly revisit and refactor code to avoid tech debt and improve maintainability. ([Wikipedia](https://en.wikipedia.org) :contentReference[oaicite:14]{index=14})
* **Stay Updated:**  
  Track modern C# developments (C# 13), .NET releases, and follow trusted sources (.NET blog, Microsoft Docs, Code Maze). ([dev.to](https://dev.to) :contentReference[oaicite:15]{index=15})

---

## Specific Do's and Don'ts
  
- Do not touch or otherwise make edits to CHANGELOG.md if found or otherwise seen; do please provide succinct and relevant task reports separately as usual, versioning and other such edits involving the CHANGELOG.md or othertwise should generally be left solely to the human developer for now.
- Never use the 'private' accessibility modifier, as things will be private by default unless made public/internal/etc.

---

## Codex Keywords & Workflow

The Codex system uses the following keywords:

* **CreatePrd** – create a Product Requirements Document.
* **CreateTasks** – generate task lists from the PRD.
* **TaskMaster** – execute tasks and update their status.
* **ClosePrd** – finalize and archive completed PRDs.

`.codex/install.sh` and `dev_init.sh` handle environment setup.