---
name: ux-list-pages-nas
description: Use when reviewing or improving Index and Details Razor pages, list/detail UI, strongly typed views, tag-helper links, or beginner-readable MVC page UX in the NAS lab project.
---

# UX List Pages NAS

Follow this workflow for Lab 3 Razor list/detail page work:

1. Inspect the relevant controller actions, view models, and Razor pages before editing.
2. Use strongly typed views for Index and Details pages.
3. Use ASP.NET Core tag helpers for links instead of hard-coded MVC URLs.
4. Prefer simple, beginner-readable UI that makes model data, relationships, and navigation obvious.
5. Use partial views only when real reuse exists across pages.
6. Do not add Create, Edit, or Delete UI unless the current task explicitly requests those actions.
7. Keep page changes scoped to the requested Index or Details experience.
8. Run `dotnet build` when Razor or related controller/view-model changes are made.
9. Append a concise UX review summary to `lab-1/agent_log.txt` with inspected files, changed files, commands, build result, remaining risks, and next action.
