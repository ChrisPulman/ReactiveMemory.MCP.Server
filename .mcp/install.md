# ReactiveMemory MCP Server Install Notes

Executable command name after pack/install:
- `reactivememory-mcp-server`

Suggested stdio config:

```json
{
  "mcpServers": {
    "reactivememory-mcp-server": {
      "command": "dotnet-reactivememory-mcp-server"
    }
  }
}
```

Alternative source-run config:

```json
{
  "mcpServers": {
    "reactivememory-mcp-server": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/ReactiveMemory.MCP.Server/src/ReactiveMemory.MCP.Server/ReactiveMemory.MCP.Server.csproj"
      ]
    }
  }
}
```
