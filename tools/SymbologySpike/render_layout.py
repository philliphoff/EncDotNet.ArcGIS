"""A14 spike (#16): draws a SymbologySpike export with Pro's native symbology
and compares it with the chart server's raster portrayal.

Experimental; not part of the add-in. Run with ArcGIS Pro's Python::

    & "$env:ProgramFiles\\ArcGIS\\Pro\\bin\\Python\\envs\\arcgispro-py3\\python.exe" -I `
        tools/SymbologySpike/render_layout.py <export folder> <dataset> <s100.exe> [--category Standard]

It loads the export's GeoJSON into ``spike.gdb``, adds the ``.lyrx`` layers
to a Web Mercator map, exports a layout at 91 dpi (0.28 mm per pixel, the
pixel size ``s100 render`` assumes) and at 300 dpi, plus a PDF, renders the
same extent with ``s100 render``, and writes ``compare.json`` and diff
images. See docs/design/native-symbology-spike.md.
"""

import argparse
import json
import math
import subprocess
import time
from pathlib import Path

import arcpy
import numpy as np
from PIL import Image
from scipy import ndimage

LAYERS = ["AreaFills", "AreaSymbols", "Lines", "Points", "Text"]
GEOMETRY = {"AreaFills": "POLYGON", "AreaSymbols": "POLYGON", "Lines": "POLYLINE", "Points": "POINT", "Text": "POINT"}
EARTH_RADIUS = 6378137.0
FRAME_INCHES = (8.0, 6.0)
MARGIN_INCHES = 0.25
COMPARE_DPI = 91  # 25.4 / 0.28 = 90.7: s100 render's pixel size.
HIDE_LAYERS = {"areas": ["AreaFills"], "lines": ["Lines"], "points": ["AreaSymbols", "Points"], "text": ["Text"]}


def mercator(lon, lat):
    return (math.radians(lon) * EARTH_RADIUS,
            math.log(math.tan(math.pi / 4 + math.radians(lat) / 2)) * EARTH_RADIUS)


def lon_lat(x, y):
    return (math.degrees(x / EARTH_RADIUS),
            math.degrees(2 * math.atan(math.exp(y / EARTH_RADIUS)) - math.pi / 2))


def load(export, gdb):
    if arcpy.Exists(str(gdb)):
        arcpy.management.Delete(str(gdb))
    arcpy.management.CreateFileGDB(str(gdb.parent), gdb.name)
    for name in LAYERS:
        source = export / f"{name}.geojson"
        if json.loads(source.read_text(encoding="utf-8"))["features"]:
            arcpy.conversion.JSONToFeatures(str(source), str(gdb / name), GEOMETRY[name])


def frame_extent(gdb, scale):
    """A Mercator extent around the data, for the frame at the given scale."""
    xs, ys = [], []
    for name in LAYERS:
        if arcpy.Exists(str(gdb / name)):
            e = arcpy.Describe(str(gdb / name)).extent
            xs += [e.XMin, e.XMax]
            ys += [e.YMin, e.YMax]
    cx, cy = mercator((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2)
    # Ground metres per paper metre is the scale; Mercator stretches by 1/cos(lat).
    stretch = 1 / math.cos(math.radians((min(ys) + max(ys)) / 2))
    half_w = FRAME_INCHES[0] * 0.0254 * scale * stretch / 2
    half_h = FRAME_INCHES[1] * 0.0254 * scale * stretch / 2
    return cx - half_w, cy - half_h, cx + half_w, cy + half_h


def blank_project():
    install = Path(arcpy.GetInstallInfo()["InstallDir"])
    return install / "Resources" / "ArcToolBox" / "Services" / "routingservices" / "data" / "Blank.aprx"


def layout(export, gdb, extent, category_groups, hidden=()):
    project_path = export / "spike.aprx"
    arcpy.mp.ArcGISProject(str(blank_project())).saveACopy(str(project_path))
    project = arcpy.mp.ArcGISProject(str(project_path))
    project.defaultGeodatabase = str(gdb)
    web_mercator = arcpy.SpatialReference(3857)
    chart_map = project.createMap("Native", "MAP")
    for basemap in chart_map.listLayers():
        chart_map.removeLayer(basemap)
    for name in LAYERS:
        lyrx = export / f"{name}.lyrx"
        if lyrx.exists():
            # Point the layer file at this folder's geodatabase, wherever
            # the export was written.
            document = json.loads(lyrx.read_text(encoding="utf-8"))
            for definition in document["layerDefinitions"]:
                definition["featureTable"]["dataConnection"]["workspaceConnectionString"] = f"DATABASE={gdb}"
            lyrx.write_text(json.dumps(document, indent=2), encoding="utf-8")
            layer = chart_map.addLayer(arcpy.mp.LayerFile(str(lyrx)), "TOP")[0]
            layer.visible = name not in hidden
            if category_groups is not None:
                layer.definitionQuery = "ViewingGroup IN ({})".format(",".join(str(g) for g in category_groups))
    # The map takes the first layer's coordinate system (WGS 84); the chart
    # server draws Web Mercator.
    chart_map.spatialReference = web_mercator
    assert chart_map.spatialReference.factoryCode == 3857, chart_map.spatialReference.name
    page_w = FRAME_INCHES[0] + 2 * MARGIN_INCHES
    page_h = FRAME_INCHES[1] + 2 * MARGIN_INCHES
    lay = project.createLayout(page_w, page_h, "INCH")
    frame = lay.createMapFrame(
        arcpy.Extent(MARGIN_INCHES, MARGIN_INCHES, MARGIN_INCHES + FRAME_INCHES[0], MARGIN_INCHES + FRAME_INCHES[1]),
        chart_map)
    frame.camera.setExtent(arcpy.Extent(*extent, spatial_reference=web_mercator))
    project.save()
    return project, lay


def frame_pixels(png, dpi):
    image = np.asarray(Image.open(png).convert("RGB"))
    margin = round(MARGIN_INCHES * dpi)
    w, h = round(FRAME_INCHES[0] * dpi), round(FRAME_INCHES[1] * dpi)
    return image[margin:margin + h, margin:margin + w]


def compare(native, raster, out):
    """Pixel agreement between two same-size RGB images."""
    h = min(native.shape[0], raster.shape[0])
    w = min(native.shape[1], raster.shape[1])
    a = native[:h, :w].astype(int)
    b = raster[:h, :w].astype(int)
    diff = np.abs(a - b).max(axis=2)
    differs = diff > 48
    # Tolerate a pixel of placement and anti-aliasing: a pixel matches when
    # the other image has a close colour within one pixel of it.
    near = np.zeros_like(differs)
    near[:] = True
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            shifted = np.roll(np.roll(b, dy, axis=0), dx, axis=1)
            near &= np.abs(a - shifted).max(axis=2) > 48
    background = (b > 245).all(axis=2) & (a > 245).all(axis=2)
    ink = ~background
    result = {
        "pixels": int(h * w),
        "meanAbsDiff": round(float(np.abs(a - b).mean()), 2),
        "differPercent": round(100 * float(differs.mean()), 2),
        "differWithin1pxPercent": round(100 * float(near.mean()), 2),
        "inkPixels": int(ink.sum()),
        "differOfInkPercent": round(100 * float((near & ink).sum()) / max(1, int(ink.sum())), 2),
    }
    heat = np.full((h, w, 3), 255, dtype=np.uint8)
    heat[ink] = (210, 210, 210)
    heat[near] = (220, 30, 30)
    Image.fromarray(heat).save(out / "diff.png")
    Image.fromarray(np.concatenate([native[:h, :w], raster[:h, :w]], axis=1).astype(np.uint8)).save(out / "side-by-side.png")
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("export", type=Path)
    parser.add_argument("dataset", type=Path)
    parser.add_argument("s100", type=Path)
    parser.add_argument("--scale", type=float, default=0)
    parser.add_argument("--category", default=None, help="DisplayBase, Standard or OtherInformation")
    parser.add_argument("--hide", default="", help="instruction categories to leave out of both: areas,lines,points,text")
    args = parser.parse_args()

    export = args.export.resolve()
    summary = json.loads((export / "summary.json").read_text(encoding="utf-8"))
    gdb = export / "spike.gdb"
    timings = {}
    started = time.perf_counter()
    load(export, gdb)
    timings["loadGdbS"] = round(time.perf_counter() - started, 2)

    scale = args.scale
    if not scale:
        # Fit the data in the frame.
        xs, ys = [], []
        for name in LAYERS:
            if arcpy.Exists(str(gdb / name)):
                e = arcpy.Describe(str(gdb / name)).extent
                xs += [e.XMin, e.XMax]
                ys += [e.YMin, e.YMax]
        x0, y0 = mercator(min(xs), min(ys))
        x1, y1 = mercator(max(xs), max(ys))
        stretch = 1 / math.cos(math.radians((min(ys) + max(ys)) / 2))
        scale = 1.05 * max((x1 - x0) / (FRAME_INCHES[0] * 0.0254), (y1 - y0) / (FRAME_INCHES[1] * 0.0254)) / stretch
        scale = float(round(scale, -2))
    extent = frame_extent(gdb, scale)
    groups = summary["categoryViewingGroups"][args.category] if args.category else None

    hide = [h for h in args.hide.split(",") if h]
    hidden = [layer for h in hide for layer in HIDE_LAYERS[h]]
    project, lay = layout(export, gdb, extent, groups, hidden)
    results = {"scale": scale, "category": args.category or "All", "hidden": hide}
    for dpi in (COMPARE_DPI, 300):
        started = time.perf_counter()
        lay.exportToPNG(str(export / f"native-{dpi}.png"), resolution=dpi)
        timings[f"exportPng{dpi}S"] = round(time.perf_counter() - started, 2)
    started = time.perf_counter()
    lay.exportToPDF(str(export / "native.pdf"), resolution=300, image_compression="DEFLATE")
    timings["exportPdfS"] = round(time.perf_counter() - started, 2)
    results["pdfBytes"] = (export / "native.pdf").stat().st_size

    # The raster portrayal of the same extent and pixel size.
    lon0, lat0 = lon_lat(extent[0], extent[1])
    lon1, lat1 = lon_lat(extent[2], extent[3])
    width, height = round(FRAME_INCHES[0] * COMPARE_DPI), round(FRAME_INCHES[1] * COMPARE_DPI)
    command = [str(args.s100), "render", str(args.dataset), str(export / "raster.png"),
               "--bbox", f"{lon0},{lat0},{lon1},{lat1}", "--width", str(width), "--height", str(height),
               "--palette", summary["palette"].lower()]
    if hide:
        command += ["--hide", ",".join(hide)]
    started = time.perf_counter()
    subprocess.run(command, check=True, capture_output=True)
    timings["rasterRenderS"] = round(time.perf_counter() - started, 2)

    native = frame_pixels(export / f"native-{COMPARE_DPI}.png", COMPARE_DPI)
    raster = np.asarray(Image.open(export / "raster.png").convert("RGB"))
    results["compare"] = compare(native, raster, export)
    results["timings"] = timings
    results["extent3857"] = extent
    results["bbox"] = [lon0, lat0, lon1, lat1]
    (export / "compare.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
