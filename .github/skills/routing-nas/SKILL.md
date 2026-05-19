---
name: routing-nas
description: Use when adding, changing, or reviewing ASP.NET Core MVC routes, endpoint mappings, controller route patterns, readable URLs, or sitemap updates in the NAS lab project.
---

# Routing NAS

Follow this workflow for Lab 3 routing work:

1. Inspect current route configuration, controllers, actions, and any existing sitemap before editing.
2. Ensure the project has at least 4 custom routes beyond the default route when route work is requested.
3. Prefer readable URLs such as `/servers`, `/servers/1`, `/files`, `/files/1`, and `/scan-jobs`.
4. Keep route names and route templates clear enough for a beginner to trace from URL to controller action.
5. Update `sitemap.md` after route changes so the documented URLs match the implementation.
6. Avoid changing unrelated controller behavior unless the current task explicitly asks for it.
7. Run `dotnet build` after route changes.
8. Append a concise audit summary to `lab-1/agent_log.txt` with inspected files, changed files, commands, build result, remaining risks, and next action.
