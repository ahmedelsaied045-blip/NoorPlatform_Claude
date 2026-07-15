using System.Globalization;
using System.Text.RegularExpressions;
using static System.FormattableString;
using Nop.Plugin.Misc.NoorAiAssistant.Models;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Lighting;

/// <summary>
/// The lumen method, which is how lighting is actually sized:
///   required lumens = floor area x target illuminance (lux)
///   fixture count   = required lumens / lumens per fixture
/// Everything else here is picking sensible values for those terms and rounding the result to something a
/// shopper can buy.
/// </summary>
public partial class LightingCalculator : ILightingCalculator
{
    #region Fields

    private readonly NoorAiAssistantSettings _settings;

    /// <summary>
    /// Matches "5 x 4", "5*4", "5 × 4", "5 by 4", "5 في 4", "5m x 4m", "5.5x4".
    /// Arabic-Indic digits and Arabic letter forms are already folded by TextNormalizer.
    /// </summary>
    private static readonly Regex _dimensions = DimensionsRegex();

    /// <summary>
    /// Matches a stated area: "20 sqm", "20 square meters", "20 m2", "20 متر مربع".
    /// </summary>
    private static readonly Regex _area = AreaRegex();

    /// <summary>
    /// The light output assumed for one fixture, in lumens, when the shopper has not named a product.
    /// A mainstream 7-9W LED spotlight lands here.
    /// </summary>
    private const int DEFAULT_FIXTURE_LUMENS = 800;

    /// <summary>
    /// Wattages a shopper can actually buy. The computed figure is snapped to the nearest of these rather
    /// than being reported as, say, 8.4W.
    /// </summary>
    private static readonly int[] _standardWattages = [3, 5, 7, 9, 12, 15, 18, 20, 24, 30, 36, 50];

    #endregion

    #region Ctor

    public LightingCalculator(NoorAiAssistantSettings settings)
    {
        _settings = settings;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Parses a number written with either decimal separator. The regex accepts "5,5" because that is how
    /// half the world writes it, so the comma has to be folded before an invariant parse.
    /// </summary>
    private static bool TryParseNumber(string value, out double result)
    {
        return double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Snaps a computed wattage to the nearest wattage the store is likely to stock.
    /// </summary>
    private static int SnapToStandardWattage(double watts)
    {
        if (watts <= _standardWattages[0])
            return _standardWattages[0];

        if (watts >= _standardWattages[^1])
            return _standardWattages[^1];

        return _standardWattages.OrderBy(w => Math.Abs(w - watts)).First();
    }

    /// <summary>
    /// Arranges <paramref name="count"/> fixtures into the grid that best matches the room's proportions,
    /// so a long narrow room gets a long narrow grid rather than a square one.
    /// </summary>
    private static string SuggestLayout(int count, RoomDimensions dimensions)
    {
        if (count <= 1 || !dimensions.HasBothSides)
            return null;

        var longSide = Math.Max(dimensions.LengthMeters, dimensions.WidthMeters);
        var shortSide = Math.Min(dimensions.LengthMeters, dimensions.WidthMeters);
        var targetRatio = longSide / shortSide;

        var best = (rows: 1, cols: count, error: double.MaxValue);
        for (var rows = 1; rows <= count; rows++)
        {
            //only consider grids that hold every fixture without leaving a ragged row
            if (count % rows != 0)
                continue;

            var cols = count / rows;
            var error = Math.Abs((double)Math.Max(rows, cols) / Math.Min(rows, cols) - targetRatio);
            if (error < best.error)
                best = (rows, cols, error);
        }

        //a prime count (7, 11, ...) only factors as 1 x n, which is a silly layout for a room;
        //report it as a bare count instead of pretending a single row is a plan
        if (best.rows == 1 && count > 3)
            return null;

        return $"{Math.Max(best.rows, best.cols)} x {Math.Min(best.rows, best.cols)}";
    }

    /// <summary>
    /// Names a colour temperature the way a shopper would recognise it.
    /// </summary>
    private static string DescribeColorTemperature(int kelvin, bool arabic)
    {
        return kelvin switch
        {
            <= 2700 => arabic ? $"أبيض دافئ جدًا ({kelvin}K)" : $"Extra warm white ({kelvin}K)",
            <= 3200 => arabic ? $"أبيض دافئ ({kelvin}K)" : $"Warm white ({kelvin}K)",
            <= 4500 => arabic ? $"أبيض طبيعي ({kelvin}K)" : $"Natural white ({kelvin}K)",
            _ => arabic ? $"أبيض بارد ({kelvin}K)" : $"Cool white ({kelvin}K)"
        };
    }

    #endregion

    #region Methods

    /// <summary>
    /// Reads room dimensions out of a message
    /// </summary>
    public virtual RoomDimensions ParseDimensions(string normalizedMessage)
    {
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return null;

        var match = _dimensions.Match(normalizedMessage);
        if (match.Success &&
            TryParseNumber(match.Groups["a"].Value, out var a) &&
            TryParseNumber(match.Groups["b"].Value, out var b) &&
            //reject nonsense: a 0.4m room is a typo, a 200m one is not a room
            a is >= 0.5 and <= 100 && b is >= 0.5 and <= 100)
        {
            return new RoomDimensions
            {
                LengthMeters = Math.Round(a, 2),
                WidthMeters = Math.Round(b, 2),
                AreaSquareMeters = Math.Round(a * b, 2)
            };
        }

        var areaMatch = _area.Match(normalizedMessage);
        if (areaMatch.Success &&
            TryParseNumber(areaMatch.Groups["area"].Value, out var area) &&
            area is >= 1 and <= 10000)
        {
            return new RoomDimensions { AreaSquareMeters = Math.Round(area, 2) };
        }

        return null;
    }

    /// <summary>
    /// Produces a full lighting plan
    /// </summary>
    public virtual LightingPlanDto CalculatePlan(RoomDimensions dimensions, RoomProfile room, NeedProfile fixture)
    {
        ArgumentNullException.ThrowIfNull(dimensions);

        room ??= RoomCatalog.Default;

        //a fixture-specific colour temperature (a 5000K floodlight) beats the room's, because the shopper
        //named the fixture; otherwise the room decides
        var kelvin = fixture is { IsLightFixture: true, ColorTemperatureKelvin: > 0 }
            ? fixture.ColorTemperatureKelvin
            : room.ColorTemperatureKelvin;

        var luxTarget = room.LuxTarget > 0 ? room.LuxTarget : _settings.DefaultLuxTarget;
        var lumensPerWatt = _settings.DefaultLumensPerWatt > 0 ? _settings.DefaultLumensPerWatt : 100;

        var totalLumens = dimensions.AreaSquareMeters * luxTarget;

        var count = (int)Math.Ceiling(totalLumens / DEFAULT_FIXTURE_LUMENS);
        count = Math.Clamp(count, 1, 200);

        //having fixed the count, back out the wattage each one actually has to deliver, so the plan stays
        //internally consistent after the count was rounded up
        var wattsPerFixture = SnapToStandardWattage(totalLumens / count / lumensPerWatt);

        return new LightingPlanDto
        {
            RoomLengthMeters = dimensions.LengthMeters,
            RoomWidthMeters = dimensions.WidthMeters,
            AreaSquareMeters = dimensions.AreaSquareMeters,
            RoomType = room.Slug,
            LuxTarget = luxTarget,
            TotalLumensRequired = (int)Math.Round(totalLumens),
            FixtureCount = count,
            WattsPerFixture = wattsPerFixture,
            TotalWatts = count * wattsPerFixture,
            ColorTemperatureKelvin = kelvin,
            SuggestedLayout = SuggestLayout(count, dimensions),
            RecommendedCategory = (fixture ?? NeedCatalog.FindBySlug("spotlight"))?.Slug
        };
    }

    /// <summary>
    /// Answers "how many do I need?"
    /// </summary>
    public virtual QuantityEstimateDto EstimateQuantity(NeedProfile fixture, RoomProfile room, RoomDimensions dimensions, bool arabic)
    {
        room ??= RoomCatalog.Default;
        fixture ??= NeedCatalog.FindBySlug("spotlight");

        var assumptions = new List<string>();

        if (dimensions is null)
        {
            //a 4x4 room is the median room in a villa; state the assumption rather than hiding it
            dimensions = new RoomDimensions { LengthMeters = 4, WidthMeters = 4, AreaSquareMeters = 16 };
            assumptions.Add(arabic
                ? "افترضت غرفة بمقاس 4 × 4 متر (16 م²) — أخبرني بمقاس غرفتك لحساب أدق."
                : "I assumed a 4 x 4 m room (16 m²) — tell me your room size for an exact figure.");
        }

        //an LED strip is bought by the metre, not by the piece, so it is measured rather than counted
        if (fixture.Slug == "led-strip")
        {
            var perimeter = dimensions.HasBothSides
                ? 2 * (dimensions.LengthMeters + dimensions.WidthMeters)
                //fall back to the perimeter of a square of the same area
                : 4 * Math.Sqrt(dimensions.AreaSquareMeters);

            //10% spare for corners, offcuts and the run back to the driver
            var meters = (int)Math.Ceiling(perimeter * 1.1);

            assumptions.Add(arabic
                ? "الحساب لمحيط الغرفة بالكامل (كورنيش مخفي) مع 10% إضافية للزوايا والهدر."
                : "Calculated for the full room perimeter (cove lighting) plus 10% for corners and offcuts.");

            //Invariant: the thread culture follows the storefront, not the language being answered in
            var perimeterText = Invariant($"{perimeter:0.#}");

            return new QuantityEstimateDto
            {
                ItemLabel = fixture.GetName(arabic),
                Quantity = meters,
                Unit = arabic ? "متر" : "m",
                Explanation = arabic
                    ? $"محيط الغرفة {perimeterText} متر، وبإضافة 10% للهدر تحتاج نحو **{meters} متر** من شريط الليد."
                    : $"The room perimeter is {perimeterText} m; with 10% spare you need about **{meters} m** of LED strip.",
                Assumptions = assumptions
            };
        }

        var plan = CalculatePlan(dimensions, room, fixture);

        assumptions.Add(arabic
            ? $"مستوى إضاءة {plan.LuxTarget} لوكس، وهو المناسب لـ{room.GetName(true)}."
            : $"A {plan.LuxTarget} lux target, which is what a {room.GetName(false)} wants.");
        assumptions.Add(arabic
            ? $"إضاءة {DEFAULT_FIXTURE_LUMENS} لومن لكل وحدة (سبوت LED بقدرة {plan.WattsPerFixture} واط تقريبًا)."
            : $"{DEFAULT_FIXTURE_LUMENS} lumens per fixture (roughly a {plan.WattsPerFixture}W LED).");

        var areaText = Invariant($"{plan.AreaSquareMeters:0.#}");
        var lumensText = Invariant($"{plan.TotalLumensRequired:N0}");

        var explanation = arabic
            ? $"مساحة {areaText} م² × {plan.LuxTarget} لوكس = **{lumensText} لومن**. " +
              $"بتقسيمها على {DEFAULT_FIXTURE_LUMENS} لومن للوحدة ينتج **{plan.FixtureCount} وحدة**."
            : $"{areaText} m² × {plan.LuxTarget} lux = **{lumensText} lumens**. " +
              $"Divided by {DEFAULT_FIXTURE_LUMENS} lumens per fixture, that is **{plan.FixtureCount} fixtures**.";

        return new QuantityEstimateDto
        {
            ItemLabel = fixture.GetName(arabic),
            Quantity = plan.FixtureCount,
            Unit = arabic ? "قطعة" : "pcs",
            Explanation = explanation,
            Assumptions = assumptions
        };
    }

    /// <summary>
    /// Names a colour temperature the way a shopper would recognise it
    /// </summary>
    public static string DescribeKelvin(int kelvin, bool arabic)
    {
        return DescribeColorTemperature(kelvin, arabic);
    }

    #endregion

    #region Generated regexes

    [GeneratedRegex(@"(?<a>\d+(?:[.,]\d+)?)\s*(?:m|متر)?\s*(?:x|\*|×|by|في)\s*(?<b>\d+(?:[.,]\d+)?)\s*(?:m|متر)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase, 200)]
    private static partial Regex DimensionsRegex();

    [GeneratedRegex(@"(?<area>\d+(?:[.,]\d+)?)\s*(?:sqm|sq m|square met(?:er|re)s?|m2|m²|متر مربع|متر²)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase, 200)]
    private static partial Regex AreaRegex();

    #endregion
}
