using System.Globalization;
using EncDotNet.SoundCharts.Themes;

namespace EncDotNet.SoundCharts.Tests;

public class ScaleNoteStateTests
{
    [Fact]
    public void BelowTheMinimumScale_SaysZoomIn_InTheWarningTone()
    {
        using var culture = new CultureScope(CultureInfo.GetCultureInfo("en-US"));

        var state = ScaleNoteState.For(belowMinimumScale: true);

        Assert.Equal("Zoom in to see the chart", state.Text);
        Assert.Equal(SoundChartsTone.Warning, state.Tone);
        Assert.True(state.HasNote);
        Assert.Equal(state.Text, state.ToolTip);
    }

    [Fact]
    public void InScale_IsBlank_WithADimmedGlyph()
    {
        var state = ScaleNoteState.For(belowMinimumScale: false);

        Assert.Equal(ScaleNoteState.Idle, state);
        Assert.Equal(string.Empty, state.Text);
        Assert.Equal(SoundChartsTone.Neutral, state.Tone);
        Assert.False(state.HasNote);
        Assert.Null(state.ToolTip);
    }
}
