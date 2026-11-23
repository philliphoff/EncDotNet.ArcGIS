namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// The layers of a SoundCharts group, split by time behaviour (design brief
/// §3.1, D12). Each is one WMTS layer from its own chart server.
/// </summary>
public enum ChartLayerKind
{
    /// <summary>
    /// <b>Chart</b>: every dataset that doesn't change with map time (S-57,
    /// S-101, S-102, S-122, S-124 until U7, and datasets with a single time
    /// step).
    /// </summary>
    Chart,

    /// <summary><b>Water level</b>: S-104 datasets.</summary>
    WaterLevel,

    /// <summary><b>Surface currents</b>: S-111 datasets.</summary>
    SurfaceCurrents,

    /// <summary>
    /// <b>Weather</b>: every other dataset with more than one time step, such
    /// as NHC tropical cyclone advisories, CAP warnings, GRIB2 and NetCDF
    /// grids, surface analyses, S-411 sea ice and S-412.
    /// </summary>
    Weather,
}
