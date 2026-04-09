using ReactiveMemory.MCP.Core.Abstractions;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Default local embedding provider.
/// </summary>
public sealed class SimpleTextEmbeddingProvider : IEmbeddingProvider
{
    /// <summary>
    /// Generates a vector embedding that represents the specified text.
    /// </summary>
    /// <param name="text">The input text to embed. Cannot be null.</param>
    /// <returns>A read-only list of doubles representing the embedding vector for the input text.</returns>
    public IReadOnlyList<double> Embed(string text) => SimpleTextEmbeddingService.Embed(text);

    /// <summary>
    /// Calculates the cosine similarity between two numeric vectors.
    /// </summary>
    /// <param name="left">The first vector to compare. Must have the same length as <paramref name="right"/>.</param>
    /// <param name="right">The second vector to compare. Must have the same length as <paramref name="left"/>.</param>
    /// <returns>A double value representing the cosine similarity between the two vectors. The value ranges from -1 (completely
    /// dissimilar) to 1 (identical).</returns>
    public double Similarity(IReadOnlyList<double> left, IReadOnlyList<double> right) => SimpleTextEmbeddingService.CosineSimilarity(left, right);
}
