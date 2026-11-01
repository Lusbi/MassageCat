# Coding tasks: apply Ponytail by default

For **every coding task** in this repository (writing, fixing, refactoring, reviewing, or making dependency decisions), automatically read and follow `.agents/skills/ponytail/SKILL.md` before making code changes. Use its **full** mode by default. Do not require a manual `/ponytail` or `$ponytail` command. A user's explicit request for `lite`, `ultra`, `off`, or normal mode takes priority.

Keep the smallest **complete** change. Reuse existing project patterns and Unity/C# APIs where appropriate; avoid needless dependencies and future-proof abstractions, but never sacrifice correctness, security, validation, data safety, platform compatibility, or appropriate testing. At the end of a coding task, briefly report skipped checks and any residual risks.

Do not apply Ponytail to unrelated non-coding tasks.
