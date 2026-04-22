using CommandSynergy.Application.Contracts;
using Microsoft.AspNetCore.Components;
namespace CommandSynergy.Components.Decks;

/// <summary>
/// Renders the optional deck-stat charts once the user opts into the deferred visualization panel.
/// </summary>
public partial class DeckStatsPanel : ComponentBase
{
    private const decimal MinimumBarHeightPercent = 8m;
    private static readonly IReadOnlyDictionary<string, string> ChartColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["White"] = "oklch(0.95 0.03 95)",
        ["Blue"] = "oklch(0.69 0.11 240)",
        ["Black"] = "oklch(0.36 0.02 250)",
        ["Red"] = "oklch(0.63 0.17 32)",
        ["Green"] = "oklch(0.62 0.14 150)",
        ["Colorless"] = "oklch(0.72 0.02 90)",
        ["Any"] = "oklch(0.76 0.1 65)",
        ["Creature"] = "oklch(0.68 0.1 135)",
        ["Artifact"] = "oklch(0.75 0.03 220)",
        ["Enchantment"] = "oklch(0.8 0.08 320)",
        ["Instant"] = "oklch(0.7 0.12 250)",
        ["Sorcery"] = "oklch(0.69 0.15 24)",
        ["Planeswalker"] = "oklch(0.63 0.12 15)",
        ["Land"] = "oklch(0.74 0.06 115)",
        ["Battle"] = "oklch(0.67 0.13 12)",
        ["Kindred"] = "oklch(0.7 0.11 285)",
        ["Other"] = "oklch(0.78 0.02 135)",
        ["0"] = "oklch(0.83 0.03 120)",
        ["1"] = "oklch(0.8 0.05 110)",
        ["2"] = "oklch(0.76 0.07 100)",
        ["3"] = "oklch(0.72 0.09 90)",
        ["4"] = "oklch(0.68 0.11 75)",
        ["5"] = "oklch(0.64 0.13 60)",
        ["6"] = "oklch(0.61 0.15 48)",
        ["7"] = "oklch(0.57 0.16 36)",
        ["8+"] = "oklch(0.52 0.15 28)",
    };

    [Parameter]
    public DeckAnalysisResponseContract? Analysis { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public bool HasError { get; set; }

    private bool isExpanded;

    private DeckStatsContract? DeckStats => Analysis?.DeckStats;

    private bool HasDeckStats => DeckStats is not null;

    private decimal MaxManaValueBucketValue => DeckStats?.ManaValueHistogram.Max(static slice => slice.Value) ?? 0m;

    private IReadOnlyList<CurvePoint> CurvePoints => BuildCurvePoints();

    private string CurvePolylinePoints => string.Join(" ", CurvePoints.Select(point => $"{point.X.ToString("0.##")},{point.Y.ToString("0.##")}"));

    private string CurveAreaPath
    {
        get
        {
            if (CurvePoints.Count == 0)
            {
                return string.Empty;
            }

            var first = CurvePoints[0];
            var last = CurvePoints[^1];
            var points = string.Join(" L ", CurvePoints.Select(point => $"{point.X.ToString("0.##")} {point.Y.ToString("0.##")}"));
            return $"M {first.X.ToString("0.##")} 154 L {points} L {last.X.ToString("0.##")} 154 Z";
        }
    }

    private void Expand()
    {
        isExpanded = true;
    }

    private static string FormatStatValue(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("0") : value.ToString("0.#");

    private static string GetBarHeightStyle(decimal value, decimal maxValue)
    {
        var height = maxValue <= 0m ? 0m : Math.Max(MinimumBarHeightPercent, Math.Round((value / maxValue) * 100m, 2));
        return $"height: {height.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}%;";
    }

    private IReadOnlyList<CurvePoint> BuildCurvePoints()
    {
        if (DeckStats?.ManaCurve.Buckets.Count is not > 0)
        {
            return Array.Empty<CurvePoint>();
        }

        var maxValue = DeckStats.ManaCurve.Buckets.Max(static slice => slice.Value);
        if (maxValue <= 0m)
        {
            return DeckStats.ManaCurve.Buckets
                .Select((slice, index) => new CurvePoint(
                    ManaCurveChartDimensions.Left + ((ManaCurveChartDimensions.Width / Math.Max(DeckStats.ManaCurve.Buckets.Count - 1, 1)) * index),
                    ManaCurveChartDimensions.Bottom,
                    slice))
                .ToArray();
        }

        var step = ManaCurveChartDimensions.Width / Math.Max(DeckStats.ManaCurve.Buckets.Count - 1, 1);
        return DeckStats.ManaCurve.Buckets
            .Select((slice, index) =>
            {
                var x = ManaCurveChartDimensions.Left + (step * index);
                var y = ManaCurveChartDimensions.Bottom - ((slice.Value / maxValue) * ManaCurveChartDimensions.Height);
                return new CurvePoint(x, y, slice);
            })
            .ToArray();
    }

    private static string BuildPieStyle(IReadOnlyList<DeckStatSliceContract> slices)
    {
        decimal currentStart = 0m;
        List<string> segments = new(slices.Count);

        foreach (var slice in slices)
        {
            var end = currentStart + (slice.Share * 360m);
            segments.Add($"{ResolveColor(slice.Label)} {currentStart.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}deg {end.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}deg");
            currentStart = end;
        }

        return $"background: conic-gradient({string.Join(", ", segments)});";
    }

    private static string ResolveColor(string label) =>
        ChartColors.TryGetValue(label, out var color)
            ? color
            : "oklch(0.78 0.03 120)";

    private sealed record ChartDimensions(decimal Left, decimal Width, decimal Height, decimal Bottom);

    private sealed record CurvePoint(decimal X, decimal Y, DeckStatSliceContract Slice);

    private static readonly ChartDimensions ManaCurveChartDimensions = new(24m, 312m, 126m, 154m);
}
