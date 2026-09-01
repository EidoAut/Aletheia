using Aletheia.Core;
using Aletheia.Data;

namespace Aletheia.Data.Tests;

public sealed class CnmvIicParserTests
{
    private const string RegistrationXml = """
        <Datos>
          <FechaDatos>202401</FechaDatos>
          <Entidad>
            <Tipo>FI</Tipo>
            <NumeroRegistro>123</NumeroRegistro>
            <Denominacion>FONDO ALFA GLOBAL</Denominacion>
            <Gestora>
              <DenominacionGestora>GESTORA ÑANDU</DenominacionGestora>
            </Gestora>
            <Compartimento>
              <NumeroCompartimento>1</NumeroCompartimento>
              <Clase>
                <NumeroClase>1</NumeroClase>
                <DenominacionClase>Clase A</DenominacionClase>
                <ISIN>ES0000000001</ISIN>
              </Clase>
              <Clase>
                <NumeroClase>2</NumeroClase>
                <DenominacionClase>Clase B</DenominacionClase>
                <ISIN>ES0000000002</ISIN>
              </Clase>
            </Compartimento>
          </Entidad>
        </Datos>
        """;

    private const string MonthlyNavXml = """
        <Datos>
          <FechaDatos>202401</FechaDatos>
          <Entidad>
            <Compartimento>
              <Clase>
                <ISIN>ES0000000001</ISIN>
                <VLDiario>
                  <VL_Dia1>10.00</VL_Dia1>
                  <VL_Dia3>10.25</VL_Dia3>
                  <VL_Dia4>0</VL_Dia4>
                </VLDiario>
              </Clase>
              <Clase>
                <ISIN>ES0000000002</ISIN>
                <VLDiario>
                  <VL_Dia1>999.00</VL_Dia1>
                </VLDiario>
              </Clase>
            </Compartimento>
          </Entidad>
        </Datos>
        """;

    [Fact]
    public void ParseRegistrations_UsesExactShareClassIdentityAndManager()
    {
        var parser = new CnmvIicParser();

        var registrations = parser.ParseRegistrations(RegistrationXml);

        Assert.Equal(2, registrations.Count);
        Assert.Contains(registrations, item =>
            item.Isin == "ES0000000001" &&
            item.FundName == "FONDO ALFA GLOBAL" &&
            item.ManagerName == "GESTORA ÑANDU" &&
            item.ClassName == "Clase A");
        Assert.Contains(registrations, item => item.Isin == "ES0000000002");
    }

    [Fact]
    public void ParseMonthlyNavs_ReturnsOnlyExactIsinObservationsWithoutFillingGaps()
    {
        var parser = new CnmvIicParser();

        var points = parser.ParseMonthlyNavs(MonthlyNavXml, "ES0000000001");

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateOnly(2024, 1, 1), points[0].Date);
        Assert.Equal(new DateOnly(2024, 1, 3), points[1].Date);
        Assert.DoesNotContain(points, point => point.Date == new DateOnly(2024, 1, 2));
        Assert.DoesNotContain(points, point => point.Value == 999m);
    }

    [Fact]
    public void ObservationFrequencyDetector_DetectsTrueCadenceWithoutRepairingSeries()
    {
        var daily = new[]
        {
            new NavPoint(new DateOnly(2024, 1, 1), 10m),
            new NavPoint(new DateOnly(2024, 1, 2), 11m),
            new NavPoint(new DateOnly(2024, 1, 3), 12m),
        };
        var businessDaily = new[]
        {
            new NavPoint(new DateOnly(2024, 1, 5), 10m),
            new NavPoint(new DateOnly(2024, 1, 8), 11m),
            new NavPoint(new DateOnly(2024, 1, 9), 12m),
        };
        var irregular = new[]
        {
            new NavPoint(new DateOnly(2024, 1, 1), 10m),
            new NavPoint(new DateOnly(2024, 1, 3), 11m),
        };

        Assert.Equal(ObservationFrequency.Daily, ObservationFrequencyDetector.Detect(daily));
        Assert.Equal(ObservationFrequency.BusinessDaily, ObservationFrequencyDetector.Detect(businessDaily));
        Assert.Equal(ObservationFrequency.Irregular, ObservationFrequencyDetector.Detect(irregular));
    }

    [Fact]
    public void ObservationFrequencyDetector_AllowsSparseHolidayGapsWithoutRepairingSeries()
    {
        var observations = new[]
        {
            new NavPoint(new DateOnly(2024, 1, 2), 10m),
            new NavPoint(new DateOnly(2024, 1, 3), 11m),
            new NavPoint(new DateOnly(2024, 1, 5), 12m),
            new NavPoint(new DateOnly(2024, 1, 8), 13m),
            new NavPoint(new DateOnly(2024, 1, 9), 14m),
        };

        var frequency = ObservationFrequencyDetector.Detect(observations);

        Assert.Equal(ObservationFrequency.BusinessDaily, frequency);
        Assert.Equal(5, observations.Length);
        Assert.DoesNotContain(observations, point => point.Date == new DateOnly(2024, 1, 4));
    }

    [Fact]
    public void ObservationFrequencyDetector_DetectsWeeklyCadenceWithoutTreatingItAsSparseBusinessDaily()
    {
        var observations = new[]
        {
            new NavPoint(new DateOnly(2024, 1, 2), 10m),
            new NavPoint(new DateOnly(2024, 1, 9), 11m),
            new NavPoint(new DateOnly(2024, 1, 16), 12m),
            new NavPoint(new DateOnly(2024, 1, 23), 13m),
        };

        Assert.Equal(ObservationFrequency.Weekly, ObservationFrequencyDetector.Detect(observations));
    }

    [Fact]
    public void ObservationFrequencyDetector_AllowsOccasionalWeekendReportsInBusinessDailySeries()
    {
        var start = new DateOnly(2024, 1, 1);
        var observations = Enumerable.Range(0, 19)
            .Select(offset => start.AddDays(offset))
            .Where(date => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday ||
                date == new DateOnly(2024, 1, 6))
            .Select((date, index) => new NavPoint(date, 100m + index))
            .ToArray();

        Assert.Equal(ObservationFrequency.BusinessDaily, ObservationFrequencyDetector.Detect(observations));
    }

    [Fact]
    public void ObservationFrequencyDetector_DetectsMonthlyCadenceWithOneMissingMonth()
    {
        var observations = new[]
        {
            new NavPoint(new DateOnly(2024, 1, 31), 10m),
            new NavPoint(new DateOnly(2024, 2, 29), 11m),
            new NavPoint(new DateOnly(2024, 4, 30), 12m),
            new NavPoint(new DateOnly(2024, 5, 31), 13m),
        };

        Assert.Equal(ObservationFrequency.Monthly, ObservationFrequencyDetector.Detect(observations));
    }

    [Fact]
    public void ObservationFrequencyDetector_LeavesGenuinelyUnevenCadenceIrregular()
    {
        var observations = new[]
        {
            new NavPoint(new DateOnly(2024, 1, 2), 10m),
            new NavPoint(new DateOnly(2024, 1, 5), 11m),
            new NavPoint(new DateOnly(2024, 1, 19), 12m),
            new NavPoint(new DateOnly(2024, 2, 2), 13m),
            new NavPoint(new DateOnly(2024, 3, 21), 14m),
        };

        Assert.Equal(ObservationFrequency.Irregular, ObservationFrequencyDetector.Detect(observations));
    }

    [Fact]
    public void FundSearchQuery_NormalizesNamesManagersAndPartialIsins()
    {
        var text = FundSearchQuery.NormalizeSearchText("Gestora Ñandú, clase A");
        var query = FundSearchQuery.FromUserText(" es0000 ");

        Assert.Equal("GESTORA NANDU CLASE A", text);
        Assert.Equal("ES0000", query.Isin);
        Assert.Equal("ES0000", query.Text);
    }
}
