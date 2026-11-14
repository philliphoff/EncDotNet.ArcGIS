"""A SoundCharts layout export keeps its symbols' size and content at any DPI (issue #12).

Exports one layout with arcpy at 96 and at 300 dpi, the SoundCharts chart
rendered the way **Export SoundCharts Chart...** renders it
(``TilesExportCommandLine`` and ``ChartTilePlan`` in the add-in), and checks
the two images:

- a chart symbol has the same size on paper: magenta symbols cover the same
  share of the frame;
- the SCAMIN content is the same: the 300 dpi image, averaged down to 96 dpi,
  shows the same soundings, labels and symbols in the same places as the
  96 dpi image.

A control renders the 300 dpi chart the way a DPI-unaware WMS request does,
at the zoom nearest the 300 dpi pixel size, and checks the same measures
tell it apart, so the test can fail.

Needs ArcGIS Pro's Python (arcpy), the bundled server
(``tools/build-server.ps1``) and EncDotNet.S100's test datasets
(``SOUNDCHARTS_S100_TEST_DATASETS``); it skips without them, so it doesn't
run in CI. Run it from the repository root::

    & "$env:ProgramFiles\\ArcGIS\\Pro\\bin\\Python\\envs\\arcgispro-py3\\python.exe" -I -m unittest discover -s tests/export -v

Set ``SOUNDCHARTS_KEEP_EXPORTS`` to a folder to keep the exported images.
"""

import math
import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

try:
    import arcpy
    import numpy as np
    from PIL import Image
    from scipy import ndimage
except ImportError:  # Not Pro's Python.
    arcpy = None

REPO = Path(__file__).resolve().parents[2]
EARTH_RADIUS = 6378137.0
TILE_SIZE = 256

# US5MA1BO (Nantucket Sound), a 6 x 6 inch frame at zoom 14 on screen
# (about 1:36,000).
DATASET = "S57/US5MA1BO/US5MA1BO.000"
CENTER = (-70.3125, 41.2875)
FRAME_INCHES = 6.0
SCREEN_ZOOM = 14
MARGIN_INCHES = 0.5


def metres_per_tile_pixel(zoom):
    """ChartTilePlan.MetresPerTilePixel."""
    return 2 * math.pi * EARTH_RADIUS / TILE_SIZE / 2 ** zoom


def zoom_for(metres_per_pixel):
    """ChartTilePlan.ZoomFor: the nearest zoom on a logarithmic scale."""
    zoom = math.log2(metres_per_tile_pixel(0) / metres_per_pixel)
    return max(0, min(24, math.floor(zoom + 0.5)))


def from_lon_lat(lon, lat):
    return (EARTH_RADIUS * math.radians(lon),
            EARTH_RADIUS * math.log(math.tan(math.pi / 4 + math.radians(lat) / 2)))


def to_lon_lat(x, y):
    return (math.degrees(x / EARTH_RADIUS),
            math.degrees(2 * math.atan(math.exp(y / EARTH_RADIUS)) - math.pi / 2))


def frame_extent():
    """The frame's Web Mercator extent: 6 inches at 96 dpi of zoom 14 pixels."""
    x, y = from_lon_lat(*CENTER)
    half = FRAME_INCHES * 96 * metres_per_tile_pixel(SCREEN_ZOOM) / 2
    return (x - half, y - half, x + half, y + half)


def find_s100():
    override = os.environ.get("SOUNDCHARTS_S100_PATH")
    if override and Path(override).is_file():
        return Path(override)
    arm64 = "ARM64" in (os.environ.get("PROCESSOR_ARCHITEW6432", "") + os.environ.get("PROCESSOR_ARCHITECTURE", "")).upper()
    for rid in (["win-arm64"] if arm64 else []) + ["win-x64"]:
        candidate = REPO / "artifacts" / "server" / rid / "s100.exe"
        if candidate.is_file():
            return candidate
    return None


def find_dataset():
    root = os.environ.get("SOUNDCHARTS_S100_TEST_DATASETS")
    path = Path(root) / DATASET if root else None
    return path if path and path.is_file() else None


def render(s100, dataset, out, zoom, pixel_ratio, work):
    """TilesExportCommandLine.Build: one zoom of the frame as a Web Mercator COG."""
    xmin, ymin, xmax, ymax = frame_extent()
    west, south = to_lon_lat(xmin, ymin)
    east, north = to_lon_lat(xmax, ymax)
    arguments = [
        str(s100), "tiles", "export", str(dataset),
        "-o", str(out),
        "--container", "geotiff",
        "--format", "png",
        "--min-zoom", str(zoom),
        "--max-zoom", str(zoom),
        "--bbox", f"{west!r},{south!r},{east!r},{north!r}",
        "--tile-size", str(TILE_SIZE * pixel_ratio),
        "--palette", "day",
    ]
    environment = dict(os.environ, S100_NO_UPDATE_CHECK="1", NO_COLOR="1",
                       DOTNET_hostBuilder__reloadConfigOnChange="false")
    subprocess.run(arguments, cwd=work, env=environment, check=True,
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=600)
    return out


def blank_project():
    install = Path(arcpy.GetInstallInfo()["InstallDir"])
    return install / "Resources" / "ArcToolBox" / "Services" / "routingservices" / "data" / "Blank.aprx"


def export_layout(raster, dpi, out, work):
    """A layout with one map frame showing the chart raster, exported with arcpy."""
    project_path = work / f"export-{out.stem}.aprx"
    arcpy.mp.ArcGISProject(str(blank_project())).saveACopy(str(project_path))
    project = arcpy.mp.ArcGISProject(str(project_path))
    web_mercator = arcpy.SpatialReference(3857)

    chart_map = project.createMap("Chart", "MAP")
    chart_map.spatialReference = web_mercator
    layer = chart_map.addDataFromPath(str(raster))

    # As ChartExporter.SwapIn: the chart's own colours (no stretch, no
    # gamma), its alpha band, bilinear.
    definition = layer.getDefinition("V3")
    colorizer = definition.colorizer
    colorizer.stretchType = "None"
    colorizer.useDefaultMapping = False
    colorizer.useGammaStretch = False
    colorizer.gammaR = colorizer.gammaG = colorizer.gammaB = 1.0
    colorizer.useAlphaBand = True
    colorizer.alphaBandIndex = 3
    colorizer.resamplingType = "BilinearInterpolation"
    layer.setDefinition(definition)

    page = FRAME_INCHES + 2 * MARGIN_INCHES
    layout = project.createLayout(page, page, "INCH")
    frame = layout.createMapFrame(
        arcpy.Extent(MARGIN_INCHES, MARGIN_INCHES, MARGIN_INCHES + FRAME_INCHES, MARGIN_INCHES + FRAME_INCHES),
        chart_map)
    xmin, ymin, xmax, ymax = frame_extent()
    frame.camera.setExtent(arcpy.Extent(xmin, ymin, xmax, ymax, spatial_reference=web_mercator))
    layout.exportToPNG(str(out), resolution=dpi)
    del project
    return out


def frame_pixels(png, dpi):
    """The map frame's pixels of a layout export, as RGB."""
    image = np.asarray(Image.open(png).convert("RGB"))
    margin = round(MARGIN_INCHES * dpi)
    size = round(FRAME_INCHES * dpi)
    return image[margin:margin + size, margin:margin + size]


def magenta_share(rgb):
    """The share of the frame magenta symbols cover (S-52 CHMGD and CHMGF)."""
    r, g, b = (rgb[..., i].astype(int) for i in range(3))
    return float(np.mean((r > 150) & (b > 150) & (g < 130) & (r - g > 60)))


def on_screen_grid(rgb, size):
    """A frame averaged down to the 96 dpi grid, in grey, softened by a pixel."""
    small = Image.fromarray(rgb).resize((size, size), Image.Resampling.BOX).convert("L")
    return ndimage.gaussian_filter(np.asarray(small, dtype=float), 1.0)


def content_difference(screen, printed):
    """The share of the 96 dpi grid where the two frames show something different.

    A sounding, label or symbol that one shows and the other doesn't (SCAMIN),
    or one drawn at another size or place, differs by more than anti-aliasing.
    """
    a = on_screen_grid(screen, screen.shape[0])
    b = on_screen_grid(printed, screen.shape[0])
    return float(np.mean(np.abs(a - b) > 24))


def similarity(screen, printed):
    """Correlation of the 96 dpi frame with the 300 dpi frame averaged down to it."""
    small = Image.fromarray(printed).resize(screen.shape[1::-1], Image.Resampling.BOX)
    a = np.asarray(small.convert("L"), dtype=float).ravel()
    b = np.asarray(Image.fromarray(screen).convert("L"), dtype=float).ravel()
    return float(np.corrcoef(a, b)[0, 1])


@unittest.skipIf(arcpy is None, "needs ArcGIS Pro's Python (arcpy)")
class ExportDpiTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.s100 = find_s100()
        cls.dataset = find_dataset()
        if cls.s100 is None:
            raise unittest.SkipTest("no bundled server; run tools/build-server.ps1")
        if cls.dataset is None:
            raise unittest.SkipTest("set SOUNDCHARTS_S100_TEST_DATASETS to EncDotNet.S100's tests/datasets")

        cls.work = Path(tempfile.mkdtemp(prefix="soundcharts-export-"))
        work = cls.work

        def export(name, dpi, zoom, ratio):
            raster = render(cls.s100, cls.dataset, work / f"{name}.tif", zoom, ratio, work)
            return frame_pixels(export_layout(raster, dpi, work / f"{name}.png", work), dpi)

        # As ChartTilePlan.For: the screen's zoom at every DPI, 512-pixel tiles above 96.
        xmin, _, xmax, _ = frame_extent()
        zoom = zoom_for((xmax - xmin) / (FRAME_INCHES * 96))
        assert zoom == SCREEN_ZOOM
        cls.screen = export("chart-96", 96, zoom, 1)
        cls.printed = export("chart-300", 300, zoom, 2)

        # The control: a DPI-unaware WMS picks the zoom nearest the 300 dpi pixels.
        cls.wms_zoom = zoom_for((xmax - xmin) / (FRAME_INCHES * 300))
        cls.wms = export("wms-300", 300, cls.wms_zoom, 1)

        keep = os.environ.get("SOUNDCHARTS_KEEP_EXPORTS")
        if keep:
            Path(keep).mkdir(parents=True, exist_ok=True)
            for png in work.glob("*.png"):
                shutil.copy2(png, keep)

    @classmethod
    def tearDownClass(cls):
        if getattr(cls, "work", None):
            shutil.rmtree(cls.work, ignore_errors=True)

    def test_the_frames_have_the_resolutions_asked_for(self):
        self.assertEqual(self.screen.shape[:2], (576, 576))
        self.assertEqual(self.printed.shape[:2], (1800, 1800))

    def test_a_symbol_keeps_its_size_on_paper(self):
        screen, printed = magenta_share(self.screen), magenta_share(self.printed)
        self.assertGreater(screen, 0.0005, "the frame should show magenta symbols")
        self.assertAlmostEqual(printed / screen, 1.0, delta=0.2,
                               msg=f"magenta covers {screen:.4%} at 96 dpi, {printed:.4%} at 300 dpi")

    def test_the_scamin_content_is_the_same(self):
        difference = content_difference(self.screen, self.printed)
        self.assertLess(difference, 0.003, f"{difference:.3%} of the frame differs")
        self.assertGreater(similarity(self.screen, self.printed), 0.85)

    def test_the_checks_catch_a_dpi_unaware_export(self):
        self.assertGreater(self.wms_zoom, SCREEN_ZOOM)
        size = magenta_share(self.wms) / magenta_share(self.screen)
        self.assertFalse(0.8 <= size <= 1.2, f"the control's symbols should print at another size: x{size:.2f}")
        self.assertGreater(content_difference(self.screen, self.wms), 3 * content_difference(self.screen, self.printed))
        self.assertLess(similarity(self.screen, self.wms), similarity(self.screen, self.printed))

if __name__ == "__main__":
    unittest.main()
