using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Parses CNMV IIC XML payloads.
/// </summary>
public sealed class CnmvIicParser
{
    private const long MaximumXmlCharacters = 20_000_000L;
    private static readonly Regex DailyNavNamePattern = new("^VL_Dia(?<day>[0-9]{1,2})$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Parses CNMV fund registration XML.
    /// </summary>
    /// <param name="xml">The FONDREGISTRO XML payload.</param>
    /// <returns>Registered fund share classes.</returns>
    public IReadOnlyList<CnmvFundRegistration> ParseRegistrations(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new FundProviderException(FundProviderErrorKind.InvalidResponse, "CNMV registration XML is empty.");
        }

        var document = LoadXml(xml);
        var period = ElementValue(document.Root, "FechaDatos");
        var results = new List<CnmvFundRegistration>();
        foreach (var entity in document.Root?.Elements("Entidad") ?? [])
        {
            var fundType = ElementValue(entity, "Tipo");
            var registerNumber = ParseNullableInt(ElementValue(entity, "NumeroRegistro"));
            var fundName = ElementValue(entity, "Denominacion");
            var managerName = ElementValue(entity.Element("Gestora"), "DenominacionGestora");
            if (string.IsNullOrWhiteSpace(fundName))
            {
                continue;
            }

            foreach (var compartment in entity.Elements("Compartimento"))
            {
                var compartmentNumber = ParseNullableInt(ElementValue(compartment, "NumeroCompartimento"));
                foreach (var shareClass in compartment.Elements("Clase"))
                {
                    var isin = FundSearchQuery.NormalizeIsin(ElementValue(shareClass, "ISIN"));
                    if (isin is null || !Isin.IsValid(isin))
                    {
                        continue;
                    }

                    var classNumber = ParseNullableInt(ElementValue(shareClass, "NumeroClase"));
                    var className = ElementValue(shareClass, "DenominacionClase");
                    var sourceReference = string.Join(
                        "; ",
                        new[]
                        {
                            $"CNMV FONDREGISTRO {period ?? "unknown-period"}",
                            registerNumber.HasValue ? $"fund register {registerNumber.Value.ToString(CultureInfo.InvariantCulture)}" : null,
                            compartmentNumber.HasValue ? $"compartment {compartmentNumber.Value.ToString(CultureInfo.InvariantCulture)}" : null,
                            classNumber.HasValue ? $"class {classNumber.Value.ToString(CultureInfo.InvariantCulture)}" : null,
                        }.Where(item => item is not null));
                    results.Add(new CnmvFundRegistration(
                        fundName,
                        isin,
                        managerName,
                        fundType,
                        registerNumber,
                        compartmentNumber,
                        classNumber,
                        className,
                        sourceReference));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Parses daily NAV observations for one ISIN from CNMV FONDMENS XML.
    /// </summary>
    /// <param name="xml">The FONDMENS XML payload.</param>
    /// <param name="isin">The exact ISIN.</param>
    /// <returns>The reported observations, without interpolation or filling.</returns>
    public IReadOnlyList<NavPoint> ParseMonthlyNavs(string xml, string isin)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new FundProviderException(FundProviderErrorKind.InvalidResponse, "CNMV monthly NAV XML is empty.");
        }

        var normalizedIsin = FundSearchQuery.NormalizeIsin(isin);
        if (normalizedIsin is null)
        {
            throw new ArgumentException("ISIN cannot be empty.", nameof(isin));
        }

        var document = LoadXml(xml);
        var period = ElementValue(document.Root, "FechaDatos");
        if (period is null || period.Length != 6 ||
            !int.TryParse(period[..4], NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
            !int.TryParse(period[4..], NumberStyles.None, CultureInfo.InvariantCulture, out var month))
        {
            throw new FundProviderException(FundProviderErrorKind.InvalidResponse, "CNMV monthly NAV XML does not contain a valid FechaDatos value.");
        }

        var points = new List<NavPoint>();
        foreach (var shareClass in document.Root?.Descendants("Clase") ?? [])
        {
            var classIsin = FundSearchQuery.NormalizeIsin(ElementValue(shareClass, "ISIN"));
            if (!string.Equals(classIsin, normalizedIsin, StringComparison.Ordinal))
            {
                continue;
            }

            var dailyNav = shareClass.Element("VLDiario");
            if (dailyNav is null)
            {
                continue;
            }

            foreach (var element in dailyNav.Elements())
            {
                var match = DailyNavNamePattern.Match(element.Name.LocalName);
                if (!match.Success)
                {
                    continue;
                }

                var day = int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture);
                if (day < 1 || day > DateTime.DaysInMonth(year, month))
                {
                    continue;
                }

                if (!decimal.TryParse(
                    element.Value.Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value))
                {
                    continue;
                }

                if (value <= 0m)
                {
                    continue;
                }

                points.Add(new NavPoint(new DateOnly(year, month, day), value));
            }
        }

        return points.OrderBy(point => point.Date).ToArray();
    }

    private static string? ElementValue(XContainer? container, string name)
    {
        var value = container?.Element(name)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static XDocument LoadXml(string xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = MaximumXmlCharacters,
            XmlResolver = null,
        };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
