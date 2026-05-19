---
name: entity-framework-nas
description: Use when changing or reviewing Entity Framework models, DbContext configuration, EF repository code, migrations, seed data, or connection strings in the NAS lab project.
---

# Entity Framework NAS

Follow this workflow for EF-related Lab 3 work:

1. Inspect the affected model, DbContext, repository, migration, seed, or configuration files before editing.
2. Check EF annotations for correctness and beginner readability.
3. Check that collection navigation properties use `virtual ICollection<T>` when the project pattern calls for EF navigation collections.
4. Check that relationship models include clear foreign key properties where needed.
5. Check that each persisted entity has an appropriate `DbSet<T>` property in the DbContext.
6. Preserve existing project structure and naming unless the current task explicitly asks for a broader change.
7. Run `dotnet build` after EF changes.
8. Explain EF changes in beginner language, focusing on tables, relationships, keys, and why the change was needed.
9. Append a concise audit summary to `lab-1/agent_log.txt` with inspected files, changed files, commands, build result, remaining risks, and next action.
