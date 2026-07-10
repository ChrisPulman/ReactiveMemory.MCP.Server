# ReactiveMemory MCP Server

<!-- mcp-name: io.github.chrispulman/reactivememory-mcp-server -->

<div align="center">
  <img src="Images/reactive-memory-logo.png" style="width:25%;" />
</div>

ReactiveMemory MCP Server gives AI assistants a persistent, queryable, locally-stored memory system backed by vector search, a temporal knowledge graph, and a vault-structured content store. 
It is designed to be the durable external memory layer for agents, copilots, and AI workflows that need to remember, reason over, and navigate accumulated knowledge across sessions.

It is implemented in C# on `net10.0` using `ModelContextProtocol` `1.3.0`.

## Quick Install

Click to install in your preferred environment:

[![VS Code - Install Reactive Memory MCP](https://img.shields.io/badge/VS_Code-Install_ReactiveMemory_MCP-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://vscode.dev/redirect/mcp/install?name=reactivememory-mcp-server&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22CP.ReactiveMemory.Mcp.Server%401.*%22%2C%22--yes%22%5D%7D)
[![VS Code Insiders - Install Reactive Memory MCP](https://img.shields.io/badge/VS_Code_Insiders-Install_ReactiveMemory_MCP-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://insiders.vscode.dev/redirect/mcp/install?name=reactivememory-mcp-server&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22CP.ReactiveMemory.Mcp.Server%401.*%22%2C%22--yes%22%5D%7D&quality=insiders)
[![Visual Studio - Install Reactive Memory MCP](https://img.shields.io/badge/Visual_Studio-Install_ReactiveMemory_MCP-5C2D91?style=flat-square&logo=visualstudio&logoColor=white)](https://vs-open.link/mcp-install?%7B%22name%22%3A%22CP.ReactiveMemory.Mcp.Server%22%2C%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22CP.ReactiveMemory.Mcp.Server%401.*%22%2C%22--yes%22%5D%7D)

Note:
- These install links are prepared for the intended NuGet package identity `CP.ReactiveMemory.Mcp.Server`.
- If the latest package has not been published yet, use the manual source-build configuration below.

Manual MCP configuration using NuGet:

```json
{
  "mcpServers": {
    "reactivememory-mcp-server": {
      "type": "stdio",
      "command": "dnx",
      "args": [
        "CP.ReactiveMemory.Mcp.Server@1.*",
        "--yes"
      ]
    }
  }
}
```

## Getting started

1. Install the MCP server with the quick-install link or the `dnx` configuration above.
2. Restart or reload your MCP-capable client so it discovers the `reactivememory_*` tools.
3. Ask your assistant to call `reactivememory_status` to confirm the core path, current taxonomy, and fallback-safe local model status.
4. For memory-aware work, have the assistant call `reactivememory_memory_automanage` or `reactivememory_react_to_prompt` at the start of each user prompt, then use `reactivememory_context_pack`, `reactivememory_memory_get_relevant`, `reactivememory_search_relays`, `reactivememory_search`, or `reactivememory_facts_query` before answering from memory.
5. After meaningful work, store durable outcomes with `reactivememory_memory_add`, `reactivememory_add_drawer`, or `reactivememory_diary_write` rather than saving full transcripts. Use `reactivememory_memory_classify` / `reactivememory_memory_should_store` to audit decisions, `reactivememory_memory_summarise` to compress long-term groups, and `reactivememory_memory_prune` for dry-run pruning recommendations unless `apply=true` is explicitly requested.

Minimal first prompt:

```text
Call reactivememory_status, then react to this prompt with reactivememory_react_to_prompt. Search for any prior memories about this repository before planning the work.
```

## What ReactiveMemory helps with

Without persistent memory, every AI session starts from zero. ReactiveMemory solves this by giving agents a place to:

- **Store** any content — code snippets, decisions, meeting notes, design facts — under an organized sector/vault taxonomy
- **Search** across everything stored using semantic vector similarity, not just keyword matching
- **Reason** over time using a temporal knowledge graph that tracks when facts became true and when they were superseded
- **Navigate** the stored graph to discover how vaults relate to each other across sectors
- **Log** agent activity in per-agent diaries that persist session summaries across restarts
- **Deduplicate** incoming content before storing it to keep the core lean
- **Audit** every mutation via a JSONL write-ahead log

This server is intended for:
- AI coding agents that need to remember project context, architectural decisions, and code patterns across sessions
- Copilot workflows where facts discovered in one session need to survive to the next
- Long-running agent pipelines where temporal knowledge (facts with lifetimes) matters
- Multi-agent setups where a shared knowledge core is needed

## Core concepts

ReactiveMemory organises everything into a four-level hierarchy:

| Term | Meaning |
|------|---------|
| **Core** | The root storage instance — all drawers, vector embeddings, and the knowledge graph live here |
| **Sector** | A top-level grouping, roughly equivalent to a domain or project area (e.g. `auth`, `payments`, `ui_layer`) |
| **Vault** | A subdivision within a sector that describes a content category (e.g. `decisions`, `patterns`, `bugs`) |
| **Drawer** | A single stored content record filed into a sector and vault, with a stable SHA-256-derived ID |

**Relays** are internal metadata annotations on drawers that describe the relay channel (e.g. `relay_diary`, `relay_code`). They are used for graph construction and traversal but are not directly exposed as a tool parameter.

### Storage architecture

| Layer | Technology | Default location |
|-------|------------|-----------------|
| Drawer store | JSON flat file | `~/.reactivememory/core/reactivememory_drawers.json` |
| Relay store | Compact relay/closet-style vector index | `~/.reactivememory/core/reactivememory_relays.vectors.json` |
| Vector store | JSON + local embedding vectors | `~/.reactivememory/core/` |
| Knowledge graph | SQLite (WAL mode) | `~/.reactivememory/core/knowledge_graph.sqlite3` |
| Explicit tunnels | JSON flat file | `~/.reactivememory/core/explicit_tunnels.json` |
| Hook/checkpoint state | JSON flat files | `~/.reactivememory/hook_state/` |
| Write-ahead log | JSONL append-only file | `~/.reactivememory/wal/write_log.jsonl` |

All storage is local and file-based — no external services or API keys are required.

### Retention and performance design

ReactiveMemory is designed to keep a project memory core useful for the lifetime of a project without turning it into a transcript dump.

- **Compact writes:** `reactivememory_react_to_prompt` and `reactivememory_diary_write` should store concise summaries, decisions, facts, commands, paths, and outcomes rather than whole chats or terminal logs.
- **Two retrieval layers:** `reactivememory_search_relays` searches compact routing hints first; `reactivememory_search` retrieves the full drawer content when the hint layer is not enough.
- **Duplicate control:** exact normalized text matching is checked before vector candidates, and duplicate decisions require high lexical overlap to avoid storing repeated prompts or misclassifying unrelated memories.
- **Fast local stores:** JSON stores are loaded into memory once, indexed by drawer/vector ID, and flushed atomically on mutation instead of rereading the full file for every query.
- **Efficient search:** vector queries scan the in-memory index and keep only the top results during the scan instead of sorting every candidate.
- **Stable local embeddings:** the default embedding provider uses deterministic token hashing with a wider vector space for repeatable local recall across sessions.
- **Temporal truth:** changing facts should be invalidated and re-added in the knowledge graph, preserving what used to be true without bloating drawer memory.

## Agent protocol

When this server is active, agents should follow the **ReactiveMemory Protocol**:

1. Call `reactivememory_status` on initialisation to load the current core summary and receive operational guidance.
2. On every user prompt, call `reactivememory_react_to_prompt` first so ReactiveMemory can recall related memories, detect duplicates, learn entities, and checkpoint the prompt automatically.
3. Before answering questions about persisted facts, call `reactivememory_facts_query`, `reactivememory_search`, or `reactivememory_search_relays` and use retrieved data instead of assumptions.
4. When facts change, call `reactivememory_facts_invalidate` for the old state and `reactivememory_facts_add` for the replacement.
5. After a meaningful interaction, call `reactivememory_diary_write` to persist a concise session record.
6. Use `reactivememory_list_sectors`, `reactivememory_list_vaults`, `reactivememory_get_taxonomy`, `reactivememory_traverse`, `reactivememory_find_tunnels`, `reactivememory_create_tunnel`, and `reactivememory_follow_tunnels` for discovery and navigation.
7. Call `reactivememory_check_duplicate` before storing repeated content when deduplication accuracy matters.
8. Use `reactivememory_entities_lookup` and `reactivememory_entities_list` to inspect people/projects learned from prompt reactions.
9. Use `reactivememory_hook_settings`, `reactivememory_memories_filed_away`, and `reactivememory_reconnect` when operating with automated checkpointing or external store changes.

## Codex and agent skill

This repository includes a Codex skill at [`skills/reactive-memory/SKILL.md`](skills/reactive-memory/SKILL.md). It gives Codex and other compatible agents a compact operating guide for using ReactiveMemory well:

- search before relying on remembered facts
- use Core, Sector, Vault, and Drawer concepts consistently
- store durable memories as small, factual records
- avoid full transcript, log, diff, secret, or temporary speculation dumps
- use duplicate checks and fact invalidation before creating redundant or stale records
- write concise handoff and diary memories after meaningful work

To install the skill for Codex on Windows:

```powershell
$source = ".\skills\reactive-memory"
$target = Join-Path $env:USERPROFILE ".codex\skills\reactive-memory"
New-Item -ItemType Directory -Force $target | Out-Null
Copy-Item -Path "$source\*" -Destination $target -Recurse -Force
```

Agents that support repository-local skills can use the file directly. Agents that do not support skills can still follow the workflow in `SKILL.md` as their memory protocol.

## Available MCP tools

### Core & taxonomy

#### `reactivememory_status`
Returns a summary of the entire core: total drawer count, per-sector counts, per-vault counts, the configured core path, fallback-safe local model/NPU status, and the full agent protocol instructions.

**When to use:** Call this first on every session start. The response tells the agent what is stored, whether optional local model acceleration is active or falling back to hash embeddings, and how to use the server correctly.

---

#### `reactivememory_local_model_status`
Returns only the optional local model/NPU runtime status. It is safe to call when ONNX Runtime, DirectML/QNN providers, or model files are not installed; absent dependencies are reported as status/messages instead of startup failures.

**When to use:** Use when enabling or troubleshooting local embeddings. The default should report `enabled=false`, `activeEmbeddingProvider="Hash"`, and CPU/hash fallback active.

---

#### `reactivememory_list_sectors`
Returns all sectors that exist in the core with the number of drawers in each.

**When to use:** Use when you need to know what top-level domains are present — for example, to decide which sector to search or store into, or to give a user an overview of what topics the memory covers.

---

#### `reactivememory_list_vaults`
Returns vaults and their drawer counts. Accepts an optional `sector` parameter to narrow the listing to one sector.

**Parameters:**
- `sector` *(optional)* — filter to a specific sector; omit to list all vaults across all sectors

**When to use:** Use after listing sectors to drill down into the vault-level structure. Helpful for building navigation menus or deciding where new content belongs.

---

#### `reactivememory_get_taxonomy`
Returns the full three-level tree: sector → vault → drawer count.

**When to use:** Use when you need a complete structural overview of the core in one call — for example, to present an agent with a map of everything stored before starting a complex query.

---

#### `reactivememory_get_aaak_spec`
Returns the AAAK (Authentication, Authorization, Accounting Key) dialect specification.

The AAAK format is a compact, parseable, append-only record format for security-relevant identity and access state. Fields include `principal`, `credential_id`, `permission_set`, `scope`, `observed_at`, `expires_at`, `status`, and `evidence`.

**When to use:** Use when an agent needs to store, retrieve, or reason about security-related facts (identity, permissions, access tokens, role grants) in a structured and indexable form. AAAK entries are designed to be stored as drawer content and invalidated cleanly when the security state changes.

---

### Search & relays

#### `reactivememory_search`
Performs a semantic vector search across stored drawers. Results are ranked by similarity to the query text and include each matching `drawerId` so callers can immediately fetch the full record with `reactivememory_get_drawer`.

**Parameters:**
- `query` — the search string
- `limit` *(default: 5)* — maximum number of results to return
- `sector` *(optional)* — restrict results to one sector
- `vault` *(optional)* — restrict results to one vault

**When to use:** Use this as the primary retrieval mechanism whenever an agent needs to find relevant stored content. Prefer this over asking the user to repeat context that may already be in the core. Also use it before writing new drawers to check if closely related content already exists.

---

#### `reactivememory_search_relays`
Searches the compact relay layer — to retrieve short routing and topical hints derived from stored drawers.

**Parameters:**
- `query` — the search string
- `limit` *(default: 5)* — maximum number of results to return
- `sector` *(optional)* — restrict results to one sector
- `vault` *(optional)* — restrict results to one vault

**When to use:** Use this before or alongside `reactivememory_search` when you want compact, high-signal hints about where relevant content is likely filed. This is especially useful for prompt reaction flows and for cheaply recalling likely related areas of the core before running broader retrieval.

---

#### `reactivememory_context_pack`
Runs compact relay search and full semantic search concurrently, deduplicates by drawer ID, preserves source provenance, and returns a sector-diverse context pack under strict item and character budgets.

**Parameters:** `query`, `maxItems` (default 8), `maxCharacters` (default 6000), `searchLimitPerSource` (default 12), and optional `sector` / `vault` filters.

**When to use:** Prefer this one-call retrieval path for multi-agent handoffs and cross-project work where a bounded, high-signal context window is more useful than multiple search round trips.

---

#### `reactivememory_check_duplicate`
Checks whether content already exists in the core using configurable similarity threshold matching.

**Parameters:**
- `content` — the text to check
- `threshold` *(default: 0.9)* — similarity score above which a match is considered a duplicate

**When to use:** Call before `reactivememory_add_drawer` when the content might already be stored. A threshold of `0.9` catches near-identical content; lower values (e.g. `0.7`) will flag looser semantic overlaps. Returns whether a duplicate was found, the matching drawer IDs, and their similarity scores.

---

### Cognitive memory management

#### `reactivememory_memory_classify`
Classifies a candidate message before storage as `personal_preference`, `long_term_fact`, `short_term_context`, `irrelevant`, or `sensitive_do_not_store`. Sensitive and irrelevant classifications are conservative and return `shouldStore=false`.

#### `reactivememory_memory_add`
`memory.add` equivalent. Runs classification first, rejects sensitive/do-not-store and irrelevant content, then stores accepted memories with category metadata and vector indexes.

#### `reactivememory_memory_get_relevant`
`memory.getRelevant` equivalent. Searches managed memories and returns category metadata alongside drawer IDs and similarity scores.

#### `reactivememory_memory_summarise`
`memory.summarise` equivalent. Builds a local summarisation prompt and uses the optional local model runtime when linked; otherwise it falls back to deterministic local compression so no NPU/model is required.

#### `reactivememory_memory_prune`
`memory.prune` equivalent. Produces auditable duplicate/irrelevant pruning recommendations by default. Destructive deletion occurs only when `apply=true`, and each delete is logged through the write-ahead log.

#### `reactivememory_memory_automanage`
`memory.automanage` equivalent. Runs classify → skip sensitive/irrelevant → embed/store with category/vector metadata → summarise-if-large → pruning recommendation. It remains offline/private by default and does not require local model hardware.

---

### Background project catalog and migration

#### `reactivememory_catalog_project`
Queues incremental project mining on a bounded, single-consumer background worker and returns a job ID immediately. The worker loads the mining config, safely streams project files, skips `.git`, `bin`, `obj`, directory links, oversized files, and binary content, and applies a per-job timeout.

Use `reactivememory_catalog_status` to poll the job and `reactivememory_catalog_cancel` to cancel queued or running work. Completed job metadata has bounded retention, while mined memories remain durable.

#### `reactivememory_migrate_legacy_storage`
Inspects legacy drawer and relay vector indexes in dry-run mode by default. With `apply=true`, it rebuilds missing, stale, or incompatible vectors from the drawer source of truth. Drawer JSON, orphan vectors, and the SQLite knowledge graph are preserved, and rerunning is idempotent.

---

### Drawer storage

#### `reactivememory_add_drawer`
Stores a piece of content into the core under a specified sector and vault. The drawer is assigned a stable SHA-256-derived ID computed from the sector, vault, and the first 100 characters of content, so re-adding identical content is idempotent.

**Parameters:**
- `sector` — the sector to file under (no spaces or path separators)
- `vault` — the vault within that sector
- `content` — the text content to store
- `sourceFile` *(optional)* — the originating file path for traceability
- `addedBy` *(optional)* — the agent or system that created the entry (defaults to `"mcp"`)

**When to use:** Use whenever new knowledge, context, decisions, code patterns, or facts should be persisted. Pair with `reactivememory_check_duplicate` if deduplication matters. Every add is also recorded in the write-ahead log.

---

#### `reactivememory_get_drawer`
Fetches a single drawer by ID and returns its full content plus stored metadata.

**Parameters:**
- `drawerId` — the full drawer ID

**When to use:** Use when you already know the drawer identifier from search, duplicate detection, tunnel traversal, or prior storage and need the full verbatim record.

---

#### `reactivememory_list_drawers`
Lists stored drawers with optional sector/vault filters and paging support.

**Parameters:**
- `sector` *(optional)* — restrict to one sector
- `vault` *(optional)* — restrict to one vault
- `limit` *(default: 20)* — maximum number of entries to return
- `offset` *(default: 0)* — paging offset

**When to use:** Use for deterministic inspection of stored records, debugging a filing strategy, or browsing a vault without using semantic search.

---

#### `reactivememory_update_drawer`
Updates an existing drawer's content and/or filing location while preserving its identity.

**Parameters:**
- `drawerId` — the full drawer ID
- `content` *(optional)* — replacement content
- `sector` *(optional)* — replacement sector
- `vault` *(optional)* — replacement vault

**When to use:** Use when a drawer should stay the same logical record but its text or filing location needs correction. This also refreshes the vector and relay indexes.

---

#### `reactivememory_delete_drawer`
Removes a drawer from both the drawer store and the vector index by its ID.

**Parameters:**
- `drawerId` — the full drawer ID (returned by `reactivememory_add_drawer` or search results)

**When to use:** Use when stored content is outdated, incorrect, or superseded and should no longer appear in search results. For knowledge graph facts, prefer `reactivememory_facts_invalidate` instead of deletion, since invalidation preserves the historical record.

---

### Temporal knowledge graph

The knowledge graph stores facts as subject–predicate–object triples with optional `valid_from` and `valid_to` dates. This makes it possible to reason about what was true at a given point in time, track changes over time, and query the current state of any entity.

The SQLite store is backed by WAL-mode journalling and is indexed on subject, object, and predicate for fast entity lookups.

#### `reactivememory_facts_add`
Adds a new fact triple to the knowledge graph.

**Parameters:**
- `subject` — the entity the fact is about (e.g. `"AuthService"`)
- `predicate` — the relationship (e.g. `"uses_database"`, `"owned_by"`)
- `object` — the value or related entity (e.g. `"PostgreSQL"`)
- `validFrom` *(optional)* — ISO 8601 date string from which the fact is true (defaults to now)
- `sourceVault` *(optional)* — the vault or source identifier for traceability

**When to use:** Use whenever a new factual assertion needs to be persisted — architectural decisions, ownership, technology choices, environment states, dependency relationships. Predicates are normalised to `snake_case` automatically.

---

#### `reactivememory_facts_invalidate`
Marks an existing fact triple as no longer true by setting its `valid_to` date.

**Parameters:**
- `subject` — the entity the fact was about
- `predicate` — the relationship being ended
- `object` — the value being superseded
- `ended` *(optional)* — the ISO 8601 date when the fact stopped being true (defaults to today)

**When to use:** Use whenever a previously recorded fact changes — for example, when a service changes its database, when ownership transfers, or when a configuration value is updated. Always pair with a follow-up `reactivememory_facts_add` for the replacement state. This preserves the full change history.

---

#### `reactivememory_facts_query`
Retrieves all facts from the knowledge graph related to a named entity.

**Parameters:**
- `entity` — the subject or object entity name to look up
- `asOf` *(optional)* — an ISO 8601 date to query the state at a point in time (omit for current state)
- `direction` *(default: `"both"`)* — `"outgoing"` for facts where the entity is the subject, `"incoming"` for facts where it is the object, `"both"` for all

**When to use:** Use before answering any question about a specific entity that may have recorded facts. This is the primary retrieval tool for knowledge-graph-backed reasoning. Provide `asOf` when the question is about a past state.

---

#### `reactivememory_facts_timeline`
Returns all facts in chronological order, optionally filtered to a single entity.

**Parameters:**
- `entity` *(optional)* — restrict the timeline to one entity; omit for the full graph timeline

**When to use:** Use when you need to understand the history of changes to an entity or to audit all knowledge graph mutations in order. Useful for explaining how a system evolved over time.

---

#### `reactivememory_facts_stats`
Returns knowledge graph statistics: total entity count, total triple count, currently active triple count, and expired triple count.

**When to use:** Use for health checks, to understand how much knowledge is stored and how many facts have been superseded. Complements `reactivememory_status` for a complete picture of both storage layers.

---

### Graph traversal & tunnels

ReactiveMemory builds an implicit vault graph by analysing which drawers share sectors. Vaults that have drawers in multiple sectors become **tunnels** — natural bridges between domains. In addition, ReactiveMemory supports **explicit tunnels**: manually declared cross-sector links that let you encode intentional bridges between vaults and optionally anchor them to specific drawers.

#### `reactivememory_traverse`
Walks the vault graph using breadth-first traversal starting from a named vault.

**Parameters:**
- `startVault` — the vault name to start from
- `maxHops` *(default: 2)* — maximum traversal depth (capped at 50 results)

Returns each visited vault with its hop distance, sector membership, drawer count, and the shared sectors that connect it to the path. If the start vault is not found, a fuzzy-match suggestion is included in the response.

**When to use:** Use when you want to discover which areas of the knowledge core are semantically connected to a given vault. Useful for understanding how topics relate, for generating navigation suggestions, or for building context around a starting point before conducting a broader search.

---

#### `reactivememory_find_tunnels`
Identifies vaults that contain drawers in more than one sector, making them cross-domain bridges.

**Parameters:**
- `sectorA` *(optional)* — only return tunnels that touch this sector
- `sectorB` *(optional)* — only return tunnels that also touch this sector

**When to use:** Use when you need to find the conceptual intersections in the knowledge core — for example, to locate shared infrastructure vaults that appear in both a `backend` sector and a `devops` sector. Results are sorted by drawer count descending (most connected first).

---

#### `reactivememory_graph_stats`
Returns overall core graph statistics: total vault node count, total edge count, and vault counts per sector.

**When to use:** Use for a structural health check of the graph, or to understand how densely sectors and vaults are connected.

---

#### `reactivememory_create_tunnel`
Creates or updates an explicit tunnel between two sector/vault pairs.

**Parameters:**
- `sourceSector`
- `sourceVault`
- `targetSector`
- `targetVault`
- `tunnelType` *(default: `"reference"`)* — classification of the relationship
- `description` *(optional)* — human-readable explanation
- `createdBy` *(optional)* — actor that created or updated the tunnel
- `sourceDrawerId` *(optional)* — optional anchor drawer on the source side
- `targetDrawerId` *(optional)* — optional anchor drawer on the target side

**When to use:** Use when a cross-sector relationship is intentional and should not rely only on implicit co-occurrence.

---

#### `reactivememory_list_tunnels`
Lists explicit tunnels, optionally filtered by sector.

**Parameters:**
- `sector` *(optional)* — only show tunnels touching the given sector

**When to use:** Use to inspect manually curated bridges across the core or to build navigation/UI around explicit cross-domain relationships.

---

#### `reactivememory_delete_tunnel`
Deletes an explicit tunnel by ID.

**Parameters:**
- `tunnelId` — the explicit tunnel identifier

**When to use:** Use when an intentional bridge is obsolete or was created in error.

---

#### `reactivememory_follow_tunnels`
Follows explicit tunnels starting from a sector/vault and returns the connected tunnels plus any anchored drawers.

**Parameters:**
- `sector` — start sector
- `vault` — start vault

**When to use:** Use when you want deterministic, human-curated navigation instead of relying only on implicit graph traversal.

---

### Agent diary, hooks, and prompt reaction

Each agent maintains its own named diary — a chronological log of session notes stored as a dedicated vault under a per-agent sector.

#### `reactivememory_diary_write`
Writes a new diary entry for a named agent.

**Parameters:**
- `agentName` — the agent's identifier (used as the sector name)
- `entry` — the diary content (session summary, decision log, notes)
- `topic` *(optional)* — a topic tag for the entry (defaults to `"general"`)

**When to use:** Call at the end of every significant session to persist a summary of what was done, decided, or discovered. This gives the agent continuity across restarts and enables later retrieval of what was worked on and when.

---

#### `reactivememory_diary_read`
Reads the most recent diary entries for a named agent.

**Parameters:**
- `agentName` — the agent's identifier
- `lastN` *(default: 10)* — the number of most recent entries to return

**When to use:** Call at the start of a session to recall what the agent did previously, or to give a user a summary of recent agent activity. Returns entries in reverse-chronological order with timestamps, topics, and content.

---

#### `reactivememory_hook_settings`
Gets or updates hook/checkpoint behavior settings.

**Parameters:**
- `silentSave` *(optional)* — whether checkpointing should stay quiet by default
- `desktopToast` *(optional)* — whether desktop notifications are preferred

**When to use:** Use when integrating ReactiveMemory into a client that performs background checkpointing and you want that behavior configurable rather than hard-coded.

---

#### `reactivememory_memories_filed_away`
Acknowledges the latest silent checkpoint and returns a short summary of what was saved.

**When to use:** Use after automatic prompt or diary filing when the client wants a compact acknowledgement without surfacing the full save operation inline.

---

#### `reactivememory_entities_lookup`
Looks up a person or project learned by prompt reaction or mining.

**Parameters:**
- `name` — the entity name to resolve

**When to use:** Use when a prompt mentions a person or project and the agent needs to know whether ReactiveMemory has already catalogued it.

---

#### `reactivememory_entities_list`
Lists all people and projects currently learned in the prompt-reactive entity registry.

**When to use:** Use during session start, project handoff, or diagnostics to inspect the entity catalogue built from prior prompts and mined interactions.

---

#### `reactivememory_reconnect`
Reinitializes local stores after external modifications.

**When to use:** Use when another process has written directly to ReactiveMemory's local files and the client wants to refresh without restarting the MCP host.

---

#### `reactivememory_react_to_prompt`
Reacts to the current user prompt by recalling related memories, checking for duplicate prompt storage, learning entities, filing the prompt into the core if needed, and writing a checkpoint summary.

**Parameters:**
- `prompt` — the current raw user prompt
- `agentName` *(optional)* — agent/session identifier to associate with the prompt flow
- `sector` *(optional)* — override filing sector for prompt storage
- `vault` *(optional)* — override filing vault for prompt storage

**When to use:** This should be the first call for every incoming user prompt when you want ReactiveMemory to operate as an always-on memory layer. It is the server-side primitive that enables automatic memory maximization across conversations.

---

## Automatic prompt reaction integration

To make ReactiveMemory operate as an always-on memory layer, your MCP client or agent host should call `reactivememory_react_to_prompt` for **every incoming user prompt** before planning or answering.

That gives the server a chance to:
- recall related memories
- detect duplicate prompt storage
- learn entities from the prompt
- store prompt context when useful
- write a compact checkpoint summary for later acknowledgement

### Recommended flow per user prompt

1. Receive raw user prompt.
2. Call `reactivememory_react_to_prompt` with that raw prompt.
3. Use the returned related memories plus normal retrieval tools (`reactivememory_search`, `reactivememory_search_relays`, `reactivememory_facts_query`) to ground the answer.
4. After the interaction, optionally call `reactivememory_diary_write` for a session-level summary.
5. Optionally call `reactivememory_memories_filed_away` when you want a lightweight acknowledgement of the last automatic checkpoint.

### Minimal host-side pseudocode

```text
on_user_prompt(prompt):
    prompt_context = call_tool("reactivememory_react_to_prompt", {
        "prompt": prompt,
        "agentName": "copilot"
    })

    grounded_search = call_tool("reactivememory_search", {
        "query": prompt,
        "limit": 5
    })

    answer = llm(prompt, memory=[prompt_context, grounded_search])
    return answer
```

### JSON-RPC / MCP-style example

```json
{
  "method": "tools/call",
  "params": {
    "name": "reactivememory_react_to_prompt",
    "arguments": {
      "prompt": "How did we wire JWT refresh token rotation last sprint?",
      "agentName": "copilot"
    }
  }
}
```

### C# host integration example

```csharp
public async Task<PromptContext> PreparePromptAsync(IMcpClient client, string prompt, CancellationToken cancellationToken)
{
    var reaction = await client.CallToolAsync(
        "reactivememory_react_to_prompt",
        new
        {
            prompt,
            agentName = "copilot",
        },
        cancellationToken);

    var related = await client.CallToolAsync(
        "reactivememory_search",
        new
        {
            query = prompt,
            limit = 5,
        },
        cancellationToken);

    return new PromptContext(reaction, related);
}
```

### Practical guidance

- **Do not** wait until the end of the session to call `reactivememory_react_to_prompt` if your goal is maximum continuity.
- **Do** pass the raw user prompt, not a summary, so entity detection and duplicate checks work correctly.
- **Do** keep using normal search and facts tools after prompt reaction; `reactivememory_react_to_prompt` is the automatic entrypoint, not a replacement for deeper retrieval.
- **Do** use `reactivememory_hook_settings` if your host supports background checkpointing preferences.

---

## Additional capabilities (library API)

The `ReactiveMemory.MCP.Core` library also exposes higher-level mining and entity detection APIs for use outside the MCP tool surface:

### ProjectMiner
Scans a source directory and files all discovered content into the core. It skips `bin`, `obj`, `.git`, inaccessible and linked directories, ignores oversized/binary files, supports cooperative cancellation, routes files via a configurable `VaultRouter`, and maintains a file index to skip unchanged files. The MCP catalog tools run this work through a bounded background queue so tool requests never wait for a repository scan.

### ConversationMiner
Parses conversation transcripts, normalises them, chunks them into segments, classifies each segment into a vault, and stores each chunk as a drawer. This is useful for bulk-importing chat history or support transcripts into the memory core.

### EntityDetector
Applies heuristic regex-based entity extraction to raw text to identify people, projects, and uncertain candidates. Useful for pre-processing content before storage to tag drawers with detected entities.

### EntityRegistry
Provides a lookup layer for previously detected entities, allowing agents to resolve entity names to stored knowledge graph records.

---

## Solution layout

```
src/
├── ReactiveMemory.MCP.Core/        # Storage, services, graph, KG, mining, entities
├── ReactiveMemory.MCP.Server/      # MCP host, tool registration, Program.cs
├── ReactiveMemory.MCP.Tests/       # Unit and integration tests
└── ReactiveMemory.MCP.Server.slnx  # Solution file
```

---

## Configuration

The server stores all data under `~/.reactivememory/` by default. No environment variables or connection strings are required.

| Path | Purpose |
|------|---------|
| `~/.reactivememory/core/` | Drawer JSON store, relay index, explicit tunnels, and vector files |
| `~/.reactivememory/core/knowledge_graph.sqlite3` | Temporal knowledge graph |
| `~/.reactivememory/hook_state/` | Hook settings and latest checkpoint acknowledgement state |
| `~/.reactivememory/wal/write_log.jsonl` | Append-only audit log of all mutations |

Configuration can be overridden by providing a `ReactiveMemoryOptions` instance when constructing `ReactiveMemoryService` programmatically.

### Optional local model/NPU runtime

ReactiveMemory remains offline and fallback-safe by default. Local model support is opt-in and no model downloads, cloud calls, ONNX Runtime assemblies, DirectML/QNN/OpenVINO/Core ML providers, or model files are required for the default server.
NPU is used by ReactiveMemory as an optional support model, not as the main agent runtime: MCP clients such as Codex, GitHub Copilot, Claude Code, Cursor, Windsurf, Visual Studio, VS Code, or Claude Desktop continue to run their own agent models while ReactiveMemory optionally accelerates local embedding, classification, and summarisation support work.

The built-in NuGet/stdio server ships with deterministic hash embeddings enabled and local model execution disabled. To run an actual NPU-backed LLM or embedding model, host ReactiveMemory with a concrete `ILocalModelRuntime` implementation that wraps your local inference stack, for example ONNX Runtime, ONNX Runtime GenAI, OpenVINO, Core ML, MLX, or another offline backend. The core APIs are intentionally provider-neutral so the server can run on Windows, Linux, and macOS without taking a hard dependency on any one accelerator runtime.

#### What the local model is used for

- Embeddings: vector relevance search and `reactivememory_memory_get_relevant`.
- Classification: `reactivememory_memory_classify`, `reactivememory_memory_should_store`, and the first stage of `reactivememory_memory_automanage`.
- Summarisation: `reactivememory_memory_summarise` and category compaction when automanage thresholds are reached.
- Pruning decisions: duplicate, irrelevant, stale, and contradiction recommendations for `reactivememory_memory_prune`.

The local model never has to be the same model as the agent. A small embedding model such as `all-MiniLM-L6-v2`, `e5-small`, or `bge-small` is usually enough for retrieval. A compact instruction model such as Phi-3 Mini, Llama 3.x 8B/3B/1B quantised, or another ONNX/INT4 model can be used for summarisation/classification when your hardware supports it.

#### Default disabled settings

```csharp
var options = new ReactiveMemoryOptions();
// options.LocalModel.Enabled == false
// options.LocalModel.EmbeddingProvider == "Hash"
// options.LocalModel.AllowCloud == false
// options.LocalModel.DownloadAllowed == false
// options.LocalModel.AllowCpuFallback == true
// options.LocalModel.ProviderPreference == ["CPU"]
```

With these defaults:

- `reactivememory_local_model_status` reports `enabled=false`.
- `reactivememory_status` reports the same local model block.
- `SimpleTextEmbeddingProvider` remains active (`ProviderId="Hash"`, `Version=1`, `Dimensions=512`).
- All memory tools still work offline using deterministic local heuristics and hash embeddings.

#### Common model folder layout

Put model assets under the ReactiveMemory model directory and point options at explicit files. Runtime downloads are disabled by default, so install models yourself.

Recommended layout:

```text
~/.reactivememory/models/
  all-MiniLM-L6-v2/
    onnx/model.onnx
    tokenizer.json
  phi-3-mini-instruct/
    onnx/model.onnx
    tokenizer.json
```

Windows equivalent:

```text
%USERPROFILE%\.reactivememory\models\
  all-MiniLM-L6-v2\onnx\model.onnx
  all-MiniLM-L6-v2\tokenizer.json
  phi-3-mini-instruct\onnx\model.onnx
  phi-3-mini-instruct\tokenizer.json
```

Use model files that match your runtime adapter. Examples:

- Embeddings: ONNX export of `sentence-transformers/all-MiniLM-L6-v2`, `intfloat/e5-small-v2`, or `BAAI/bge-small-en-v1.5`.
- Summarisation/classification: ONNX or ORT GenAI export of Phi-3 Mini Instruct, Llama 3.x quantised, or another small local instruction model.
- Keep embedding dimensions explicit: `384` for MiniLM, commonly `384` or `768` for other small embedding models depending on the export.

#### Enable local model support in a source/custom host

The packaged stdio server calls `AddReactiveMemory()` with defaults. For NPU-backed inference, create a small source/custom host that supplies options and registers an offline runtime adapter.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Wiring;

var options = new ReactiveMemoryOptions
{
    LocalModel =
    {
        Enabled = true,
        EmbeddingProvider = "Onnx",
        ModelDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".reactivememory",
            "models"),
        EmbeddingModelPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".reactivememory",
            "models",
            "all-MiniLM-L6-v2",
            "onnx",
            "model.onnx"),
        TokenizerPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".reactivememory",
            "models",
            "all-MiniLM-L6-v2",
            "tokenizer.json"),
        ProviderPreference = ["DirectML", "QNN", "OpenVINO", "CPU"],
        ExpectedEmbeddingDimensions = 384,
        AllowCpuFallback = true,
        AllowCloud = false,
        DownloadAllowed = false,
    },
};

builder.Services.AddReactiveMemory(options);

// Replace this with your concrete local runtime implementation.
// The adapter should create IEmbeddingProvider instances and implement
// TryGenerateTextAsync for summarisation/classification prompts.
builder.Services.Replace(ServiceDescriptor.Singleton<ILocalModelRuntime, OnnxLocalModelRuntime>());
```

A runtime adapter should:

- Load local model files only from configured paths.
- Never call the network unless `AllowCloud=true` and the user explicitly opted in.
- Report execution providers through `GetStatus()` so `reactivememory_local_model_status` can explain what is active.
- Return deterministic fallback/unavailable results instead of throwing when native libraries or model files are absent, unless `AllowCpuFallback=false`.
- Keep prompts and rejected sensitive content out of audit logs.

After changing the host, restart the MCP client so it respawns the stdio server.

#### Windows setup: DirectML, Qualcomm QNN, or CPU fallback

Use this path for Windows 11, Copilot+ PCs, Qualcomm Snapdragon X devices, Intel/AMD systems, and machines where DirectML exposes acceleration.

1. Install prerequisites:

```powershell
winget install Microsoft.DotNet.SDK.10
winget install Git.Git
```

2. Check for accelerator hardware:

```powershell
Get-PnpDevice -Class System | Where-Object FriendlyName -Match 'NPU|Neural|AI|Qualcomm|Intel|AMD'
Get-CimInstance Win32_VideoController | Select-Object Name, DriverVersion
```

Also check **Task Manager → Performance** for an `NPU` graph and **Device Manager** for neural processor devices.

3. Choose a runtime adapter/package strategy:

- DirectML path: use `Microsoft.ML.OnnxRuntime.DirectML` in your adapter for DirectML-capable Windows devices. DirectML may run on GPU or NPU depending on Windows, driver, provider, and model support.
- Qualcomm NPU path: use a QNN-capable ONNX Runtime build/adapter where available and set `ProviderPreference = ["QNN", "CPU"]`.
- CPU fallback path: use `Microsoft.ML.OnnxRuntime` or keep the built-in hash provider. This is safest for development and CI.
- Local LLM path: use ONNX Runtime GenAI or an equivalent offline runtime adapter for Phi-3/Llama summarisation.

4. Copy models to `%USERPROFILE%\.reactivememory\models\...`.

5. Enable with Windows-oriented options:

```csharp
LocalModel =
{
    Enabled = true,
    EmbeddingProvider = "Onnx",
    EmbeddingModelPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".reactivememory", "models", "all-MiniLM-L6-v2", "onnx", "model.onnx"),
    TokenizerPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".reactivememory", "models", "all-MiniLM-L6-v2", "tokenizer.json"),
    ProviderPreference = ["DirectML", "QNN", "CPU"],
    ExpectedEmbeddingDimensions = 384,
    AllowCpuFallback = true,
    AllowCloud = false,
    DownloadAllowed = false,
}
```

6. Verify through your MCP client:

```text
Call reactivememory_local_model_status and tell me whether DirectML/QNN/local model acceleration is ready or falling back to hash embeddings.
```

Expected healthy states:

- `ready=true`, `activeEmbeddingProvider="Onnx"`: local model path is active.
- `ready=false`, `activeEmbeddingProvider="Hash"`, `cpuFallbackActive=true`: model acceleration is unavailable but memory search remains functional.

#### Linux setup: OpenVINO, CUDA, ROCm/custom, or CPU fallback

Use this path for Linux workstations, Intel NPU/OpenVINO machines, NVIDIA CUDA systems, AMD/ROCm systems, and servers where CPU fallback is acceptable.

1. Install prerequisites:

```bash
sudo apt-get update
sudo apt-get install -y git curl
# Install .NET 10 SDK using your distribution's Microsoft package feed.
dotnet --info
```

2. Check accelerator visibility:

```bash
lspci | grep -Ei 'nvidia|intel|amd|neural|npu|vga|3d'
lsusb | grep -Ei 'neural|npu|intel|qualcomm' || true
```

For Intel/OpenVINO devices, install the vendor driver/runtime packages recommended for your distribution. For NVIDIA, install the matching driver/CUDA runtime. For AMD/ROCm or other NPUs, use a matching custom `ILocalModelRuntime` adapter.

3. Choose provider preference:

```csharp
ProviderPreference = ["OpenVINO", "CUDA", "CPU"]
```

Use:

- `OpenVINO` for Intel CPU/GPU/NPU acceleration where your adapter supports it.
- `CUDA` for NVIDIA GPU acceleration.
- `CPU` for deterministic fallback and CI.
- A custom provider name if your adapter reports another backend, for example `ROCm`, `Vulkan`, or a vendor NPU runtime.

4. Copy models to `~/.reactivememory/models/...`:

```bash
mkdir -p ~/.reactivememory/models/all-MiniLM-L6-v2/onnx
cp /path/to/model.onnx ~/.reactivememory/models/all-MiniLM-L6-v2/onnx/model.onnx
cp /path/to/tokenizer.json ~/.reactivememory/models/all-MiniLM-L6-v2/tokenizer.json
```

5. Enable with Linux-oriented options:

```csharp
LocalModel =
{
    Enabled = true,
    EmbeddingProvider = "Onnx",
    EmbeddingModelPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".reactivememory", "models", "all-MiniLM-L6-v2", "onnx", "model.onnx"),
    TokenizerPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".reactivememory", "models", "all-MiniLM-L6-v2", "tokenizer.json"),
    ProviderPreference = ["OpenVINO", "CUDA", "CPU"],
    ExpectedEmbeddingDimensions = 384,
    AllowCpuFallback = true,
    AllowCloud = false,
    DownloadAllowed = false,
}
```

6. Verify:

```text
Call reactivememory_local_model_status and confirm the active provider, model path, tokenizer path, and fallback state.
```

#### macOS setup: Apple Silicon, Core ML/MLX adapter, or CPU fallback

Use this path for Apple Silicon Macs and Intel Macs. Apple Neural Engine access is runtime-specific; the core server remains provider-neutral and requires a Core ML, MLX, or ONNX-compatible adapter if you want hardware acceleration.

1. Install prerequisites:

```bash
brew install dotnet git
# Or install the .NET 10 SDK package from Microsoft.
dotnet --info
```

2. Check hardware:

```bash
system_profiler SPHardwareDataType | grep -E 'Chip|Processor|Model Name'
system_profiler SPDisplaysDataType | grep -E 'Chipset Model|Metal'
```

3. Choose provider preference:

```csharp
ProviderPreference = ["CoreML", "MLX", "CPU"]
```

Use:

- `CoreML` for a Core ML-backed adapter that can use Apple Neural Engine when model/operator support allows it.
- `MLX` for an MLX-backed local LLM adapter on Apple Silicon.
- `CPU` for the built-in deterministic fallback or a portable ONNX CPU adapter.

4. Copy models to `~/.reactivememory/models/...`. Use formats your adapter supports: ONNX for ONNX Runtime adapters, `.mlmodelc`/Core ML packages for Core ML adapters, or MLX-compatible model folders for MLX adapters.

5. Enable with macOS-oriented options:

```csharp
LocalModel =
{
    Enabled = true,
    EmbeddingProvider = "Onnx",
    EmbeddingModelPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".reactivememory", "models", "all-MiniLM-L6-v2", "onnx", "model.onnx"),
    TokenizerPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".reactivememory", "models", "all-MiniLM-L6-v2", "tokenizer.json"),
    ProviderPreference = ["CoreML", "MLX", "CPU"],
    ExpectedEmbeddingDimensions = 384,
    AllowCpuFallback = true,
    AllowCloud = false,
    DownloadAllowed = false,
}
```

6. Verify:

```text
Call reactivememory_local_model_status and confirm whether the CoreML/MLX adapter reports ready or CPU/hash fallback is active.
```

#### Disable local model/NPU support

Disable acceleration by returning to the default safe settings and restarting the MCP client/server process.

```csharp
var options = new ReactiveMemoryOptions
{
    LocalModel =
    {
        Enabled = false,
        EmbeddingProvider = "Hash",
        ProviderPreference = ["CPU"],
        EmbeddingModelPath = null,
        TokenizerPath = null,
        ExpectedEmbeddingDimensions = null,
        AllowCpuFallback = true,
        AllowCloud = false,
        DownloadAllowed = false,
    },
};
```

Also remove or stop registering any custom `ILocalModelRuntime` adapter if you want the packaged fallback-only behavior.

After disabling, verify:

```text
Call reactivememory_local_model_status. It should report enabled=false, activeEmbeddingProvider="Hash", and CPU/hash fallback active.
```

#### Runtime verification checklist

1. Start or restart the MCP client so the stdio server is relaunched.
2. Ask the agent to call `reactivememory_status`.
3. Ask the agent to call `reactivememory_local_model_status`.
4. Confirm:
   - `enabled` matches your intended toggle.
   - `requestedEmbeddingProvider` is `Onnx` only when you intended local embeddings.
   - `activeEmbeddingProvider` is either `Onnx`/your adapter provider or `Hash` fallback.
   - `modelFilePresent` and `tokenizerFilePresent` are true when local model mode is expected.
   - `allowCloud=false` and `downloadAllowed=false` for private/offline operation.
5. Run an automanage smoke test:

```text
Call reactivememory_memory_automanage with a harmless long-term preference, then call reactivememory_memory_get_relevant for the same topic.
```

6. Run a privacy smoke test:

```text
Call reactivememory_memory_automanage with a synthetic secret such as "password=not-a-real-secret-123" and verify it is rejected and not stored in drawers, vectors, relays, or WAL content.
```

#### Troubleshooting

- `enabled=false`: the feature is disabled by configuration. Enable `LocalModel.Enabled=true` in your host options and restart the MCP client.
- `activeEmbeddingProvider="Hash"` with `cpuFallbackActive=true`: local acceleration is unavailable, but the server is operating safely with deterministic fallback.
- Model file missing: check `EmbeddingModelPath`, path separators, and whether the MCP server process runs under the user account that owns `~/.reactivememory/models`.
- Tokenizer missing: copy `tokenizer.json` next to the model and set `TokenizerPath` explicitly.
- Provider not reported: install the native runtime/provider package for your OS or change `ProviderPreference` to include `CPU`.
- Startup fails when local provider is unavailable: set `AllowCpuFallback=true`, or install the provider/model files required by your adapter.
- Unexpected cloud/network activity: keep `AllowCloud=false`, `DownloadAllowed=false`, and use only adapters that honor those flags.

Embedding provider selection is explicit and fallback-safe:

- `SimpleTextEmbeddingProvider` remains the default (`ProviderId="Hash"`, `Version=1`, `Dimensions=512`).
- Local model embeddings are selected only when `LocalModel.Enabled=true`, `EmbeddingProvider` is not `Hash`, an `ILocalModelRuntime` resolves a provider, and `ExpectedEmbeddingDimensions` is either unset or matches the resolved provider dimensions.
- If the configured local runtime is unavailable or its dimensions do not match, `AllowCpuFallback=true` keeps deterministic hash embeddings active; disabling fallback causes startup to fail instead of silently mixing incompatible vectors.
- Vector records persist `embeddingProviderId`, `embeddingVersion`, and `embeddingDimensions`. Queries re-embed incompatible legacy vectors in memory rather than comparing different provider/dimension spaces, preserving search compatibility without silently corrupting stored vectors.

---

## Build

```bash
dotnet build src/ReactiveMemory.MCP.Server.slnx
```

From WSL using the Windows .NET SDK:

```bash
"/mnt/c/Program Files/dotnet/dotnet.exe" build "$(wslpath -w /mnt/d/Projects/Github/chrispulman/ReactiveMemory.MCP.Server/src/ReactiveMemory.MCP.Server.slnx)"
```

---

## Test

Build first, then run the test project:

```bash
dotnet test src/ReactiveMemory.MCP.Tests/ReactiveMemory.MCP.Tests.csproj
```

---

## Installation

### Requirements

- .NET 10 SDK
- An MCP-capable client (VS Code, Visual Studio, Claude Desktop, or any MCP 1.x host)

### Manual configuration from source

Configure your MCP client to launch the server from the built output:

```json
{
  "type": "stdio",
  "command": "dotnet",
  "args": [
    "run",
    "--project",
    "/path/to/ReactiveMemory.MCP.Server/src/ReactiveMemory.MCP.Server/CP.ReactiveMemory.MCP.Server.csproj"
  ]
}
```

---

## Example prompts for your AI assistant

Once configured, you can ask things like:

- "Call reactivememory_status and tell me what is stored in the core"
- "For this prompt, call reactivememory_react_to_prompt first, then search for everything related to authentication decisions we made last sprint"
- "Search relay hints for JWT refresh flow before doing a full semantic search"
- "Store this architectural decision under sector `backend`, vault `decisions`"
- "Fetch drawer `drawer_backend_decisions_...` and show me the full stored content"
- "List the last 20 drawers in sector `backend`, vault `patterns`"
- "Update this stored drawer so it moves from vault `bugs` to vault `fixes`"
- "What do we know about the `PaymentService` entity? Call reactivememory_facts_query"
- "Record that `AuthService` changed its database from MySQL to PostgreSQL today using reactivememory_facts_invalidate then reactivememory_facts_add"
- "Show me a timeline of all changes to the `DeploymentPipeline` entity using reactivememory_facts_timeline"
- "Walk the vault graph from `patterns` with 3 hops and tell me what is connected"
- "Find all vaults that bridge the `auth` and `infrastructure` sectors"
- "Create an explicit tunnel from sector `backend` / vault `patterns` to sector `devops` / vault `deployments`"
- "Follow tunnels from sector `backend`, vault `patterns`"
- "Write a diary entry summarising what we did in this session"
- "Read the last 5 diary entries for the `copilot` agent"
- "Check if this code snippet is already stored before saving it"
- "Acknowledge whatever memories were silently filed away most recently"
- "Reconnect ReactiveMemory because another process modified the local core"
- "Call reactivememory_local_model_status and tell me whether local model acceleration is enabled or falling back to hash embeddings"
- "What does the AAAK spec say and how should I store a new credential record?"
