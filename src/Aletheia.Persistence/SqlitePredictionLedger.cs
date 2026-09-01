using System.Data;
using System.Globalization;
using System.Text.Json;
using Aletheia.Core;
using Aletheia.Validation;
using Microsoft.Data.Sqlite;

namespace Aletheia.Persistence;

/// <summary>
/// Persists immutable predictions and separate evaluations in SQLite.
/// </summary>
public sealed class SqlitePredictionLedger : IPredictionLedger
{
    private const int SchemaVersion = 2;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General);
    private readonly string connectionString;
    private readonly string? databasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlitePredictionLedger"/> class.
    /// </summary>
    /// <param name="databasePath">The SQLite database path.</param>
    public SqlitePredictionLedger(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path cannot be empty.", nameof(databasePath));
        }

        this.databasePath = Path.GetFullPath(databasePath);
        this.connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = this.databasePath,
            Pooling = false,
        }.ToString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlitePredictionLedger"/> class from a raw connection string.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <param name="usesConnectionString">A marker distinguishing this overload from path construction.</param>
    public SqlitePredictionLedger(string connectionString, bool usesConnectionString)
    {
        if (!usesConnectionString)
        {
            throw new ArgumentException("Use the path constructor when a raw connection string is not intended.", nameof(usesConnectionString));
        }

        this.connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString))
            : connectionString;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (this.databasePath is not null)
        {
            var directory = Path.GetDirectoryName(this.databasePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        await using var connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var schemaVersionSql = """
            CREATE TABLE IF NOT EXISTS schema_version (
                version INTEGER NOT NULL
            );
            """;
        await ExecuteNonQueryAsync(connection, schemaVersionSql, cancellationToken).ConfigureAwait(false);

        await using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "SELECT version FROM schema_version LIMIT 1;";
        var versionValue = await versionCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (versionValue is null || versionValue == DBNull.Value)
        {
            await ExecuteNonQueryAsync(connection, CreateSchemaSql(), cancellationToken).ConfigureAwait(false);
            await ExecuteNonQueryAsync(
                connection,
                $"INSERT INTO schema_version (version) VALUES ({SchemaVersion.ToString(CultureInfo.InvariantCulture)});",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var version = Convert.ToInt32(versionValue, CultureInfo.InvariantCulture);
        if (version == 1)
        {
            await MigrateFromVersion1To2Async(connection, cancellationToken).ConfigureAwait(false);
            version = SchemaVersion;
        }

        if (version == SchemaVersion)
        {
            await ExecuteNonQueryAsync(connection, CreateSchemaSql(), cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException($"Unsupported prediction ledger schema version {version}.");
    }

    /// <inheritdoc />
    public async Task StorePredictionAsync(PredictionLedgerRecord prediction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prediction);
        await using var connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var existing = await ReadExistingPredictionFingerprintAsync(
            connection,
            prediction.LogicalKey,
            cancellationToken).ConfigureAwait(false);
        if (existing.Found)
        {
            if (string.Equals(existing.Fingerprint, prediction.ContentFingerprint, StringComparison.Ordinal))
            {
                return;
            }

            throw new PredictionLedgerIntegrityException("Prediction logical key already exists with different scientific content.");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO predictions (
                prediction_id,
                logical_key,
                prediction_content_fingerprint,
                generated_at_utc,
                fund_identifier_kind,
                fund_identifier_value,
                data_cutoff_date,
                dataset_provider,
                dataset_fingerprint,
                dataset_timestamp_utc,
                model_id,
                model_name,
                model_version,
                model_configuration_fingerprint,
                requested_horizon_value,
                requested_horizon_unit,
                observation_frequency,
                effective_observation_count,
                horizon_target_date,
                resolution_policy,
                resolution_is_approximation,
                forecast_capabilities,
                point_forecast_statistic,
                point_forecast,
                expected_return,
                median_return,
                probability_positive,
                quantiles_json,
                model_parameters_json,
                aletheia_version,
                state_schema_version,
                state_schema_fingerprint,
                feature_configuration_id,
                random_seed,
                signal,
                signal_strength,
                origin,
                simulated_generated_at_utc,
                training_start_index,
                training_end_index,
                training_start_date,
                training_end_date,
                prediction_cutoff_index,
                target_index,
                target_date,
                diagnostics_json)
            VALUES (
                $prediction_id,
                $logical_key,
                $prediction_content_fingerprint,
                $generated_at_utc,
                $fund_identifier_kind,
                $fund_identifier_value,
                $data_cutoff_date,
                $dataset_provider,
                $dataset_fingerprint,
                $dataset_timestamp_utc,
                $model_id,
                $model_name,
                $model_version,
                $model_configuration_fingerprint,
                $requested_horizon_value,
                $requested_horizon_unit,
                $observation_frequency,
                $effective_observation_count,
                $horizon_target_date,
                $resolution_policy,
                $resolution_is_approximation,
                $forecast_capabilities,
                $point_forecast_statistic,
                $point_forecast,
                $expected_return,
                $median_return,
                $probability_positive,
                $quantiles_json,
                $model_parameters_json,
                $aletheia_version,
                $state_schema_version,
                $state_schema_fingerprint,
                $feature_configuration_id,
                $random_seed,
                $signal,
                $signal_strength,
                $origin,
                $simulated_generated_at_utc,
                $training_start_index,
                $training_end_index,
                $training_start_date,
                $training_end_date,
                $prediction_cutoff_index,
                $target_index,
                $target_date,
                $diagnostics_json);
            """;

        AddPredictionParameters(command, prediction);
        await ExecuteIntegrityCheckedInsertAsync(command, "Prediction insert conflicted with an existing ledger row.", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StoreEvaluationAsync(PredictionEvaluationRecord evaluation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        await using var connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var existing = await ReadExistingEvaluationFingerprintAsync(
            connection,
            evaluation.PredictionId,
            cancellationToken).ConfigureAwait(false);
        if (existing.Found)
        {
            if (string.Equals(existing.Fingerprint, evaluation.EvaluationContentFingerprint, StringComparison.Ordinal))
            {
                return;
            }

            throw new PredictionLedgerIntegrityException("Prediction evaluation already exists with different scientific content.");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO prediction_evaluations (
                prediction_evaluation_id,
                prediction_id,
                evaluation_content_fingerprint,
                evaluated_at_utc,
                actual_return,
                actual_direction,
                predicted_direction,
                direction_rule,
                absolute_error,
                squared_error,
                direction_correct,
                probability_outcome,
                brier_contribution)
            VALUES (
                $prediction_evaluation_id,
                $prediction_id,
                $evaluation_content_fingerprint,
                $evaluated_at_utc,
                $actual_return,
                $actual_direction,
                $predicted_direction,
                $direction_rule,
                $absolute_error,
                $squared_error,
                $direction_correct,
                $probability_outcome,
                $brier_contribution);
            """;

        Add(command, "$prediction_evaluation_id", evaluation.PredictionEvaluationId.ToString());
        Add(command, "$prediction_id", evaluation.PredictionId.ToString());
        Add(command, "$evaluation_content_fingerprint", evaluation.EvaluationContentFingerprint);
        Add(command, "$evaluated_at_utc", FormatDateTime(evaluation.EvaluatedAtUtc));
        Add(command, "$actual_return", evaluation.ActualReturn);
        Add(command, "$actual_direction", (int)evaluation.ActualDirection);
        Add(command, "$predicted_direction", (int)evaluation.PredictedDirection);
        Add(command, "$direction_rule", (int)evaluation.DirectionRule);
        Add(command, "$absolute_error", evaluation.AbsoluteError);
        Add(command, "$squared_error", evaluation.SquaredError);
        Add(command, "$direction_correct", evaluation.DirectionCorrect ? 1 : 0);
        Add(command, "$probability_outcome", evaluation.ProbabilityOutcome);
        Add(command, "$brier_contribution", evaluation.BrierContribution);
        await ExecuteIntegrityCheckedInsertAsync(command, "Evaluation insert conflicted with an existing ledger row.", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PredictionLedgerRecord?> GetPredictionAsync(Guid predictionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM predictions WHERE prediction_id = $prediction_id;";
        Add(command, "$prediction_id", predictionId.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadPrediction(reader) : null;
    }

    /// <inheritdoc />
    public async Task<PredictionLedgerRecord?> GetPredictionByLogicalKeyAsync(string logicalKey, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM predictions WHERE logical_key = $logical_key;";
        Add(command, "$logical_key", logicalKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadPrediction(reader) : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PredictionLedgerRecord>> ListPredictionsAsync(int limit, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be positive.");
        }

        await using var connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM predictions ORDER BY generated_at_utc DESC, prediction_id DESC LIMIT $limit;";
        Add(command, "$limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var predictions = new List<PredictionLedgerRecord>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            predictions.Add(ReadPrediction(reader));
        }

        return predictions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PredictionEvaluationRecord>> GetEvaluationsAsync(Guid predictionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM prediction_evaluations WHERE prediction_id = $prediction_id ORDER BY evaluated_at_utc;";
        Add(command, "$prediction_id", predictionId.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var evaluations = new List<PredictionEvaluationRecord>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            evaluations.Add(ReadEvaluation(reader));
        }

        return evaluations;
    }

    private static string CreateSchemaSql()
    {
        return """
            CREATE TABLE IF NOT EXISTS predictions (
                prediction_id TEXT PRIMARY KEY,
                logical_key TEXT NOT NULL UNIQUE,
                prediction_content_fingerprint TEXT NOT NULL,
                generated_at_utc TEXT NOT NULL,
                fund_identifier_kind INTEGER NOT NULL,
                fund_identifier_value TEXT NOT NULL,
                data_cutoff_date TEXT NOT NULL,
                dataset_provider TEXT NOT NULL,
                dataset_fingerprint TEXT NOT NULL,
                dataset_timestamp_utc TEXT NULL,
                model_id TEXT NOT NULL,
                model_name TEXT NOT NULL,
                model_version TEXT NOT NULL,
                model_configuration_fingerprint TEXT NOT NULL,
                requested_horizon_value INTEGER NOT NULL,
                requested_horizon_unit INTEGER NOT NULL,
                observation_frequency INTEGER NOT NULL,
                effective_observation_count INTEGER NOT NULL,
                horizon_target_date TEXT NULL,
                resolution_policy TEXT NOT NULL,
                resolution_is_approximation INTEGER NOT NULL,
                forecast_capabilities INTEGER NOT NULL,
                point_forecast_statistic INTEGER NOT NULL,
                point_forecast REAL NOT NULL,
                expected_return REAL NOT NULL,
                median_return REAL NOT NULL,
                probability_positive REAL NOT NULL,
                quantiles_json TEXT NOT NULL,
                model_parameters_json TEXT NOT NULL,
                aletheia_version TEXT NOT NULL,
                state_schema_version TEXT NOT NULL,
                state_schema_fingerprint TEXT NOT NULL,
                feature_configuration_id TEXT NOT NULL,
                random_seed INTEGER NULL,
                signal INTEGER NULL,
                signal_strength REAL NULL,
                origin TEXT NOT NULL,
                simulated_generated_at_utc TEXT NULL,
                training_start_index INTEGER NOT NULL,
                training_end_index INTEGER NOT NULL,
                training_start_date TEXT NOT NULL,
                training_end_date TEXT NOT NULL,
                prediction_cutoff_index INTEGER NOT NULL,
                target_index INTEGER NULL,
                target_date TEXT NULL,
                diagnostics_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS prediction_evaluations (
                prediction_evaluation_id TEXT PRIMARY KEY,
                prediction_id TEXT NOT NULL,
                evaluation_content_fingerprint TEXT NOT NULL,
                evaluated_at_utc TEXT NOT NULL,
                actual_return REAL NOT NULL,
                actual_direction INTEGER NOT NULL,
                predicted_direction INTEGER NOT NULL,
                direction_rule INTEGER NOT NULL,
                absolute_error REAL NOT NULL,
                squared_error REAL NOT NULL,
                direction_correct INTEGER NOT NULL,
                probability_outcome INTEGER NOT NULL,
                brier_contribution REAL NOT NULL,
                UNIQUE(prediction_id),
                FOREIGN KEY(prediction_id) REFERENCES predictions(prediction_id)
            );
            """;
    }

    private static void AddPredictionParameters(SqliteCommand command, PredictionLedgerRecord record)
    {
        var prediction = record.Prediction;
        Add(command, "$prediction_id", prediction.PredictionId.ToString());
        Add(command, "$logical_key", record.LogicalKey);
        Add(command, "$prediction_content_fingerprint", record.ContentFingerprint);
        Add(command, "$generated_at_utc", FormatDateTime(prediction.GeneratedAtUtc));
        Add(command, "$fund_identifier_kind", (int)prediction.FundIdentifier.Kind);
        Add(command, "$fund_identifier_value", prediction.FundIdentifier.Value);
        Add(command, "$data_cutoff_date", FormatDate(prediction.DataCutoffDate));
        Add(command, "$dataset_provider", prediction.DatasetIdentity.DataProvider);
        Add(command, "$dataset_fingerprint", prediction.DatasetIdentity.DatasetFingerprintSha256);
        Add(command, "$dataset_timestamp_utc", FormatNullableDateTime(prediction.DatasetIdentity.DatasetTimestampUtc));
        Add(command, "$model_id", prediction.Model.Id);
        Add(command, "$model_name", prediction.Model.Name);
        Add(command, "$model_version", prediction.Model.Version);
        Add(command, "$model_configuration_fingerprint", record.ModelConfigurationFingerprint);
        Add(command, "$requested_horizon_value", prediction.RequestedHorizon.Value);
        Add(command, "$requested_horizon_unit", (int)prediction.RequestedHorizon.Unit);
        Add(command, "$observation_frequency", (int)prediction.ObservationFrequency);
        Add(command, "$effective_observation_count", prediction.EffectiveObservationCount);
        Add(command, "$horizon_target_date", FormatNullableDate(prediction.TargetDate));
        Add(command, "$resolution_policy", prediction.HorizonResolution.ResolutionPolicyName);
        Add(command, "$resolution_is_approximation", prediction.HorizonResolution.IsApproximation ? 1 : 0);
        Add(command, "$forecast_capabilities", (int)prediction.ForecastCapabilities);
        Add(command, "$point_forecast_statistic", (int)prediction.PointForecastStatistic);
        Add(command, "$point_forecast", prediction.PointForecastReturn);
        Add(command, "$expected_return", prediction.ExpectedReturn);
        Add(command, "$median_return", prediction.MedianReturn);
        Add(command, "$probability_positive", prediction.ProbabilityPositive);
        Add(command, "$quantiles_json", JsonSerializer.Serialize(prediction.ReturnPercentiles, JsonOptions));
        Add(command, "$model_parameters_json", JsonSerializer.Serialize(prediction.ModelParameters, JsonOptions));
        Add(command, "$aletheia_version", prediction.AletheiaVersion);
        Add(command, "$state_schema_version", prediction.StateSchemaVersion);
        Add(command, "$state_schema_fingerprint", prediction.StateSchemaFingerprint);
        Add(command, "$feature_configuration_id", prediction.FeatureConfigurationId);
        Add(command, "$random_seed", prediction.RandomSeed);
        Add(command, "$signal", prediction.Signal.HasValue ? (int)prediction.Signal.Value : null);
        Add(command, "$signal_strength", prediction.SignalStrength);
        Add(command, "$origin", record.Origin.ToString());
        Add(command, "$simulated_generated_at_utc", FormatNullableDateTime(record.SimulatedGeneratedAtUtc));
        Add(command, "$training_start_index", record.TrainingStartIndex);
        Add(command, "$training_end_index", record.TrainingEndIndex);
        Add(command, "$training_start_date", FormatDate(record.TrainingStartDate));
        Add(command, "$training_end_date", FormatDate(record.TrainingEndDate));
        Add(command, "$prediction_cutoff_index", record.PredictionCutoffIndex);
        Add(command, "$target_index", record.TargetIndex);
        Add(command, "$target_date", FormatNullableDate(record.TargetDate));
        Add(command, "$diagnostics_json", JsonSerializer.Serialize(record.DiagnosticMetadata, JsonOptions));
    }

    private static PredictionLedgerRecord ReadPrediction(SqliteDataReader reader)
    {
        var requestedHorizon = new ForecastHorizon(
            reader.GetInt32("requested_horizon_value"),
            (ForecastHorizonUnit)reader.GetInt32("requested_horizon_unit"));
        var horizonResolution = new ForecastHorizonResolution(
            requestedHorizon,
            (ObservationFrequency)reader.GetInt32("observation_frequency"),
            reader.GetInt32("effective_observation_count"),
            ParseNullableDate(reader.GetNullableString("horizon_target_date")),
            reader.GetString("resolution_policy"),
            reader.GetInt32("resolution_is_approximation") == 1);
        var signalValue = reader.GetNullableInt32("signal");
        var forecastCapabilities = (ForecastCapabilities)(reader.GetNullableInt32("forecast_capabilities") ??
            (int)(ForecastCapabilities.PointForecast |
                ForecastCapabilities.ExpectedReturn |
                ForecastCapabilities.Median |
                ForecastCapabilities.ProbabilityPositive |
                ForecastCapabilities.Quantiles));
        var pointForecastStatistic = (PointForecastStatistic)(reader.GetNullableInt32("point_forecast_statistic") ??
            (int)PointForecastStatistic.Median);
        var prediction = new PredictionRecord(
            Guid.Parse(reader.GetString("prediction_id")),
            new FundIdentifier(
                (FundIdentifierKind)reader.GetInt32("fund_identifier_kind"),
                reader.GetString("fund_identifier_value")),
            ParseDateTime(reader.GetString("generated_at_utc")),
            ParseDate(reader.GetString("data_cutoff_date")),
            horizonResolution,
            reader.GetDouble("point_forecast"),
            reader.GetDouble("expected_return"),
            reader.GetDouble("median_return"),
            reader.GetDouble("probability_positive"),
            DeserializeDictionary<int, double>(reader.GetString("quantiles_json")),
            new ModelDescriptor(
                reader.GetString("model_id"),
                reader.GetString("model_name"),
                reader.GetString("model_version")),
            DeserializeDictionary<string, string>(reader.GetString("model_parameters_json")),
            reader.GetString("aletheia_version"),
            reader.GetString("state_schema_version"),
            reader.GetString("state_schema_fingerprint"),
            new DatasetIdentity(
                reader.GetString("dataset_provider"),
                reader.GetString("dataset_fingerprint"),
                ParseNullableDateTime(reader.GetNullableString("dataset_timestamp_utc"))),
            reader.GetNullableInt32("random_seed"),
            signalValue.HasValue ? (InvestmentSignal?)signalValue.Value : null,
            reader.GetNullableDouble("signal_strength"),
            reader.GetString("feature_configuration_id"),
            forecastCapabilities,
            pointForecastStatistic);

        return new PredictionLedgerRecord(
            prediction,
            reader.GetString("logical_key"),
            reader.GetString("model_configuration_fingerprint"),
            Enum.Parse<PredictionOrigin>(reader.GetString("origin")),
            ParseNullableDateTime(reader.GetNullableString("simulated_generated_at_utc")),
            reader.GetInt32("training_start_index"),
            reader.GetInt32("training_end_index"),
            ParseDate(reader.GetString("training_start_date")),
            ParseDate(reader.GetString("training_end_date")),
            reader.GetInt32("prediction_cutoff_index"),
            reader.GetNullableInt32("target_index"),
            ParseNullableDate(reader.GetNullableString("target_date")),
            DeserializeDictionary<string, string>(reader.GetString("diagnostics_json")),
            reader.GetNullableString("prediction_content_fingerprint"));
    }

    private static PredictionEvaluationRecord ReadEvaluation(SqliteDataReader reader)
    {
        return new PredictionEvaluationRecord(
            Guid.Parse(reader.GetString("prediction_evaluation_id")),
            Guid.Parse(reader.GetString("prediction_id")),
            ParseDateTime(reader.GetString("evaluated_at_utc")),
            reader.GetDouble("actual_return"),
            (ForecastDirection)reader.GetInt32("actual_direction"),
            (ForecastDirection)reader.GetInt32("predicted_direction"),
            reader.GetDouble("absolute_error"),
            reader.GetDouble("squared_error"),
            reader.GetInt32("direction_correct") == 1,
            reader.GetInt32("probability_outcome"),
            reader.GetDouble("brier_contribution"),
            (DirectionPredictionRule)(reader.GetNullableInt32("direction_rule") ?? 0),
            reader.GetNullableString("evaluation_content_fingerprint"));
    }

    private static async Task MigrateFromVersion1To2Async(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            ALTER TABLE predictions ADD COLUMN prediction_content_fingerprint TEXT NULL;
            ALTER TABLE predictions ADD COLUMN forecast_capabilities INTEGER NOT NULL DEFAULT {(int)(ForecastCapabilities.PointForecast | ForecastCapabilities.ExpectedReturn | ForecastCapabilities.Median | ForecastCapabilities.ProbabilityPositive | ForecastCapabilities.Quantiles)};
            ALTER TABLE predictions ADD COLUMN point_forecast_statistic INTEGER NOT NULL DEFAULT {(int)PointForecastStatistic.Median};
            ALTER TABLE prediction_evaluations ADD COLUMN evaluation_content_fingerprint TEXT NULL;
            ALTER TABLE prediction_evaluations ADD COLUMN direction_rule INTEGER NOT NULL DEFAULT {(int)DirectionPredictionRule.Automatic};
            UPDATE schema_version SET version = {SchemaVersion};
            """;
        await ExecuteNonQueryAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(bool Found, string? Fingerprint)> ReadExistingPredictionFingerprintAsync(
        SqliteConnection connection,
        string logicalKey,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT prediction_content_fingerprint FROM predictions WHERE logical_key = $logical_key;";
        Add(command, "$logical_key", logicalKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return (false, null);
        }

        return (true, reader.GetNullableString("prediction_content_fingerprint"));
    }

    private static async Task<(bool Found, string? Fingerprint)> ReadExistingEvaluationFingerprintAsync(
        SqliteConnection connection,
        Guid predictionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT evaluation_content_fingerprint FROM prediction_evaluations WHERE prediction_id = $prediction_id;";
        Add(command, "$prediction_id", predictionId.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return (false, null);
        }

        return (true, reader.GetNullableString("evaluation_content_fingerprint"));
    }

    private static Dictionary<TKey, TValue> DeserializeDictionary<TKey, TValue>(string json)
        where TKey : notnull
    {
        return JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json, JsonOptions) ?? new Dictionary<TKey, TValue>();
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ExecuteIntegrityCheckedInsertAsync(
        SqliteCommand command,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new PredictionLedgerIntegrityException(message);
        }
    }

    private static void Add(SqliteCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string? FormatNullableDate(DateOnly? date)
    {
        return date.HasValue ? FormatDate(date.Value) : null;
    }

    private static DateOnly ParseDate(string value)
    {
        return DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParseNullableDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseDate(value);
    }

    private static string FormatDateTime(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static string? FormatNullableDateTime(DateTimeOffset? value)
    {
        return value.HasValue ? FormatDateTime(value.Value) : null;
    }

    private static DateTimeOffset ParseDateTime(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static DateTimeOffset? ParseNullableDateTime(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseDateTime(value);
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(this.connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
