// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Abstractions;

/// <summary>Embedding provider abstraction for vector-compatible search.</summary>
public interface IEmbeddingProvider
{
    /// <summary>Gets the stable provider identifier persisted with generated vectors.</summary>
    string ProviderId { get; }

    /// <summary>Gets the provider-specific embedding algorithm/model version.</summary>
    int Version { get; }

    /// <summary>Gets the number of values produced by <see cref="Embed"/>.</summary>
    int Dimensions { get; }

    /// <summary>Generates a vector representation (embedding) for the specified text.</summary>
    /// <param name="text">The input text to embed. Cannot be null or empty.</param>
    /// <returns>A read-only list of doubles representing the embedding vector for the input text.</returns>
    IReadOnlyList<double> Embed(string text);

    /// <summary>Calculates a similarity score between two sequences of double-precision values.</summary>
    /// <param name="left">The first sequence of double-precision values to compare. Cannot be null.</param>
    /// <param name="right">The second sequence of double-precision values to compare. Cannot be null.</param>
    /// <returns>A double value representing the similarity score between the two sequences. The range and interpretation of the
    /// score depend on the specific similarity metric implemented.</returns>
    double Similarity(IReadOnlyList<double> left, IReadOnlyList<double> right);
}
