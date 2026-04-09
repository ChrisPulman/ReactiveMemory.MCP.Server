using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>
/// Mine conversation transcripts into ReactiveMemory entries.
/// </summary>
public sealed class ConversationMiner
{
    private readonly ReactiveMemoryService service;

    /// <summary>
    /// Initializes a new instance of the ConversationMiner class using the specified reactive memory service.
    /// </summary>
    /// <param name="service">The ReactiveMemoryService instance to use for conversation mining operations. Cannot be null.</param>
    public ConversationMiner(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        this.service = service;
    }

    /// <summary>
    /// Processes the specified conversation content by normalizing, chunking, classifying, and storing each chunk in
    /// the service for the given sector and source file.
    /// </summary>
    /// <param name="content">The raw conversation content to be processed. Cannot be null or whitespace.</param>
    /// <param name="sector">The sector identifier used to categorize the processed conversation data. Cannot be null or whitespace.</param>
    /// <param name="sourceFile">The path or name of the source file from which the conversation content originated. Cannot be null or
    /// whitespace.</param>
    /// <returns>The number of conversation chunks processed and stored.</returns>
    public async Task<int> MineAsync(string content, string sector, string sourceFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(sector);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);

        var transcript = ConversationNormalizer.Normalize(content);
        var chunks = ConversationChunker.Chunk(transcript);
        foreach (var chunk in chunks)
        {
            var vault = ConversationVaultClassifier.Classify(chunk);
            await this.service.AddDrawerAsync(sector, vault, chunk, sourceFile, "conversation_miner");
        }

        return chunks.Count;
    }
}
