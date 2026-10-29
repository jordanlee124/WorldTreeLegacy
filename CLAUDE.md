# Worldtree Legacy

- Before planning or changing anything, read `docs/GAME_DESIGN.md`, `docs/ARCHITECTURE.md` and `docs/ROADMAP.md`. They are exports of the existing Notion specifications; follow their source links for provenance.
- Inspect the actual checkout, Git status, installed tools and repository instructions. Do not assume a Unity project or required packages exist. On 2026-10-30, the repository had no Assets/, Packages/ or ProjectSettings/ directories.
- Preserve existing work. Stage explicit intended paths, review staged changes for secrets and unintended files, and never force-reset, delete work or force-push.
- Report observed facts separately from source requirements, proposals, assumptions and blockers. Surface conflicts and missing decisions; do not invent requirements or treat roadmap estimates as commitments.
- Task 1 is audit and planning only: produce `Docs/Phase0/Audit.md` and `Docs/Phase0/ImplementationPlan.md`, including a prerequisite checklist, Phase 0 plan, risks and milestone breakdown. Do not initialize Unity, install or configure packages, or implement gameplay as part of Task 1.
- Future implementation must respect the documented Unity 6 LTS/C#/URP stack, XY gameplay plane with 3D physics, pure C# domain boundaries and offline-first scope. Networking comes later.
- Keep changes minimal and scoped to the assigned task. Validate what tooling allows and state exactly what was checked and what remains blocked.
