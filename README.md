# ReactiveMemory MCP Server

<!-- mcp-name: io.github.chrispulman/reactivememory-mcp-server -->

ReactiveMemory MCP Server gives AI assistants a persistent, queryable, locally-stored memory system backed by vector search, a temporal knowledge graph, and a vault-structured content store. 
It is designed to be the durable external memory layer for agents, copilots, and AI workflows that need to remember, reason over, and navigate accumulated knowledge across sessions.

It is implemented in C# on `net10.0` using `ModelContextProtocol` `1.2.0`.

## Quick Install

Click to install in your preferred environment:

[![VS Code - Install Reactive Memory MCP](https://img.shields.io/badge/VS_Code-Install_ReactiveMemory_MCP-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://vscode.dev/redirect/mcp/install?name=reactivememory-mcp-server&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22CP.ReactiveMemory.Mcp.Server%400.*%22%2C%22--yes%22%5D%7D)
[![VS Code Insiders - Install Reactive Memory MCP](https://img.shields.io/badge/VS_Code_Insiders-Install_ReactiveMemory_MCP-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://insiders.vscode.dev/redirect/mcp/install?name=reactivememory-mcp-server&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22CP.ReactiveMemory.Mcp.Server%400.*%22%2C%22--yes%22%5D%7D&quality=insiders)
[![Visual Studio - Install Reactive Memory MCP](https://img.shields.io/badge/Visual_Studio-Install_ReactiveMemory_MCP-5C2D91?style=flat-square&logo=visualstudio&logoColor=white)](https://vs-open.link/mcp-install?%7B%22name%22%3A%22CP.ReactiveMemory.Mcp.Server%22%2C%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22CP.ReactiveMemory.Mcp.Server%400.*%22%2C%22--yes%22%5D%7D)

Note:
- These install links are prepared for the intended NuGet package identity `CP.ReactiveMemory.Mcp.Server`.
- If the latest package has not been published yet, use the manual source-build configuration below.

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
| Vector store | JSON + TF-IDF-style text embeddings | `~/.reactivememory/core/` |
| Knowledge graph | SQLite (WAL mode) | `~/.reactivememory/core/knowledge_graph.sqlite3` |
| Write-ahead log | JSONL append-only file | `~/.reactivememory/wal/write_log.jsonl` |

All storage is local and file-based — no external services or API keys are required.

## Agent protocol

When this server is active, agents should follow the **ReactiveMemory Protocol**:

1. Call `reactivememory_status` on initialisation to load the current core summary and receive operational guidance.
2. Before answering questions about persisted facts, call `reactivememory_facts_query` or `reactivememory_search` and use retrieved data instead of assumptions.
3. When facts change, call `reactivememory_facts_invalidate` for the old state and `reactivememory_facts_add` for the replacement.
4. After a meaningful interaction, call `reactivememory_diary_write` to persist a concise session record.
5. Use `reactivememory_list_sectors`, `reactivememory_list_vaults`, `reactivememory_get_taxonomy`, `reactivememory_traverse`, and `reactivememory_find_tunnels` for discovery and navigation.
6. Call `reactivememory_check_duplicate` before storing repeated content when deduplication accuracy matters.

## Available MCP tools

### Core & taxonomy

#### `reactivememory_status`
Returns a summary of the entire core: total drawer count, per-sector counts, per-vault counts, the configured core path, and the full agent protocol instructions.

**When to use:** Call this first on every session start. The response tells the agent what is stored and how to use the server correctly.

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

### Search & deduplication

#### `reactivememory_search`
Performs a semantic vector search across stored drawers. Results are ranked by similarity to the query text.

**Parameters:**
- `query` — the search string
- `limit` *(default: 5)* — maximum number of results to return
- `sector` *(optional)* — restrict results to one sector
- `vault` *(optional)* — restrict results to one vault

**When to use:** Use this as the primary retrieval mechanism whenever an agent needs to find relevant stored content. Prefer this over asking the user to repeat context that may already be in the core. Also use it before writing new drawers to check if closely related content already exists.

---

#### `reactivememory_check_duplicate`
Checks whether content already exists in the core using configurable similarity threshold matching.

**Parameters:**
- `content` — the text to check
- `threshold` *(default: 0.9)* — similarity score above which a match is considered a duplicate

**When to use:** Call before `reactivememory_add_drawer` when the content might already be stored. A threshold of `0.9` catches near-identical content; lower values (e.g. `0.7`) will flag looser semantic overlaps. Returns whether a duplicate was found, the matching drawer IDs, and their similarity scores.

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
- `sourceCloset` *(optional)* — the vault or source identifier for traceability

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

### Graph traversal

ReactiveMemory builds an implicit vault graph by analysing which drawers share sectors. Vaults that have drawers in multiple sectors become **tunnels** — natural bridges between domains.

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
Returns overall core graph statistics: total vault node count, total edge count, average drawer count per node, and the top-connected vaults.

**When to use:** Use for a structural health check of the graph, or to find the most densely populated vaults in the core at a glance.

---

### Agent diary

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

## Additional capabilities (library API)

The `ReactiveMemory.MCP.Core` library also exposes higher-level mining and entity detection APIs for use outside the MCP tool surface:

### ProjectMiner
Scans a source directory and files all discovered content into the core. It skips `bin`, `obj`, and `.git` directories, routes files to vaults via a configurable `VaultRouter`, and maintains a file index to skip files that have not changed since the last mine.

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
| `~/.reactivememory/core/` | Drawer JSON store and vector index |
| `~/.reactivememory/core/knowledge_graph.sqlite3` | Temporal knowledge graph |
| `~/.reactivememory/wal/write_log.jsonl` | Append-only audit log of all mutations |

Configuration can be overridden by providing a `ReactiveMemoryOptions` instance when constructing `ReactiveMemoryService` programmatically.

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
    "/path/to/ReactiveMemory.MCP.Server/src/ReactiveMemory.MCP.Server/ReactiveMemory.MCP.Server.csproj"
  ]
}
```

---

## Example prompts for your AI assistant

Once configured, you can ask things like:

- "Call reactivememory_status and tell me what is stored in the core"
- "Search for everything related to authentication decisions we made last sprint"
- "Store this architectural decision under sector `backend`, vault `decisions`"
- "What do we know about the `PaymentService` entity? Call reactivememory_facts_query"
- "Record that `AuthService` changed its database from MySQL to PostgreSQL today using reactivememory_facts_invalidate then reactivememory_facts_add"
- "Show me a timeline of all changes to the `DeploymentPipeline` entity using reactivememory_facts_timeline"
- "Walk the vault graph from `patterns` with 3 hops and tell me what is connected"
- "Find all vaults that bridge the `auth` and `infrastructure` sectors"
- "Write a diary entry summarising what we did in this session"
- "Read the last 5 diary entries for the `copilot` agent"
- "Check if this code snippet is already stored before saving it"
- "What does the AAAK spec say and how should I store a new credential record?"
