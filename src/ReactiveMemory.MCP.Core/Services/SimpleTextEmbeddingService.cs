namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Lightweight deterministic embedding generator for local vector-compatible search.
/// </summary>
public static class SimpleTextEmbeddingService
{
    private const int Dimensions = 64;

    /// <summary>
    /// Generates a normalized embedding vector representing the specified text.
    /// </summary>
    /// <remarks>The returned vector has a fixed dimensionality and is normalized to unit length. Identical
    /// input text will always produce the same embedding. This method is case-insensitive and ignores empty or
    /// white-space-only tokens.</remarks>
    /// <param name="text">The input text to embed. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A read-only list of doubles containing the normalized embedding vector for the input text. The vector will
    /// contain all zeros if the input text contains no tokens.</returns>
    public static IReadOnlyList<double> Embed(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var vector = new double[Dimensions];
        var tokens = text.ToLowerInvariant().Split(default(string[]?), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return vector;
        }

        foreach (var token in tokens)
        {
            var hash = token.GetHashCode(StringComparison.Ordinal);
            var index = Math.Abs(hash % Dimensions);
            vector[index] += 1.0;
        }

        var magnitude = Math.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }

        return vector;
    }

    /// <summary>
    /// Calculates the cosine similarity between two numeric vectors.
    /// </summary>
    /// <remarks>Cosine similarity measures the cosine of the angle between two vectors, providing a value
    /// between -1 and 1. The calculation uses the minimum length of the two vectors; any extra elements in the longer
    /// vector are ignored.</remarks>
    /// <param name="left">The first vector to compare. Cannot be null.</param>
    /// <param name="right">The second vector to compare. Cannot be null.</param>
    /// <returns>A double value representing the cosine similarity between the two vectors, rounded to three decimal places.
    /// Returns 0 if either vector has zero magnitude.</returns>
    public static double CosineSimilarity(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var length = Math.Min(left.Count, right.Count);
        var dot = 0.0;
        var leftMagnitude = 0.0;
        var rightMagnitude = 0.0;
        for (var i = 0; i < length; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return Math.Round(dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude)), 3);
    }
}
