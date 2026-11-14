using EncDotNet.SoundCharts.Features;

namespace EncDotNet.SoundCharts.Tests.Features;

public sealed class DatasetTablesDumpTests
{
    [Theory]
    [InlineData("S101/S-101/DATASET_FILES/101AA00DS0003.000")]
    [InlineData("S124/navwarn_bulletin_navarea_x_104_26.gml")]
    [InlineData("S421/RTE-TEST-GFULL.s421.gml")]
    [InlineData("S57/US5MA1BO/US5MA1BO.000")]
    public void Dump(string relativePath)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, "no data");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var dataset = DatasetTables.Open(path);
        var lines = new List<string> { $"{dataset.Product} {dataset.Name} {sw.ElapsedMilliseconds} ms, {dataset.Tables.Count} tables" };
        foreach (var t in dataset.Tables)
        {
            lines.Add($"  {t.Name} ({t.DisplayName}) {t.Kind} rows={t.Rows.Count} extent={t.Extent}");
            lines.Add("    " + string.Join(", ", t.Fields.Select(f => $"{f.Name}:{f.Type}{(f.Length > 0 ? $"({f.Length})" : "")}[{f.Alias}]")));
            if (t.Rows.Count > 0)
            {
                lines.Add("    " + string.Join(" | ", t.Rows[0].Values.Select(v => v?.ToString() ?? "null")));
            }
        }

        File.WriteAllLines(Path.Combine(Path.GetTempPath(), $"a13-dump-{Path.GetFileName(path)}.txt"), lines);
    }
}
