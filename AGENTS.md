# Instruction for working with Rvmsharp

The rvmsharp repo is named for the rvmparser part, but in practice this also contains a lot more than the rvmsharp code.

- CadRevealComposer transforms various source 3D models into the Cognite Reveal format.
- HierarchyComposer is used to store the various metadata connected to the Reveal format.

## Coding style

Use modern dotnet coding styles where possible. Add new "unfamiliar styles" to the AGENTS.md below here when you are corected.

- Arrays can use `["A","B"]` syntax instead of `new string[] { "A", "B" }`
  - Similar with ".ToArray()" [..list] (but this is often not much more readable.)

## Code comments

- Explain non-obvious intent, assumptions, constraints, and tradeoffs in the current implementation. Avoid narrating what the code already makes clear, except for very complex code.
- Write for future readers, not implementation history. Avoid references to the "old", "original", or "previous" implementation, or explanations that something was "preserved" during a refactor. State the enduring requirement directly; keep change history and before/after comparisons in commits or spike documents.
  - You may add comments with "TODOS" etc that require attention of a reviewer detailing the choice between old and new implementation, which should be removed before merging etc.
- When a mistake reveals a lasting constraint, document that constraint rather than the mistake. Add repository-wide lessons here when appropriate.

## Public repository

- This repository is public. The datasets we work with are commonly not. Avoid committing or extracting real data into tests, and consider if benchmark reports written to spike-docs etc should be "less detailed" to allow for public viewing.
