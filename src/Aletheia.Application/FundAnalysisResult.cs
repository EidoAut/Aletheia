using Aletheia.Core;
using Aletheia.Data;
using Aletheia.Dynamics;
using Aletheia.Spectral;

namespace Aletheia.Application;

/// <summary>
/// Stores all reusable analysis results for one loaded fund.
/// </summary>
public sealed record FundAnalysisResult(
    DatasetSummary Dataset,
    DataQualityReport DataQuality,
    PerformanceSummary Performance,
    DistributionSummary ReturnDistribution,
    IReadOnlyList<DatedValue> Nav,
    IReadOnlyList<DatedValue> CumulativeReturn,
    IReadOnlyList<DatedValue> SimpleReturns,
    IReadOnlyList<DatedValue> LogReturns,
    IReadOnlyList<DatedValue> RollingReturn,
    IReadOnlyList<DatedValue> RollingVolatility,
    IReadOnlyList<DatedValue> Drawdown,
    DynamicState CurrentState,
    IReadOnlyList<StateObservation> StateHistory,
    IReadOnlyList<StateProjectionPoint> StateProjection,
    SpectralAnalysisResult Spectrum,
    RollingSpectralStabilityResult SpectralStability,
    DynamicModelResult ArFit,
    DynamicForecast ArForecast,
    AnalogueAnalysisResult Analogues,
    ForecastCollectionResult Forecasts,
    FundResearchReport? ResearchReport = null,
    MarketTimingAssessment? MarketTiming = null);
