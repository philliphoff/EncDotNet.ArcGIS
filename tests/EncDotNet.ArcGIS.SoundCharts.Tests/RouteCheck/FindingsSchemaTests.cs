using System.Text.RegularExpressions;
using EncDotNet.SoundCharts.RouteCheck;

namespace EncDotNet.SoundCharts.Tests.RouteCheck;

public sealed partial class FindingsSchemaTests
{
    [Fact]
    public void FeatureClassName_IsAValidGeodatabaseNamePerRouteAndShape()
    {
        var points = FindingsSchema.FeatureClassName("Nab to Southampton", FindingShape.Point);
        var segments = FindingsSchema.FeatureClassName("Nab to Southampton", FindingShape.Segment);

        Assert.Matches(TableName(), points);
        Assert.EndsWith("_Points", points);
        Assert.EndsWith("_Segments", segments);
        Assert.Equal(points[..^"_Points".Length], segments[..^"_Segments".Length]);
    }

    [Fact]
    public void FeatureClassName_SameRouteSameName_OtherRouteOtherName()
    {
        Assert.Equal(
            FindingsSchema.FeatureClassName("Nab to Southampton", FindingShape.Point),
            FindingsSchema.FeatureClassName(" nab to southampton ", FindingShape.Point));
        Assert.NotEqual(
            FindingsSchema.FeatureClassName("Nab to Southampton", FindingShape.Point),
            FindingsSchema.FeatureClassName("Southampton to Nab", FindingShape.Point));
    }

    [Fact]
    public void GroupName_IsRouteCheckDotRoute()
    {
        Assert.Equal("Route check · Nab to Southampton", FindingsSchema.GroupName("Nab to Southampton"));
    }

    [Theory]
    [InlineData(null, 5, null)]
    [InlineData("short", 5, "short")]
    [InlineData("longer text", 5, "long…")]
    public void Fit_CutsToTheFieldLength(string? value, int length, string? expected)
    {
        Assert.Equal(expected, FindingsSchema.Fit(value, length));
    }

    [Fact]
    public void TextFields_AreUniqueAndCarryEveryAttribute()
    {
        var names = FindingsSchema.TextFields.Select(f => f.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(FindingsSchema.SeverityField, names);
        Assert.Contains(FindingsSchema.RuleField, names);
        Assert.Contains(FindingsSchema.FeaturesField, names);
        Assert.Contains(FindingsSchema.MessageField, names);
        Assert.All(FindingsSchema.TextFields, f => Assert.InRange(f.Length, 16, 2048));
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,63}$")]
    private static partial Regex TableName();
}
