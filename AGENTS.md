# AGENTS.md: Universal Principles for CSharp and General Programming

This guide provides universal, intelligent principles and patterns to master C# coding effectively, applicable across any project or repository.

## 🧠 Structured Reasoning

* **Single Responsibility Principle (SRP)**: Every class and method should have one clearly defined responsibility.
* **Explicit Intent**: Write self-descriptive methods and classes, avoiding ambiguous or overly general names.
* **Readability First**: Prioritize readability over cleverness. The intent of your code should be immediately clear to others.

## 🎯 Clarity & Explicitness

* Clearly specify the purpose, parameters, and return values of each method using XML documentation comments.
* Choose names that explicitly describe intent (e.g., `CalculateTotalPrice()` instead of `Calculate()` or `CalcTP()`).
* Favor descriptive variable names (`customerAge` rather than `ca`).

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
