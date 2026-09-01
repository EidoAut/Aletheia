using Microsoft.Data.Sqlite;

namespace Aletheia.Persistence;

/// <summary>
/// Provides typed column helpers for SQLite readers.
/// </summary>
internal static class SqliteDataReaderExtensions
{
    /// <summary>
    /// Reads a string by column name.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The column name.</param>
    /// <returns>The string value.</returns>
    internal static string GetString(this SqliteDataReader reader, string name) =>
        reader.GetString(reader.GetOrdinal(name));

    /// <summary>
    /// Reads a nullable string by column name.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The column name.</param>
    /// <returns>The nullable string value.</returns>
    internal static string? GetNullableString(this SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    /// <summary>
    /// Reads an integer by column name.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The column name.</param>
    /// <returns>The integer value.</returns>
    internal static int GetInt32(this SqliteDataReader reader, string name) =>
        reader.GetInt32(reader.GetOrdinal(name));

    /// <summary>
    /// Reads a nullable integer by column name.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The column name.</param>
    /// <returns>The nullable integer value.</returns>
    internal static int? GetNullableInt32(this SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    /// <summary>
    /// Reads a double by column name.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The column name.</param>
    /// <returns>The double value.</returns>
    internal static double GetDouble(this SqliteDataReader reader, string name) =>
        reader.GetDouble(reader.GetOrdinal(name));

    /// <summary>
    /// Reads a nullable double by column name.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The column name.</param>
    /// <returns>The nullable double value.</returns>
    internal static double? GetNullableDouble(this SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
    }
}
