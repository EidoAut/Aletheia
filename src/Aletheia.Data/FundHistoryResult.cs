using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Couples a fund history with its source provenance.
/// </summary>
public sealed record FundHistoryResult(FundHistory History, FundDataProvenance Provenance);
