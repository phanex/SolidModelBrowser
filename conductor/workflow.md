# Workflow Specification

## Methodology: Spec-Driven Development (SDD)
1. Every feature or refactor starts as a **Track** in `conductor/tracks/<track_id>/`.
2. A Track contains:
   - `spec.md`: User-approved functional & UX requirements.
   - `plan.md`: Hierarchical phase/task list with verifiable checkboxes `[ ]`.
   - `metadata.json`: State tracking.
3. Code implementation progresses task-by-task.
4. Each phase concludes with verification (automated build or manual UI inspection).
5. GitHub Actions workflow compiles the solution on push to ensure zero build regressions.
