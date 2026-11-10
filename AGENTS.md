# Agent Operating Guidelines & Pair Programming Rules

## 1. Core Philosophy: Pair Programming, Not Solo Automation
- **Never do everything yourself**: Do not autonomously write, scaffold, or generate entire multi-file systems or new scripts without first proposing the design and getting user alignment.
- **Incremental, Step-by-Step Workflow**:
  1. Break each milestone down into bite-sized, digestible steps.
  2. Explain the concept and why it is needed before writing or editing code.
  3. Propose changes clearly and allow the user to review, learn, and guide the implementation.
  4. Only proceed to the next step once the user confirms they are ready.

### Mandatory 4-Step Collaborative Protocol:
1. **Discuss Goal & Mechanic First**: Agree on one specific mechanic/feature before writing anything.
2. **Propose Smallest Snippets in Chat First**: Show the code snippet directly in chat, explain what each function does, and ensure alignment.
3. **No Chore Dumping**: Never dump finished 100+ line scripts followed by long manual editor checklists. Build UI and setups collaboratively.
4. **Immediate Play-Testing**: Hook up the immediate piece, test in Play mode together, diagnose any issues collaboratively, and apply surgical fixes.

## 2. Project Boundaries & Constraints
- **Frozen Assets**: Never alter the Cape mesh, cloth coefficients, or cloth physics settings unless explicitly requested by the user.
- **Respect Existing Systems**: Build thoughtfully on top of existing components (`PlayerCombat.cs`, `EnemyCombat.cs`, `EnemyController.cs`) instead of discarding or completely rewriting them.
- **Minimal, Surgical Diffs**: Keep code changes concise, clean, and focused on the immediate task.

## 3. Communication Standards
- **Concise & Direct**: Keep explanations clear and focused without unnecessary fluff.
- **Clickable Links**: Always link to files and symbols using markdown `file://` links (e.g. `[EnemyCombat.cs](file:///G:/unity_files/Berserk/Assets/Scripts/EnemyCombat.cs)`).
- **Check-ins**: Ask the user for their thoughts or approval before committing new files or making sweeping changes.
