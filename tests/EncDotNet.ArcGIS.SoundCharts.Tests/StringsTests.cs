using System.Reflection;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.Tests;

public class StringsTests
{
    public static TheoryData<string> Keys()
    {
        var data = new TheoryData<string>();
        foreach (var property in typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            data.Add(property.Name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryProperty_HasAResource(string key)
    {
        var value = (string?)typeof(Strings).GetProperty(key)?.GetValue(null);

        // Strings.Get falls back to the key when the .resx has no entry.
        Assert.False(string.IsNullOrWhiteSpace(value));
        Assert.NotEqual(key, value);
    }
}
