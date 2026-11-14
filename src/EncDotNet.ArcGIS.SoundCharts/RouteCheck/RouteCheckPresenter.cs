using System.Globalization;
using EncDotNet.S100.Navigation;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// Turns a route check into what the <b>SoundCharts Route check</b> pane
/// shows, in the viewer's words: the verdict card (title, subtitle, summary
/// rows, footer) and the findings grouped <b>MUST REVIEW</b>, <b>CHECK</b>,
/// <b>INFO</b> (design brief D7, §7.1).
/// </summary>
/// <remarks>
/// A port of the viewer's <c>RouteCheckCard.Build</c> and findings list
/// (EncDotNet.S100 <c>src/EncDotNet.S100.Viewer/ViewModels</c>), less what
/// the pane doesn't have: filter chips, merged rows and the departure window.
/// </remarks>
public static class RouteCheckPresenter
{
    // EncDotNet.S100 0.28 labels clearance on predicted (harmonic) water level;
    // 0.27, which the add-in pins, has no such reason. Found by name, so the
    // viewer's "Water level predicted on … NM: astronomical tide only, not a
    // forecast" applies once the pin moves, with no change here.
    private static readonly ConfidenceReasons? PredictedReason =
        Enum.TryParse<ConfidenceReasons>("WaterLevelPredicted", out var reason) ? reason : null;

    private static readonly PassageStretchKind? PredictedStretch =
        Enum.TryParse<PassageStretchKind>("WaterLevelPredicted", out var kind) ? kind : null;

    /// <summary>Gets the pane before any check.</summary>
    public static RouteCheckView Empty { get; } = new(
        Strings.RouteCheck_EmptyTitle, Strings.RouteCheck_EmptyDescription, null, null, RouteCheckTone.Neutral, [], null, [], null, []);

    /// <summary>Gets the pane while a route is being checked.</summary>
    /// <param name="routeName">The route's name.</param>
    /// <returns>The view.</returns>
    public static RouteCheckView Working(string routeName) => new(
        routeName, null, Strings.RouteCheck_Computing, null, RouteCheckTone.Neutral, [], null, [], null, []);

    /// <summary>Gets the pane when a route couldn't be checked.</summary>
    /// <param name="routeName">The route's name, or <c>null</c> when no route was read.</param>
    /// <param name="message">Why, ready to show.</param>
    /// <returns>The view.</returns>
    public static RouteCheckView Failed(string? routeName, string message) => new(
        routeName ?? Strings.RouteCheck_EmptyTitle,
        null,
        string.Format(CultureInfo.CurrentCulture, Strings.RouteCheck_ErrorFormat, message),
        null,
        RouteCheckTone.Neutral,
        [],
        null,
        [],
        null,
        []);

    /// <summary>Presents a finished check.</summary>
    /// <param name="outcome">The check.</param>
    /// <returns>The view.</returns>
    public static RouteCheckView Present(RouteCheckOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var report = outcome.Report;
        var hasDraft = outcome.Prepared.Parameters.HasDraft;
        var result = hasDraft ? report.Clearance : null;
        var findings = report.Findings;
        var (title, subtitle, tone) = Verdict(report.Verdict.Kind, hasDraft, result, findings);

        List<RouteCheckSummaryRow> rows =
        [
            ClearanceRow(result),
            RouteRow([.. findings.Where(IsRoute)]),
        ];
        var warningsChecked = outcome.Products.Contains("S-124") || findings.Any(f => f.Category == RouteFindingCategory.Warning);
        if (warningsChecked)
        {
            rows.Add(WarningsRow([.. findings.Where(f => f.Category == RouteFindingCategory.Warning)]));
        }

        var weather = findings.Where(f => f.Category == RouteFindingCategory.Weather).ToList();
        if (weather.Count > 0)
        {
            rows.Add(WeatherRow(weather));
        }

        var footer = outcome.Products.Count == 0
            ? Strings.RouteCheck_FooterNoData
            : string.Format(CultureInfo.CurrentCulture, Strings.RouteCheck_FooterFormat, string.Join(", ", outcome.Products));

        return new RouteCheckView(
            outcome.Route.Name,
            ParametersLine(outcome.Prepared.Parameters),
            title,
            subtitle,
            tone,
            rows,
            result is null ? null : ConfidenceLine(result),
            [.. findings.Select(Item)],
            footer,
            outcome.Problems);
    }

    /// <summary>Gets a finding as the pane lists it.</summary>
    /// <param name="finding">The finding.</param>
    /// <returns>The row.</returns>
    public static RouteFindingItem Item(RouteFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        return new RouteFindingItem(
            finding.Id,
            FindingFeatures.SeverityOf(finding.Group),
            FindingFeatures.RankOf(finding.Group),
            finding.Title,
            finding.Meta,
            FindingFeatures.TagOf(finding),
            ToneOf(finding),
            finding.Location?.Path ?? []);
    }

    /// <summary>
    /// Gets the viewer's verdict title for a clearance result
    /// (<c>PassageCheckViewModel.VerdictTitle</c>).
    /// </summary>
    /// <param name="result">The clearance check's result.</param>
    /// <returns>The title.</returns>
    public static string VerdictTitle(PassageCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Verdict switch
        {
            PassageVerdict.WaterLevelEndsBeforeArrival => Strings.RouteCheck_VerdictWaterLevelEnds,
            PassageVerdict.BelowSetting => result.PlacesBelow == 1
                ? Strings.RouteCheck_VerdictBelowOne
                : Format(Strings.RouteCheck_VerdictBelowFormat, result.PlacesBelow),
            PassageVerdict.IncompleteData => Strings.RouteCheck_VerdictIncomplete,
            PassageVerdict.MeetsSettingsTight => Strings.RouteCheck_VerdictTight,
            _ => Strings.RouteCheck_VerdictMeets,
        };
    }

    /// <summary>
    /// Gets the clearance check's lower-confidence notes, one per line, as the
    /// viewer's verdict shows them (<c>PassageCheckViewModel.ConfidenceLine</c>):
    /// charted depth from S-101, unconfirmed datums, and predicted water level.
    /// </summary>
    /// <param name="result">The clearance check's result.</param>
    /// <returns>The notes, or <c>null</c> at full confidence.</returns>
    public static string? ConfidenceLine(PassageCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        List<string> lines = [];
        if (result.Confidence.HasFlag(ConfidenceReasons.ChartedDepthFromS101))
        {
            lines.Add(Format(Strings.RouteCheck_ConfidenceS101Format, Nm(result, PassageStretchKind.ChartedDepthFromS101)));
        }

        if (result.Confidence.HasFlag(ConfidenceReasons.DatumUnconfirmed))
        {
            lines.Add(Strings.RouteCheck_ConfidenceDatumUnconfirmed);
        }

        if (PredictedReason is { } predicted && PredictedStretch is { } stretch && result.Confidence.HasFlag(predicted))
        {
            lines.Add(Format(Strings.RouteCheck_ConfidencePredictedFormat, Nm(result, stretch)));
        }

        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private static (string Title, string? Subtitle, RouteCheckTone Tone) Verdict(
        RouteCheckVerdictKind kind, bool hasDraft, PassageCheckResult? result, IReadOnlyList<RouteFinding> findings)
    {
        var review = findings.Where(f => f.Group == RouteFindingGroup.MustReview).ToList();
        var toCheck = findings.Where(f => f.Group == RouteFindingGroup.Check).ToList();

        // The report judged the kind; the card names it in the viewer's words.
        if (kind == RouteCheckVerdictKind.MustReview)
        {
            if (hasDraft)
            {
                var worst = review.OrderBy(f => f.Code.HeadlineRank).FirstOrDefault();
                string title;
                string? where;
                if (worst is null || worst.Code == RouteFindingCodes.BelowSetting)
                {
                    title = result is null ? Count(Strings.RouteCheck_ToReviewFormat, review.Count) : VerdictTitle(result);
                    where = result is null ? null : Where(result);
                }
                else
                {
                    title = worst.Title;
                    where = worst.Meta;
                }

                var subtitle = review.Count > 1
                    ? string.Format(CultureInfo.CurrentCulture, Strings.RouteCheck_AndMoreToReviewFormat, where, review.Count - 1)
                    : where;
                return (title, subtitle, RouteCheckTone.Destructive);
            }

            var named = review.OrderBy(f => f.Code.HeadlineRank).Select(f => f.Title).ToList();
            List<string> parts = [.. named.Take(2)];
            if (named.Count > 2)
            {
                parts.Add(Format(Strings.RouteCheck_MoreFormat, named.Count - 2));
            }

            if (toCheck.Count > 0)
            {
                parts.Add(Count(Strings.RouteCheck_ToCheckFormat, toCheck.Count));
            }

            return (Count(Strings.RouteCheck_ToReviewFormat, review.Count), string.Join(Strings.RouteCheck_Separator, parts), RouteCheckTone.Destructive);
        }

        if (kind == RouteCheckVerdictKind.WaterLevelEndsBeforeArrival && result is not null)
        {
            return (VerdictTitle(result), Where(result), RouteCheckTone.Neutral);
        }

        if (kind == RouteCheckVerdictKind.Check && toCheck.Count > 0)
        {
            var title = !hasDraft ? Count(Strings.RouteCheck_ToCheckFormat, toCheck.Count)
                : result is { Verdict: PassageVerdict.IncompleteData } ? VerdictTitle(result)
                : Format(Strings.RouteCheck_VerdictCheckFormat, toCheck.Count);
            return (title, toCheck[0].Title, RouteCheckTone.Warning);
        }

        if (result is not null)
        {
            return (VerdictTitle(result), Where(result), ToneOf(result.Verdict));
        }

        return (Strings.RouteCheck_NoDraftVerdictNone, Strings.RouteCheck_ClearNoDraft, RouteCheckTone.Success);
    }

    private static RouteCheckSummaryRow ClearanceRow(PassageCheckResult? result)
    {
        if (result is null)
        {
            return new(Strings.RouteCheck_SummaryClearance, Strings.RouteCheck_SummaryNotChecked, null, RouteCheckTone.Neutral);
        }

        if (result.Minimum is not { } minimum)
        {
            return new(Strings.RouteCheck_SummaryClearance, Strings.RouteCheck_NoClearance, null, RouteCheckTone.Neutral);
        }

        var minText = Metres(minimum.ClearanceMeters);
        var text = result.NoMarginSet
            ? Format(Strings.RouteCheck_SummaryClearanceNoMarginFormat, minText)
            : string.Format(CultureInfo.CurrentCulture, Strings.RouteCheck_SummaryClearanceFormat, minText, Metres(minimum.RequiredUkcMeters));
        var (chip, tone) = result.Verdict switch
        {
            PassageVerdict.BelowSetting => (Format(Strings.RouteCheck_ChipBelowFormat, result.PlacesBelow), RouteCheckTone.Destructive),
            PassageVerdict.MeetsSettingsTight => (Strings.RouteCheck_ChipTight, RouteCheckTone.Warning),
            _ when result.Sources.WaterLevelSources.Count == 0 => (Strings.RouteCheck_ChipNoTide, RouteCheckTone.Neutral),
            PassageVerdict.MeetsSettings => (Strings.RouteCheck_ChipMeets, RouteCheckTone.Success),
            _ => ((string?)null, RouteCheckTone.Neutral),
        };
        return new(Strings.RouteCheck_SummaryClearance, text, chip, tone);
    }

    private static RouteCheckSummaryRow RouteRow(IReadOnlyList<RouteFinding> route)
    {
        var review = route.Count(f => f.Group == RouteFindingGroup.MustReview);
        var check = route.Count(f => f.Group == RouteFindingGroup.Check);
        if (review + check == 0)
        {
            return new(Strings.RouteCheck_SummaryRoute, Strings.RouteCheck_SummaryNothingFound, "0", RouteCheckTone.Success);
        }

        List<string> parts = [];
        if (review > 0)
        {
            parts.Add(Count(Strings.RouteCheck_ToReviewFormat, review));
        }

        if (check > 0)
        {
            parts.Add(Count(Strings.RouteCheck_ToCheckFormat, check));
        }

        return new(
            Strings.RouteCheck_SummaryRoute,
            string.Join(Strings.RouteCheck_Separator, parts),
            (review + check).ToString(CultureInfo.CurrentCulture),
            review > 0 ? RouteCheckTone.Destructive : RouteCheckTone.Warning);
    }

    private static RouteCheckSummaryRow WarningsRow(IReadOnlyList<RouteFinding> warnings)
    {
        var onRoute = warnings.Count(f => f.Group == RouteFindingGroup.MustReview);
        var near = warnings.Count(f => f.Group == RouteFindingGroup.Check);
        if (onRoute + near == 0)
        {
            return new(Strings.RouteCheck_SummaryWarnings, Strings.RouteCheck_SummaryNothingOnRoute, null, RouteCheckTone.Neutral);
        }

        var text = onRoute == 0
            ? Count(Strings.RouteCheck_SummaryNearRouteFormat, near)
            : near == 0
                ? Count(Strings.RouteCheck_SummaryInForceFormat, onRoute)
                : Count(Strings.RouteCheck_SummaryInForceFormat, onRoute) + Strings.RouteCheck_Separator + Count(Strings.RouteCheck_SummaryNearFormat, near);
        return new(
            Strings.RouteCheck_SummaryWarnings,
            text,
            (onRoute + near).ToString(CultureInfo.CurrentCulture),
            onRoute > 0 ? RouteCheckTone.Destructive : RouteCheckTone.Warning);
    }

    // The viewer's weather row, without the limits and storm settings the
    // add-in doesn't set: what the check found, by kind.
    private static RouteCheckSummaryRow WeatherRow(IReadOnlyList<RouteFinding> weather)
    {
        var mustReview = weather.Count(f => f.Group == RouteFindingGroup.MustReview);
        var above = weather.Count(f => f.Code == RouteFindingCodes.WindAboveLimit || f.Code == RouteFindingCodes.WavesAboveLimit);
        var toCheck = weather.Count(f => f.Group == RouteFindingGroup.Check) - above;
        var notChecked = weather.Count - mustReview - above - Math.Max(0, toCheck);
        List<string> parts = [];
        if (mustReview > 0)
        {
            parts.Add(Count(Strings.RouteCheck_SummaryWeatherMustReviewFormat, mustReview));
        }

        if (above > 0)
        {
            parts.Add(Count(Strings.RouteCheck_SummaryWeatherAboveFormat, above));
        }

        if (toCheck > 0)
        {
            parts.Add(Count(Strings.RouteCheck_SummaryWeatherToCheckFormat, toCheck));
        }

        if (notChecked > 0)
        {
            parts.Add(Count(Strings.RouteCheck_SummaryWeatherNotCheckedFormat, notChecked));
        }

        return new(
            Strings.RouteCheck_SummaryWeather,
            string.Join(Strings.RouteCheck_Separator, parts),
            weather.Count.ToString(CultureInfo.CurrentCulture),
            mustReview > 0 ? RouteCheckTone.Destructive : above + toCheck > 0 ? RouteCheckTone.Warning : RouteCheckTone.Neutral);
    }

    /// <summary>Where the clearance minimum is: "Leg 2 → 3 · 14:20Z · 6 min below" (<c>RouteCheckCard.Where</c>).</summary>
    private static string? Where(PassageCheckResult result)
    {
        if (result.Minimum is not { } minimum)
        {
            return null;
        }

        List<string> parts = [string.Format(CultureInfo.CurrentCulture, Strings.RouteCheck_LegFormat, minimum.LegIndex + 1, minimum.LegIndex + 2)];
        if (minimum.HasEta)
        {
            parts.Add(Eta(minimum.Time, result.Departure));
        }

        var minutes = (int)Math.Round(result.MinutesBelow);
        if (minutes > 0)
        {
            parts.Add(Format(Strings.RouteCheck_MinutesBelowFormat, minutes));
        }

        return string.Join(Strings.RouteCheck_Separator, parts);
    }

    // The viewer's ETA in UTC (PassageFormatting.FormatEta): "14:20Z", with the
    // day when it isn't the departure's.
    private static string Eta(DateTimeOffset eta, DateTimeOffset departure)
    {
        var at = eta.ToUniversalTime();
        var time = at.ToString("HH:mm", CultureInfo.InvariantCulture) + "Z";
        return at.Date == departure.ToUniversalTime().Date ? time : at.ToString("ddd", CultureInfo.CurrentCulture) + " " + time;
    }

    private static string ParametersLine(RouteCheckParameters parameters)
    {
        var draft = parameters.Draft is { } d
            ? FromRoute(Format(Strings.RouteCheck_ParameterDraftFormat, Metres(d)), parameters.DraftFromRoute)
            : Strings.RouteCheck_ParameterNoDraft;
        var ukc = parameters.UkcMargin is { } u
            ? FromRoute(Format(Strings.RouteCheck_ParameterUkcFormat, Metres(u)), parameters.UkcMarginFromRoute)
            : Format(Strings.RouteCheck_ParameterUkcFormat, Strings.RouteCheck_NoMarginSet);
        return string.Join(
            Strings.RouteCheck_Separator,
            draft,
            ukc,
            FromRoute(Format(Strings.RouteCheck_ParameterSafetyContourFormat, Metres(parameters.SafetyContour)), parameters.SafetyContourFromRoute),
            FromRoute(Format(Strings.RouteCheck_ParameterSafetyDepthFormat, Metres(parameters.SafetyDepth)), parameters.SafetyDepthFromRoute));
    }

    private static string FromRoute(string text, bool fromRoute) =>
        fromRoute ? Format(Strings.RouteCheck_FromRouteFormat, text) : text;

    private static bool IsRoute(RouteFinding finding) =>
        finding.Category is RouteFindingCategory.Chart or RouteFindingCategory.Depth or RouteFindingCategory.Area
        || finding.Code == RouteFindingCodes.OutsideChartCoverage;

    private static RouteCheckTone ToneOf(PassageVerdict verdict) => verdict switch
    {
        PassageVerdict.MeetsSettings => RouteCheckTone.Success,
        PassageVerdict.MeetsSettingsTight => RouteCheckTone.Warning,
        PassageVerdict.BelowSetting => RouteCheckTone.Destructive,
        _ => RouteCheckTone.Neutral,
    };

    // The viewer's row tone (RouteFindingRowViewModel.ToneOf): confidence
    // notes are neutral whatever their group.
    private static RouteCheckTone ToneOf(RouteFinding finding) =>
        finding.Group == RouteFindingGroup.MustReview ? RouteCheckTone.Destructive
        : finding.Group == RouteFindingGroup.Check && finding.Category != RouteFindingCategory.Confidence ? RouteCheckTone.Warning
        : RouteCheckTone.Neutral;

    private static double Nm(PassageCheckResult result, PassageStretchKind kind) =>
        result.LowConfidence.Where(s => s.Kind == kind).Sum(s => s.LengthMeters) / PassageGeodesy.MetersPerNauticalMile;

    // The viewer's depths: one decimal, metres (PassageFormatting.Format).
    private static string Metres(double metres) => metres.ToString("0.0", CultureInfo.CurrentCulture) + " m";

    private static string Count(string format, int n) => string.Format(CultureInfo.CurrentCulture, format, n);

    private static string Format(string format, object? value) => string.Format(CultureInfo.CurrentCulture, format, value);
}
