---
name: reactive-memory
description: "Use the Reactive Memory MCP server as a compact durable memory layer: search first, file memories into sectors/vaults/drawers, and recall prior project decisions efficiently."
---

# Reactive Memory MCP Usage

Use this skill when a task depends on persistent project memory, prior decisions, stored facts, or continuity across Codex sessions with the Reactive Memory MCP server.

## Operating Model

Reactive Memory is a local durable memory core:

- Core: the root storage instance containing drawers, vectors, relays, tunnels, and the temporal knowledge graph.
- Sector: a top-level domain or project area, such as `api`, `build`, `docs`, `testing`, or a repository name.
- Vault: the category within a sector, such as `decisions`, `patterns`, `bugs`, `commands`, `constraints`, or `handoffs`.
- Drawer: one stored memory record with stable content and a drawer ID.

Treat the server as a retrieval and filing system, not as a transcript archive.

## Default Workflow

1. At the start of memory-aware work, call `reactivememory_status` to understand the current core.
2. For each user prompt where memory may help, call `reactivememory_react_to_prompt` before deciding or answering.
3. Search before making claims about previous decisions:
   - Use `reactivememory_search_relays` first for compact routing hints.
   - Use `reactivememory_search` for semantic retrieval of stored drawers.
   - Use `reactivememory_facts_query` for entity facts or state that can change over time.
4. Fetch full records with `reactivememory_get_drawer` only when search snippets are insufficient.
5. After meaningful work, store only compact durable outcomes with `reactivememory_add_drawer` or `reactivememory_diary_write`.

## What To Store

Store memories that will matter later:

- Decisions and their rationale.
- Repository-specific conventions, constraints, and recurring commands.
- Durable implementation patterns.
- Known bugs, fixes, migrations, or compatibility notes.
- User preferences that affect future work.
- Handoffs: current branch, incomplete work, verification status, and next action.

Keep each drawer small and self-contained. Prefer a short summary with key file paths, commands, dates, issue numbers, and outcomes over raw logs.

## What Not To Store

Do not dump:

- Full chat transcripts.
- Long terminal output.
- Whole files or large diffs.
- Secrets, credentials, tokens, private keys, or personal data not needed for future work.
- Temporary speculation that was not acted on or confirmed.

If detailed context is needed, store a pointer to the durable source, such as a file path, commit SHA, issue URL, PR URL, test command, or drawer ID.

## Filing Guidance

Choose stable taxonomy names:

- Use sectors for durable domains or projects.
- Use vaults for categories that can repeat across sectors.
- Keep names lowercase and concise, using hyphens or underscores consistently within the core.

Examples:

- `reactivememory_add_drawer(sector: "reactivememory-mcp-server", vault: "decisions", content: "...")`
- `reactivememory_add_drawer(sector: "reactivememory-mcp-server", vault: "commands", content: "...")`
- `reactivememory_add_drawer(sector: "codex", vault: "user-preferences", content: "...")`

Before creating a new sector or vault, inspect existing taxonomy with `reactivememory_list_sectors`, `reactivememory_list_vaults`, or `reactivememory_get_taxonomy`.

## Deduplication And Updates

Before storing content that may already exist, call `reactivememory_check_duplicate`.

When information changes:

- Use `reactivememory_update_drawer` for correcting or refining an existing drawer.
- Use `reactivememory_facts_invalidate` followed by `reactivememory_facts_add` for changed entity facts.
- Avoid deleting drawers unless the stored content is incorrect, unsafe, or explicitly no longer wanted.

## Recall Strategy

Use increasingly specific retrieval:

1. Search relays with the user's terms plus likely project/domain terms.
2. Search drawers with sector/vault filters when taxonomy is known.
3. Query facts for named services, repositories, people, components, or decisions.
4. Traverse or follow tunnels when a topic appears to bridge sectors or vaults.

Use `reactivememory_entities_lookup` or `reactivememory_entities_list` when a named entity may have accumulated facts or related memories.

## Writing Good Memories

A good memory is compact, factual, and reusable. Include:

- Context: what project or component it applies to.
- Decision or fact: the durable thing to remember.
- Evidence: relevant file path, command, PR, issue, or date.
- Status: done, pending, superseded, or verified.

Example drawer content:

```text
Project: ReactiveMemory.MCP.Server
Decision: Repository-local Codex skills live under skills/<name>/ with SKILL.md frontmatter containing only name and description.
Evidence: skills/reactive-memory/SKILL.md created on 2026-05-10.
Status: active
```

Prefer one focused drawer over a mixed grab bag. If multiple unrelated durable facts emerge, store them as separate drawers.

## Tool Selection Quick Reference

- `reactivememory_status`: initial health and core summary.
- `reactivememory_react_to_prompt`: first memory reaction for an incoming prompt.
- `reactivememory_search_relays`: cheap hints for where memory may live.
- `reactivememory_search`: semantic drawer search.
- `reactivememory_get_drawer`: retrieve full stored content.
- `reactivememory_add_drawer`: store durable compact memory.
- `reactivememory_check_duplicate`: avoid repeat drawers.
- `reactivememory_list_sectors`, `reactivememory_list_vaults`, `reactivememory_get_taxonomy`: inspect filing structure.
- `reactivememory_facts_query`, `reactivememory_facts_add`, `reactivememory_facts_invalidate`, `reactivememory_facts_timeline`: manage changing entity facts.
- `reactivememory_diary_write`: concise session summary after meaningful work.
- `reactivememory_traverse`, `reactivememory_find_tunnels`, `reactivememory_follow_tunnels`: navigate cross-domain memory.
