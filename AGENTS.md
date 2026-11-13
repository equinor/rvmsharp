# Instructions for working with RvmSharp

This repository contains two related components:

- **RvmSharp** is a .NET library for importing AVEVA RVM files.
- **CadRevealComposer** is a .NET library and executable for converting RVM and other supported input formats to the Cognite Reveal format.

See README.md for info on RvmSharp, and `/CadRevealComposer.Exe/README.md` for CadRevealComposer.

## Coding style and comments

### Style

Follow modern C#/.NET conventions and the patterns already established in the repository. When a review correction reveals a reusable repository-wide convention, document it here.

- Format with `dotnet csharpier format .`. If CSharpier is unavailable, restore tools with `dotnet tool restore`.
- Prefer collection expressions for concise collection creation when they are as clear as the alternative, for example `["A", "B"]` instead of `new string[] { "A", "B" }`.
- For a target-typed array copy, `[..items]` can replace `items.ToArray()` when it improves readability; it is not automatically preferable.

### Code comments

- Explain non-obvious intent, assumptions, constraints, and tradeoffs. Avoid restating what the code makes clear.
- Write comments for future readers, not to narrate implementation history. State enduring requirements directly; keep change history and before/after comparisons in commits, separate code comments, or spike documents.
- Use `TODO` comments where you want feedback, and remove them when the follow-up is complete.
- When a correction reveals a lasting constraint, document the constraint rather than the mistake. Add repository-wide lessons here when appropriate.

### Tests

- Prefer behavior-focused test names in the `Method_When_Condition_ExpectedBehavior` style when it fits, for example `MatchAll_WhenGroupsArePlanar_ReturnsNotInstancedResults`. Name the observed effect when testing behavior that is not a return value.
- If a test documents a known bug, compromise, or unsupported feature, explain near the assertion that it captures current behavior, not a desired contract, and when it should be updated.
