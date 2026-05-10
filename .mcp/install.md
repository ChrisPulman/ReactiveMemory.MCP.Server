# ReactiveMemory MCP Server Install Notes

Executable command name after pack/install:
- `reactivememory-mcp-server`

Suggested stdio config:

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

Alternative source-run config:

```json
{
  "mcpServers": {
    "reactivememory-mcp-server": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/ReactiveMemory.MCP.Server/src/ReactiveMemory.MCP.Server/CP.ReactiveMemory.MCP.Server.csproj"
      ]
    }
  }
}
```
