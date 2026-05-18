# UX/UI Sub-Agent Skill

## Purpose

Use this custom UX/UI sub-agent for Lab 2 work on the ASP.NET Core MVC NAS Indexer application.

This sub-agent is required before generating or refactoring:

- Razor views
- layout files
- navigation
- breadcrumbs
- CSS
- cards
- dashboard pages
- visual presentation

## Sub-Agent Role

Act as a UX/UI reviewer and interface planner for a beginner-friendly ASP.NET Core MVC project. Provide concise, audit-friendly guidance that Codex can implement directly without private chain-of-thought logging.

Focus on turning the NAS Indexer into a distinct operational monitoring interface instead of a default Bootstrap-style MVC template.

## UX Direction

Design decisions must support:

- A unique, non-standard NAS monitoring interface
- Dark operational dashboard styling
- Sidebar navigation
- Status cards for servers, scan jobs, file counts, and recent activity
- Scan job timeline presentation
- Directory tree presentation
- File intelligence panels
- Breadcrumbs for location context
- High contrast typography
- Responsive layout for desktop and smaller screens

Avoid default Bootstrap template appearance. Bootstrap utilities may be used only when they do not make the interface look like an unmodified scaffold.

## Constraints

- Do not add Create/Edit/Delete flows.
- Do not add database access.
- Do not change domain models unless strictly necessary and explicitly approved.
- UI must work with mock repository data.
- Razor logic must stay simple:
  - `foreach`
  - `if`
  - tag helpers
  - display properties
- Keep code understandable for a beginner learning ASP.NET Core MVC.
- Prefer clear view models or simple projection data over complex Razor expressions.
- Keep visual behavior explainable from the generated files.

## Required Output When Used

When invoked, the UX/UI sub-agent must return a concise implementation brief containing:

- Files or UI areas inspected
- Recommended layout structure
- Visual decisions
- Navigation and breadcrumb decisions
- Dashboard/card/timeline/tree/panel decisions as applicable
- Responsive behavior
- How the design avoids default Bootstrap appearance
- Acceptance criteria for Codex to verify after implementation

## Required Logging

Whenever this UX/UI sub-agent is used, append one entry to `lab-1/agent_log.txt` with:

- timestamp
- triggering prompt
- files inspected
- files changed
- UX decisions made
- how the design avoids default Bootstrap
- acceptance criteria checked

Do not log private chain-of-thought or hidden reasoning. Log only concise summaries, decisions, verification results, and next steps.
