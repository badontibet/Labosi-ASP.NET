# Repository Agent Instructions

These instructions apply to all future Codex and agent work in this repository.

## Logging

- Use `lab-1/agent_log.txt` as the only execution log and audit trail.
- Append all Codex prompts, visible responses, plans, file changes, command results, build results, UX/UI sub-agent invocations, review results, and checkpoint summaries to `lab-1/agent_log.txt`.
- Do not create or use separate log files, including:
  - `lab-2/codex-prompts.md`
  - `lab-2/agent-execution-log.md`
  - `lab-2/ux-agent-log.md`
  - `lab-2/review-checklist.md`
- Keep log entries concise, chronological, and audit-friendly.

## Reasoning And Privacy

- Do not log private chain-of-thought or hidden reasoning.
- Log concise summaries of decisions, assumptions, inspected files, changed files, command outputs, build results, and next steps.
- When a technical decision matters, record the practical rationale without internal deliberation.

## Implementation Workflow

- The lab must be solved through Codex and explicitly invoked agents; manual implementation code should not be written by the user.
- Before generating or changing UI/UX screens, invoke an explicit UX/UI sub-agent and append the invocation plus summary output to `lab-1/agent_log.txt`.
- Do not begin MVC implementation, domain model changes, or `Program.cs` changes unless the current user request explicitly asks for them.
- Preserve existing repository structure unless a requested task requires a scoped change.

## Review And Verification

- After each implementation step, append a checkpoint summary to `lab-1/agent_log.txt`.
- Include files inspected, files changed, commands executed, build/test results, remaining risks, and next action.
