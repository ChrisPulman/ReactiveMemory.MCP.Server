// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.Data.Sqlite;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>SQLite-backed temporal knowledge graph.</summary>
public sealed class KnowledgeGraphStore
{
    /// <summary>Query used to read outgoing facts valid on the requested date.</summary>
    private const string OutgoingFactsSql = "SELECT t.predicate, t.valid_from, t.valid_to, t.confidence, t.source_closet, e.name FROM triples t JOIN entities e ON t.object = e.id WHERE t.subject = $entity AND (valid_from IS NULL OR valid_from <= $asOf1) AND (valid_to IS NULL OR valid_to >= $asOf2)";

    /// <summary>Query used to read incoming facts valid on the requested date.</summary>
    private const string IncomingFactsSql = "SELECT t.predicate, t.valid_from, t.valid_to, t.confidence, t.source_closet, e.name FROM triples t JOIN entities e ON t.subject = e.id WHERE t.object = $entity AND (valid_from IS NULL OR valid_from <= $asOf1) AND (valid_to IS NULL OR valid_to >= $asOf2)";

    /// <summary>Ordinal of the timeline valid-from column.</summary>
    private const int TimelineValidFromOrdinal = 3;

    /// <summary>Ordinal of the timeline object column.</summary>
    private const int TimelineObjectOrdinal = 2;

    /// <summary>Ordinal of the timeline valid-to column.</summary>
    private const int TimelineValidToOrdinal = 4;

    /// <summary>Ordinal of the query valid-from column.</summary>
    private const int QueryValidFromOrdinal = 1;

    /// <summary>Ordinal of the query valid-to column.</summary>
    private const int QueryValidToOrdinal = 2;

    /// <summary>Ordinal of the query source-closet column.</summary>
    private const int QuerySourceClosetOrdinal = 4;

    /// <summary>Ordinal of the query confidence column.</summary>
    private const int QueryConfidenceOrdinal = 3;

    /// <summary>Ordinal of the query related-entity column.</summary>
    private const int QueryRelatedEntityOrdinal = 5;

    /// <summary>Documents the CountEntitiesSql member.</summary>
    private const string CountEntitiesSql = "SELECT COUNT(*) FROM entities;";

    /// <summary>Documents the CountTriplesSql member.</summary>
    private const string CountTriplesSql = "SELECT COUNT(*) FROM triples;";

    /// <summary>Documents the CountCurrentSql member.</summary>
    private const string CountCurrentSql = "SELECT COUNT(*) FROM triples WHERE valid_to IS NULL;";

    /// <summary>Documents the CountExpiredSql member.</summary>
    private const string CountExpiredSql = "SELECT COUNT(*) FROM triples WHERE valid_to IS NOT NULL;";

    /// <summary>Documents the _connectionString member.</summary>
    private readonly string _connectionString;

    /// <summary>Documents the _initializationLock member.</summary>
    private readonly object _initializationLock = new();

    /// <summary>Documents the _initializationTask member.</summary>
    private Task? _initializationTask;

    /// <summary>Initializes a new instance of the KnowledgeGraphStore class using the specified database file path.</summary>
    /// <remarks>If the directory specified in dbPath does not exist, it is created automatically.</remarks>
    /// <param name="dbPath">The file path to the SQLite database. Cannot be null, empty, or consist only of white-space characters.</param>
    public KnowledgeGraphStore(string dbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
    }

    /// <summary>Converts the specified name to a normalized entity identifier string suitable for use in database keys or identifiers.</summary>
    /// <remarks>The returned identifier is converted to lowercase using the invariant culture. This method is
    /// intended to produce consistent, database-friendly identifiers from arbitrary names.</remarks>
    /// <param name="name">The name to convert. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A normalized string identifier derived from the specified name, with spaces replaced by underscores and
    /// apostrophes removed.</returns>
    public static string ToEntityId(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.ToLowerInvariant().Replace(" ", "_", StringComparison.Ordinal).Replace("'", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>Initializes the database schema asynchronously, creating required tables and indexes if they do not already exist.</summary>
    /// <remarks>This method ensures that the database is ready for use by creating the necessary tables and
    /// indexes. It can be safely called multiple times; existing tables and indexes are not modified if they already
    /// exist.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public Task InitializeAsync()
    {
        lock (_initializationLock)
        {
            return _initializationTask ??= InitializeCoreAsync();
        }
    }

    /// <summary>Asynchronously adds a new entity to the database or updates it if an entity with the same name already exists.</summary>
    /// <remarks>If an entity with the specified name already exists, its information is replaced with the
    /// provided values.</remarks>
    /// <param name="name">The name of the entity to add or update. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="entityType">The type of the entity. Defaults to "unknown" if not specified. Cannot be null, empty, or consist only of
    /// white-space characters.</param>
    /// <param name="propertiesJson">A JSON string representing the properties of the entity. Cannot be null.</param>
    /// <returns>A string containing the unique identifier of the added or updated entity.</returns>
    public async Task<string> AddEntityAsync(string name, string entityType = "unknown", string propertiesJson = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentNullException.ThrowIfNull(propertiesJson);
        await InitializeAsync();

        var entityId = ToEntityId(name);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO entities (id, name, type, properties) VALUES ($id, $name, $type, $properties);";
        _ = command.Parameters.AddWithValue("$id", entityId);
        _ = command.Parameters.AddWithValue("$name", name);
        _ = command.Parameters.AddWithValue("$type", entityType);
        _ = command.Parameters.AddWithValue("$properties", propertiesJson);
        await command.ExecuteNonQueryAsync();
        return entityId;
    }

    /// <summary>
    /// Asynchronously adds a new triple to the store if it does not already exist, or returns the identifier of the
    /// existing triple.
    /// </summary>
    /// <remarks>If a triple with the same subject, predicate, and object already exists and is currently
    /// valid, the method returns its identifier instead of creating a new entry. The method ensures that both the
    /// subject and object entities exist in the store before adding the triple.</remarks>
    /// <param name="subject">The subject entity of the triple. Cannot be null, empty, or whitespace.</param>
    /// <param name="predicate">The predicate describing the relationship between the subject and object. Cannot be null, empty, or whitespace.</param>
    /// <param name="obj">The object entity of the triple. Cannot be null, empty, or whitespace.</param>
    /// <param name="validFrom">The start of the validity period for the triple, or null if not specified. The format should match the expected
    /// date representation in the store.</param>
    /// <param name="validTo">The end of the validity period for the triple, or null if not specified. The format should match the expected
    /// date representation in the store.</param>
    /// <param name="confidence">A confidence score for the triple, typically between 0.0 and 1.0. Higher values indicate greater confidence.</param>
    /// <param name="sourceCloset">An optional identifier for the source closet from which the triple originates, or null if not applicable.</param>
    /// <param name="sourceFile">An optional identifier for the source file from which the triple originates, or null if not applicable.</param>
    /// <returns>A string containing the unique identifier of the newly added triple, or the identifier of the existing triple if
    /// one with the same subject, predicate, and object already exists.</returns>
    public async Task<string> AddTripleAsync(string subject, string predicate, string obj, string? validFrom, string? validTo, double confidence, string? sourceCloset, string? sourceFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(obj);
        await InitializeAsync();

        var subjectId = ToEntityId(subject);
        var objectId = ToEntityId(obj);
        var normalizedPredicate = MemoryText.NormalizePredicate(predicate);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await EnsureEntityExistsAsync(connection, subjectId, subject);
        await EnsureEntityExistsAsync(connection, objectId, obj);

        var existingCommand = connection.CreateCommand();
        existingCommand.CommandText = "SELECT id FROM triples WHERE subject = $subject AND predicate = $predicate AND object = $object AND valid_to IS NULL LIMIT 1;";
        _ = existingCommand.Parameters.AddWithValue("$subject", subjectId);
        _ = existingCommand.Parameters.AddWithValue("$predicate", normalizedPredicate);
        _ = existingCommand.Parameters.AddWithValue("$object", objectId);
        var existing = (string?)await existingCommand.ExecuteScalarAsync();
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var tripleId = $"t_{subjectId}_{normalizedPredicate}_{objectId}_{Guid.NewGuid():N}";
        var insert = connection.CreateCommand();
        insert.CommandText = """
INSERT INTO triples (id, subject, predicate, object, valid_from, valid_to, confidence, source_closet, source_file)
VALUES ($id, $subject, $predicate, $object, $validFrom, $validTo, $confidence, $sourceCloset, $sourceFile);
""";
        _ = insert.Parameters.AddWithValue("$id", tripleId);
        _ = insert.Parameters.AddWithValue("$subject", subjectId);
        _ = insert.Parameters.AddWithValue("$predicate", normalizedPredicate);
        _ = insert.Parameters.AddWithValue("$object", objectId);
        _ = insert.Parameters.AddWithValue("$validFrom", (object?)validFrom ?? DBNull.Value);
        _ = insert.Parameters.AddWithValue("$validTo", (object?)validTo ?? DBNull.Value);
        _ = insert.Parameters.AddWithValue("$confidence", confidence);
        _ = insert.Parameters.AddWithValue("$sourceCloset", (object?)sourceCloset ?? DBNull.Value);
        _ = insert.Parameters.AddWithValue("$sourceFile", (object?)sourceFile ?? DBNull.Value);
        await insert.ExecuteNonQueryAsync();
        return tripleId;
    }

    /// <summary>Marks a triple as invalid by setting its end time in the data store asynchronously.</summary>
    /// <param name="subject">The subject of the triple to invalidate.</param>
    /// <param name="predicate">The predicate of the triple to invalidate.</param>
    /// <param name="obj">The object of the triple to invalidate.</param>
    /// <param name="ended">The end time to set for the triple.</param>
    /// <returns>A task whose result is true when a matching active triple was invalidated.</returns>
    public Task<bool> InvalidateAsync(string subject, string predicate, string obj, string ended)
        => InvalidateCoreAsync(subject, predicate, obj, ended);

    /// <summary>
    /// Asynchronously queries the knowledge graph for facts related to the specified entity, filtered by direction and
    /// an optional date.
    /// </summary>
    /// <remarks>If the direction is not specified or is whitespace, "outgoing" is used by default. The query
    /// is executed against a SQLite database and results are returned in no guaranteed order.</remarks>
    /// <param name="entity">The name of the entity to query. Cannot be null or whitespace.</param>
    /// <param name="asOf">An optional ISO 8601 date string to filter facts that are valid as of the specified date. If null, all facts are
    /// returned regardless of date.</param>
    /// <param name="direction">The direction of relationships to query. Valid values are "outgoing", "incoming", or "both". Cannot be null or
    /// whitespace.</param>
    /// <returns>A read-only list of knowledge graph facts matching the specified entity, direction, and date filter. The list is
    /// empty if no matching facts are found.</returns>
    public async Task<IReadOnlyList<KnowledgeGraphFact>> QueryEntityAsync(string entity, string? asOf, string direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);
        await InitializeAsync();

        direction = string.IsNullOrWhiteSpace(direction) ? "outgoing" : direction;
        var effectiveAsOf = string.IsNullOrWhiteSpace(asOf)
            ? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
            : asOf;
        var facts = new List<KnowledgeGraphFact>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var entityId = ToEntityId(entity);

        if (direction is "outgoing" or "both")
        {
            await AppendFactsAsync(connection, facts, entity, entityId, effectiveAsOf!, outgoing: true);
        }

        if (direction is "incoming" or "both")
        {
            await AppendFactsAsync(connection, facts, entity, entityId, effectiveAsOf!, outgoing: false);
        }

        return facts;
    }

    /// <summary>Retrieves a timeline of knowledge graph entries related to the specified entity.</summary>
    /// <remarks>Entries are ordered by their valid-from date in ascending order. At most 100 entries are
    /// returned. This method executes database queries asynchronously.</remarks>
    /// <param name="entity">The name of the entity to filter timeline entries by. If null or empty, returns timeline entries for all
    /// entities.</param>
    /// <returns>A read-only list of timeline entries associated with the specified entity. The list is empty if no matching
    /// entries are found.</returns>
    public async Task<IReadOnlyList<KnowledgeGraphTimelineEntry>> TimelineAsync(string? entity)
    {
        await InitializeAsync();
        var items = new List<KnowledgeGraphTimelineEntry>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        if (string.IsNullOrWhiteSpace(entity))
        {
            command.CommandText = "SELECT s.name, t.predicate, o.name, t.valid_from, t.valid_to FROM triples t JOIN entities s ON t.subject = s.id JOIN entities o ON t.object = o.id ORDER BY COALESCE(t.valid_from, '9999-12-31') ASC LIMIT 100;";
        }
        else
        {
            command.CommandText = "SELECT s.name, t.predicate, o.name, t.valid_from, t.valid_to FROM triples t JOIN entities s ON t.subject = s.id JOIN entities o ON t.object = o.id WHERE t.subject = $entity OR t.object = $entity ORDER BY COALESCE(t.valid_from, '9999-12-31') ASC LIMIT 100;";
            _ = command.Parameters.AddWithValue("$entity", ToEntityId(entity));
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var validFrom = await reader.IsDBNullAsync(TimelineValidFromOrdinal) ? null : reader.GetString(TimelineValidFromOrdinal);
            var isCurrent = await reader.IsDBNullAsync(TimelineValidToOrdinal);
            var validTo = isCurrent ? null : reader.GetString(TimelineValidToOrdinal);
            items.Add(new KnowledgeGraphTimelineEntry(reader.GetString(0), reader.GetString(1), reader.GetString(TimelineObjectOrdinal), validFrom, validTo, isCurrent));
        }

        return items;
    }

    /// <summary>
    /// Asynchronously retrieves summary statistics about the knowledge graph, including entity and triple counts, and
    /// the distribution of relationships.
    /// </summary>
    /// <remarks>This method opens a database connection and queries for aggregate statistics. It is safe to
    /// call concurrently from multiple threads.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains a KnowledgeGraphStatsResult object
    /// with counts of entities, triples, current and expired facts, and a mapping of relationship types to their
    /// occurrence counts.</returns>
    public async Task<KnowledgeGraphStatsResult> StatsAsync()
    {
        await InitializeAsync();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var entities = await CountEntitiesAsync(connection);
        var triples = await CountTriplesAsync(connection);
        var current = await CountCurrentAsync(connection);
        var expired = await CountExpiredAsync(connection);

        var relationships = new Dictionary<string, int>(StringComparer.Ordinal);
        var relCommand = connection.CreateCommand();
        relCommand.CommandText = "SELECT predicate, COUNT(*) FROM triples GROUP BY predicate ORDER BY COUNT(*) DESC;";
        await using var reader = await relCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            relationships[reader.GetString(0)] = reader.GetInt32(1);
        }

        return new KnowledgeGraphStatsResult(entities, triples, current, expired, relationships);
    }

    /// <summary>Appends facts for one relationship direction.</summary>
    /// <param name="connection">The open database connection.</param>
    /// <param name="facts">The destination fact list.</param>
    /// <param name="entity">The queried entity name.</param>
    /// <param name="entityId">The queried entity identifier.</param>
    /// <param name="effectiveAsOf">The effective date.</param>
    /// <param name="outgoing">Whether to query outgoing facts.</param>
    /// <returns>The asynchronous operation.</returns>
    private static async Task AppendFactsAsync(
        SqliteConnection connection,
        List<KnowledgeGraphFact> facts,
        string entity,
        string entityId,
        string effectiveAsOf,
        bool outgoing)
    {
        var command = connection.CreateCommand();
        if (outgoing)
        {
            command.CommandText = OutgoingFactsSql;
        }
        else
        {
            command.CommandText = IncomingFactsSql;
        }

        _ = command.Parameters.AddWithValue("$entity", entityId);
        _ = command.Parameters.AddWithValue("$asOf1", effectiveAsOf);
        _ = command.Parameters.AddWithValue("$asOf2", effectiveAsOf);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var validFrom = await reader.IsDBNullAsync(QueryValidFromOrdinal) ? null : reader.GetString(QueryValidFromOrdinal);
            var isCurrent = await reader.IsDBNullAsync(QueryValidToOrdinal);
            var validTo = isCurrent ? null : reader.GetString(QueryValidToOrdinal);
            var sourceCloset = await reader.IsDBNullAsync(QuerySourceClosetOrdinal) ? null : reader.GetString(QuerySourceClosetOrdinal);
            facts.Add(CreateFact(reader, entity, outgoing, validFrom, validTo, sourceCloset, isCurrent));
        }
    }

    /// <summary>Creates a fact from the active data reader row.</summary>
    /// <param name="reader">The active reader.</param>
    /// <param name="entity">The queried entity.</param>
    /// <param name="outgoing">Whether the row represents an outgoing fact.</param>
    /// <param name="validFrom">The optional validity start.</param>
    /// <param name="validTo">The optional validity end.</param>
    /// <param name="sourceCloset">The optional source vault.</param>
    /// <param name="isCurrent">Whether the fact is current.</param>
    /// <returns>The materialized fact.</returns>
    private static KnowledgeGraphFact CreateFact(
        SqliteDataReader reader,
        string entity,
        bool outgoing,
        string? validFrom,
        string? validTo,
        string? sourceCloset,
        bool isCurrent)
        => outgoing
            ? new KnowledgeGraphFact("outgoing", entity, reader.GetString(0), reader.GetString(QueryRelatedEntityOrdinal), validFrom, validTo, reader.GetDouble(QueryConfidenceOrdinal), sourceCloset, isCurrent)
            : new KnowledgeGraphFact("incoming", reader.GetString(QueryRelatedEntityOrdinal), reader.GetString(0), entity, validFrom, validTo, reader.GetDouble(QueryConfidenceOrdinal), sourceCloset, isCurrent);

    /// <summary>Documents the EnsureEntityExistsAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="connection">The connection value.</param>
    /// <param name="id">The id value.</param>
    /// <param name="name">The name value.</param>
    private static async Task EnsureEntityExistsAsync(SqliteConnection connection, string id, string name)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO entities (id, name) VALUES ($id, $name);";
        _ = command.Parameters.AddWithValue("$id", id);
        _ = command.Parameters.AddWithValue("$name", name);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Counts all graph entities.</summary>
    /// <param name="connection">The open database connection.</param>
    /// <returns>The entity count.</returns>
    private static async Task<int> CountEntitiesAsync(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var command = connection.CreateCommand();
        command.CommandText = CountEntitiesSql;
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Counts all graph relationships.</summary>
    /// <param name="connection">The open database connection.</param>
    /// <returns>The relationship count.</returns>
    private static async Task<int> CountTriplesAsync(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var command = connection.CreateCommand();
        command.CommandText = CountTriplesSql;
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Counts current graph relationships.</summary>
    /// <param name="connection">The open database connection.</param>
    /// <returns>The current relationship count.</returns>
    private static async Task<int> CountCurrentAsync(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var command = connection.CreateCommand();
        command.CommandText = CountCurrentSql;
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Counts expired graph relationships.</summary>
    /// <param name="connection">The open database connection.</param>
    /// <returns>The expired relationship count.</returns>
    private static async Task<int> CountExpiredAsync(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var command = connection.CreateCommand();
        command.CommandText = CountExpiredSql;
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Documents the InitializeCoreAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task InitializeCoreAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
PRAGMA journal_mode=WAL;
CREATE TABLE IF NOT EXISTS entities (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    type TEXT DEFAULT 'unknown',
    properties TEXT DEFAULT '{}',
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE IF NOT EXISTS triples (
    id TEXT PRIMARY KEY,
    subject TEXT NOT NULL,
    predicate TEXT NOT NULL,
    object TEXT NOT NULL,
    valid_from TEXT,
    valid_to TEXT,
    confidence REAL DEFAULT 1.0,
    source_closet TEXT,
    source_file TEXT,
    extracted_at TEXT DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_triples_subject ON triples(subject);
CREATE INDEX IF NOT EXISTS idx_triples_object ON triples(object);
CREATE INDEX IF NOT EXISTS idx_triples_predicate ON triples(predicate);
""";
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Marks a triple as invalid by setting its end time in the data store asynchronously.</summary>
    /// <remarks>If the specified triple does not exist or is already invalidated, no changes are
    /// made.</remarks>
    /// <param name="subject">The subject of the triple to invalidate. Cannot be null or whitespace.</param>
    /// <param name="predicate">The predicate of the triple to invalidate. Cannot be null or whitespace.</param>
    /// <param name="obj">The object of the triple to invalidate. Cannot be null or whitespace.</param>
    /// <param name="ended">The end time to set for the triple, indicating when it became invalid. Cannot be null or whitespace.</param>
    /// <returns>A task whose result is true when a matching active triple was invalidated.</returns>
    private async Task<bool> InvalidateCoreAsync(string subject, string predicate, string obj, string ended)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(obj);
        ArgumentException.ThrowIfNullOrWhiteSpace(ended);
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE triples SET valid_to = $ended WHERE subject = $subject AND predicate = $predicate AND object = $object AND valid_to IS NULL;";
        _ = command.Parameters.AddWithValue("$ended", ended);
        _ = command.Parameters.AddWithValue("$subject", ToEntityId(subject));
        _ = command.Parameters.AddWithValue("$predicate", MemoryText.NormalizePredicate(predicate));
        _ = command.Parameters.AddWithValue("$object", ToEntityId(obj));
        return await command.ExecuteNonQueryAsync() > 0;
    }
}
