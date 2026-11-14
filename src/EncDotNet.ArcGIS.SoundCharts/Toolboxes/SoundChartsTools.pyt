# -*- coding: utf-8 -*-
"""SoundCharts Tools: geoprocessing tools for SoundCharts for ArcGIS.

Each tool runs the s100 program that ships with the add-in (design brief
D21) and reports its progress and errors as geoprocessing messages. The
logic lives in the arcpy-free soundcharts_tools package beside this file;
the tools here only map parameters to it. Docs: docs/toolbox.md.

Charts are not for navigation.
"""

import os
import re
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

import arcpy  # noqa: E402

from soundcharts_tools import commands, datasets, layerfile, locate, reports, runner  # noqa: E402

_PALETTES = ["Day", "Dusk", "Night"]
_TILE_FORMATS = {"PNG": "png", "JPEG": "jpeg", "WebP": "webp"}
_NOT_FOR_NAVIGATION = "Charts are not for navigation."


# ---------------------------------------------------------------------------
# Shared helpers
# ---------------------------------------------------------------------------

def _parameter(display_name, name, datatype, required=True, direction="Input", multi_value=False,
               category=None, value=None, values=None, extensions=None, value_range=None):
    parameter = arcpy.Parameter(
        displayName=display_name, name=name, datatype=datatype,
        parameterType="Required" if required is True else ("Derived" if required == "Derived" else "Optional"),
        direction=direction, multiValue=multi_value, category=category)
    if values is not None:
        parameter.filter.type = "ValueList"
        parameter.filter.list = values
    if extensions is not None:
        parameter.filter.list = extensions
    if value_range is not None:
        parameter.filter.type = "Range"
        parameter.filter.list = list(value_range)
    if value is not None:
        parameter.value = value
    return parameter


def _paths(parameter):
    """Gets a (multi-value) path parameter's paths."""
    text = parameter.valueAsText
    if not text:
        return []
    return [p.strip().strip("'\"") for p in text.split(";") if p.strip()]


def _text(parameter):
    return parameter.valueAsText or None


def _number(parameter):
    return parameter.value if parameter.value is not None else None


def _extent(parameter):
    """Gets an extent parameter as WGS-84 (west, south, east, north), or None."""
    extent = parameter.value
    if extent is None:
        return None
    reference = getattr(extent, "spatialReference", None)
    if reference is not None and reference.factoryCode not in (0, 4326):
        extent = extent.projectAs(arcpy.SpatialReference(4326))
    return (extent.XMin, extent.YMin, extent.XMax, extent.YMax)


def _fail(message):
    arcpy.AddError(message)
    raise arcpy.ExecuteError(message)


def _program():
    try:
        program = locate.locate(_HERE)
    except locate.S100NotFound as error:
        _fail(str(error))
    arcpy.AddMessage("Using s100 at {} ({}).".format(program.path, program.source))
    return program


def _working_folder():
    """A long-path folder for s100 to run in (A2 found an 8.3 working folder crashed it)."""
    root = os.environ.get("LOCALAPPDATA") or os.path.expanduser("~")
    folder = os.path.join(root, "SoundCharts", "ArcGIS", "toolbox")
    os.makedirs(folder, exist_ok=True)
    return folder


def _is_cancelled():
    return bool(getattr(arcpy.env, "isCancelled", False))


def _emit(level, text, findings=False):
    """Adds a message. Validation findings are warnings: the tool worked, the data has issues."""
    if level == runner.ERROR and not findings:
        arcpy.AddError(text)
    elif level in (runner.ERROR, runner.WARNING):
        arcpy.AddWarning(text)
    else:
        arcpy.AddMessage(text)


def _run(program, arguments, label, tile_progress=False, echo_stdout=True):
    """Runs s100, relaying its lines as messages; fails the tool when it's cancelled."""
    arcpy.AddMessage("Running: " + runner.command_line(program.path, arguments))
    tracker = runner.TileProgress() if tile_progress else None
    if tracker is not None:
        arcpy.SetProgressor("step", label, 0, 100, 1)
    else:
        arcpy.SetProgressor("default", label)

    def on_line(stream, line, level):
        if level is None or (stream == "stdout" and not echo_stdout):
            return
        _emit(level, line)
        if tracker is not None:
            percent = tracker.feed(line)
            if percent is not None:
                arcpy.SetProgressorPosition(percent)

    try:
        result = runner.run(program.path, arguments, on_line, _is_cancelled, cwd=_working_folder())
    finally:
        arcpy.ResetProgressor()
    if result.cancelled:
        _fail("Cancelled; s100 was stopped.")
    return result


def _check(result):
    if result.exit_code != 0:
        _fail(runner.exit_message(result.exit_code))


def _validated(parameters, build, target):
    """Shows a command builder's ValueError on the parameter it's about."""
    try:
        build()
    except ValueError as error:
        target.setErrorMessage(str(error))
    except Exception:  # A half-entered value; Pro checks it again later.
        pass


# ---------------------------------------------------------------------------
# Toolbox
# ---------------------------------------------------------------------------

class Toolbox(object):
    def __init__(self):
        self.label = "SoundCharts Tools"
        self.alias = "soundcharts"
        self.description = (
            "Add SoundCharts chart layers, export tiles and images, validate datasets and exchange sets, "
            "and convert between S-57 and S-101, with the s100 program from SoundCharts for ArcGIS. "
            + _NOT_FOR_NAVIGATION)
        self.tools = [AddSoundChartsLayer, ExportTiles, RenderToImage, ValidateDataset, VerifyExchangeSet, ConvertDataset]


# ---------------------------------------------------------------------------
# Add SoundCharts Layer
# ---------------------------------------------------------------------------

class AddSoundChartsLayer(object):
    def __init__(self):
        self.label = "Add SoundCharts Layer"
        self.description = (
            "Adds datasets and exchange sets to a map as a SoundCharts chart: a SoundCharts group with a Chart, "
            "Water level and Surface currents layer as the data needs, the layers Add SoundCharts Data makes. "
            "Writes the layer file and adds it to the map; the SoundCharts add-in connects the layers to a "
            "chart server. " + _NOT_FOR_NAVIGATION)
        self.canRunInBackground = False

    def getParameterInfo(self):
        return [
            _parameter("Input datasets, exchange sets or folders", "in_data", ["DEFile", "DEFolder"], multi_value=True),
            _parameter("Map", "in_map", "GPMap", required=False),
            _parameter("Output layer file", "out_layer_file", "DEFile", required=False, direction="Output",
                       extensions=["lyrx"]),
            _parameter("Palette", "palette", "GPString", required=False, values=_PALETTES, value="Day"),
            _parameter("Chart server WMTS capabilities URL", "server_url", "GPString", required=False,
                       category="Chart server"),
            _parameter("Output layer", "out_layer", "GPLayer", required="Derived", direction="Output"),
        ]

    def isLicensed(self):
        return True

    def updateParameters(self, parameters):
        return

    def updateMessages(self, parameters):
        url = parameters[4].valueAsText
        if url and not re.match(r"^https?://", url, re.IGNORECASE):
            parameters[4].setErrorMessage("Enter an http or https URL of a WMTS capabilities document.")

    def execute(self, parameters, messages):
        inputs, warnings = datasets.expand(_paths(parameters[0]))
        for warning in warnings:
            arcpy.AddWarning(warning)
        if not inputs:
            _fail("No datasets found in the inputs.")

        program = _program()
        version = program.version()
        total = sum(len(item.files) for item in inputs)
        arcpy.SetProgressor("step", "Reading {} dataset(s)...".format(total), 0, total, 1)
        try:
            found, warnings, names = datasets.inspect_all(
                program.path, inputs, on_done=arcpy.SetProgressorPosition, cwd=_working_folder())
        finally:
            arcpy.ResetProgressor()
        for warning in warnings:
            arcpy.AddWarning(warning)
        if not found:
            _fail("s100 couldn't read any of the datasets.")

        url = _text(parameters[4]) or layerfile.PLACEHOLDER_CAPABILITIES
        style = (_text(parameters[3]) or "Day").lower()
        name, document = layerfile.compose(names, found, url, style, version)

        out = _text(parameters[2])
        if not out:
            safe = re.sub(r'[<>:"/\\|?*]+', "_", name)
            out = os.path.join(arcpy.env.scratchFolder or _working_folder(), safe + ".lyrx")
        layerfile.write(out, document)
        arcpy.AddMessage("Wrote {}: {}, {} dataset(s).".format(out, name, len(found)))
        arcpy.SetParameterAsText(2, out)

        target = self._map(parameters[1])
        if target is None:
            arcpy.AddWarning("No map to add the layer to. Add {} to a map in ArcGIS Pro.".format(out))
            return
        added = target.addLayer(arcpy.mp.LayerFile(out), "TOP")
        if added:
            arcpy.SetParameter(5, added[0])
        arcpy.AddMessage("Added {} to {}.".format(name, target.name))
        if url == layerfile.PLACEHOLDER_CAPABILITIES:
            arcpy.AddMessage(
                "The SoundCharts add-in starts a chart server for the layers and connects them. "
                "Without the add-in they draw nothing; give a chart server URL instead.")

    @staticmethod
    def _map(parameter):
        try:
            project = arcpy.mp.ArcGISProject("CURRENT")
        except (OSError, RuntimeError):
            return None
        name = _text(parameter)
        if name:
            maps = project.listMaps(name)
            return maps[0] if maps else None
        return project.activeMap


# ---------------------------------------------------------------------------
# Export Tiles
# ---------------------------------------------------------------------------

class ExportTiles(object):
    def __init__(self):
        self.label = "Export Tiles"
        self.description = (
            "Renders datasets, an exchange set or a folder as Web Mercator raster tiles: an XYZ folder, a PMTiles "
            "or MBTiles archive, a GeoPackage, or a Cloud-Optimized GeoTIFF. Runs s100 tiles export. "
            + _NOT_FOR_NAVIGATION)
        self.canRunInBackground = True

    def getParameterInfo(self):
        return [
            _parameter("Input datasets, exchange set or folder", "in_data", ["DEFile", "DEFolder"], multi_value=True),
            _parameter("Output tiles", "out_tiles", ["DEFile", "DEFolder"], direction="Output"),
            _parameter("Container", "container", "GPString", required=False, values=list(commands.CONTAINERS)),
            _parameter("Minimum zoom", "min_zoom", "GPLong", required=False, value_range=(0, 24),
                       category="Zoom and extent"),
            _parameter("Maximum zoom", "max_zoom", "GPLong", required=False, value_range=(0, 24),
                       category="Zoom and extent"),
            _parameter("Extent", "extent", "GPExtent", required=False, category="Zoom and extent"),
            _parameter("Palette", "palette", "GPString", required=False, values=_PALETTES, value="Day",
                       category="Display"),
            _parameter("Time step", "time_step", "GPLong", required=False, value=0, category="Display"),
            _parameter("Hide text", "no_text", "GPBoolean", required=False, value=False, category="Display"),
            _parameter("Draw the offline land basemap", "offline_basemap", "GPBoolean", required=False, value=False,
                       category="Display"),
            _parameter("Tile size", "tile_size", "GPLong", required=False, values=[256, 512], value=256,
                       category="Tiles"),
            _parameter("Tile format", "tile_format", "GPString", required=False, values=list(_TILE_FORMATS),
                       value="PNG", category="Tiles"),
            _parameter("Quality (JPEG, WebP)", "quality", "GPLong", required=False, value_range=(1, 100),
                       category="Tiles"),
            _parameter("Skip empty tiles", "skip_empty", "GPBoolean", required=False, value=False, category="Tiles"),
            _parameter("Products (exchange sets and folders)", "only", "GPString", required=False, category="Tiles"),
            _parameter("Allow more than 100,000 tiles", "allow_large", "GPBoolean", required=False, value=False,
                       category="Tiles"),
        ]

    def isLicensed(self):
        return True

    def updateParameters(self, parameters):
        output, container = parameters[1], parameters[2]
        if output.altered and output.valueAsText and not container.altered:
            container.value = commands.container_label(commands.container_for_output(output.valueAsText))

    def updateMessages(self, parameters):
        if parameters[0].valueAsText and parameters[1].valueAsText:
            _validated(parameters, lambda: self._arguments(parameters), parameters[1])

    @staticmethod
    def _arguments(parameters):
        container = _text(parameters[2])
        return commands.tiles_export(
            _paths(parameters[0]), _text(parameters[1]),
            container=commands.CONTAINERS[container] if container else None,
            min_zoom=_number(parameters[3]), max_zoom=_number(parameters[4]), extent=_extent(parameters[5]),
            palette=_text(parameters[6]), time_step=_number(parameters[7]) or 0,
            no_text=bool(parameters[8].value), offline_basemap=bool(parameters[9].value),
            tile_size=_number(parameters[10]) or 256, image_format=_TILE_FORMATS[_text(parameters[11]) or "PNG"],
            quality=_number(parameters[12]), skip_empty=bool(parameters[13].value), only=_text(parameters[14]),
            allow_large=bool(parameters[15].value))

    def execute(self, parameters, messages):
        try:
            arguments = self._arguments(parameters)
        except ValueError as error:
            _fail(str(error))
        program = _program()
        result = _run(program, arguments, "Exporting tiles...", tile_progress=True)
        _check(result)


# ---------------------------------------------------------------------------
# Render to Image
# ---------------------------------------------------------------------------

class RenderToImage(object):
    def __init__(self):
        self.label = "Render to Image"
        self.description = (
            "Renders datasets, or every dataset in an exchange set, to a PNG, JPEG or WebP image with the "
            "S-100 portrayal. Runs s100 render. " + _NOT_FOR_NAVIGATION)
        self.canRunInBackground = True

    def getParameterInfo(self):
        return [
            _parameter("Input datasets or exchange set", "in_data", ["DEFile", "DEFolder"], multi_value=True),
            _parameter("Output image", "out_image", "DEFile", direction="Output",
                       extensions=["png", "jpg", "jpeg", "webp"]),
            _parameter("Width (pixels)", "width", "GPLong", required=False, value=1024, value_range=(1, 16384)),
            _parameter("Height (pixels)", "height", "GPLong", required=False, value=768, value_range=(1, 16384)),
            _parameter("Extent", "extent", "GPExtent", required=False),
            _parameter("Palette", "palette", "GPString", required=False, values=_PALETTES, value="Day",
                       category="Display"),
            _parameter("Time step", "time_step", "GPLong", required=False, value=0, category="Display"),
            _parameter("Hide text", "no_text", "GPBoolean", required=False, value=False, category="Display"),
            _parameter("Draw the offline land basemap", "offline_basemap", "GPBoolean", required=False, value=False,
                       category="Display"),
            _parameter("Background colour (#RRGGBB or #AARRGGBB)", "background", "GPString", required=False,
                       category="Display"),
            _parameter("Quality (JPEG, WebP)", "quality", "GPLong", required=False, value_range=(1, 100),
                       category="Display"),
        ]

    def isLicensed(self):
        return True

    def updateParameters(self, parameters):
        return

    def updateMessages(self, parameters):
        if parameters[0].valueAsText and parameters[1].valueAsText:
            _validated(parameters, lambda: self._arguments(parameters), parameters[1])

    @staticmethod
    def _arguments(parameters):
        return commands.render(
            _paths(parameters[0]), _text(parameters[1]), width=_number(parameters[2]) or 1024,
            height=_number(parameters[3]) or 768, extent=_extent(parameters[4]), palette=_text(parameters[5]),
            time_step=_number(parameters[6]) or 0, no_text=bool(parameters[7].value),
            offline_basemap=bool(parameters[8].value), background=_text(parameters[9]),
            quality=_number(parameters[10]))

    def execute(self, parameters, messages):
        try:
            arguments = self._arguments(parameters)
        except ValueError as error:
            _fail(str(error))
        program = _program()
        result = _run(program, arguments, "Rendering...")
        _check(result)


# ---------------------------------------------------------------------------
# Validate Dataset
# ---------------------------------------------------------------------------

class ValidateDataset(object):
    def __init__(self):
        self.label = "Validate Dataset"
        self.description = (
            "Checks datasets against their product specification's rule pack and lists the findings as "
            "warnings. Valid is true when every dataset passes. Runs s100 validate.")
        self.canRunInBackground = True

    def getParameterInfo(self):
        return [
            _parameter("Input datasets", "in_datasets", "DEFile", multi_value=True,
                       extensions=["000", "h5", "gml"]),
            _parameter("Fail on warnings", "strict", "GPBoolean", required=False, value=False),
            _parameter("Suppress rules", "suppress", "GPString", required=False),
            _parameter("Ignore S-101 update files", "no_updates", "GPBoolean", required=False, value=False),
            _parameter("Output report (JSON)", "out_report", "DEFile", required=False, direction="Output",
                       extensions=["json"]),
            _parameter("Valid", "out_valid", "GPBoolean", required="Derived", direction="Output"),
        ]

    def isLicensed(self):
        return True

    def updateParameters(self, parameters):
        return

    def updateMessages(self, parameters):
        for path in _paths(parameters[0]):
            if commands.is_exchange_set(path):
                parameters[0].setErrorMessage("{} is an exchange set; use Verify Exchange Set.".format(path))
                break

    def execute(self, parameters, messages):
        paths = _paths(parameters[0])
        try:
            arguments = [commands.validate(p, bool(parameters[1].value), _text(parameters[2]),
                                           bool(parameters[3].value)) for p in paths]
        except ValueError as error:
            _fail(str(error))
        program = _program()
        valid = True
        collected = []
        for path, args in zip(paths, arguments):
            name = os.path.basename(path)
            result = _run(program, args, "Validating {}...".format(name), echo_stdout=False)
            data = reports.parse(result.output)
            if data is None or result.exit_code not in (0, 6):
                arcpy.AddError("{}: {}".format(name, runner.exit_message(result.exit_code)))
                valid = False
                collected.append({"dataset": path, "error": runner.exit_message(result.exit_code)})
                continue
            report = reports.dataset_report(data, name)
            for level, text in report.messages:
                _emit(level, text, findings=True)
            valid = valid and report.valid
            collected.append({"dataset": path, "report": data})
        if _text(parameters[4]):
            reports.write(_text(parameters[4]), {"valid": valid, "datasets": collected})
            arcpy.AddMessage("Wrote {}.".format(_text(parameters[4])))
        arcpy.SetParameter(5, valid)


# ---------------------------------------------------------------------------
# Verify Exchange Set
# ---------------------------------------------------------------------------

class VerifyExchangeSet(object):
    def __init__(self):
        self.label = "Verify Exchange Set"
        self.description = (
            "Verifies an exchange set's signatures and checksums (S-100 Part 15, or S-57 / S-63 CATALOG.031 "
            "CRCs). Valid is true when every file passes. Runs s100 validate on the exchange set.")
        self.canRunInBackground = True

    def getParameterInfo(self):
        return [
            _parameter("Input exchange set", "in_exchange_set", ["DEFolder", "DEFile"]),
            _parameter("Fail unsigned files and files without a checksum", "strict", "GPBoolean", required=False,
                       value=False),
            _parameter("Output report (JSON)", "out_report", "DEFile", required=False, direction="Output",
                       extensions=["json"]),
            _parameter("Valid", "out_valid", "GPBoolean", required="Derived", direction="Output"),
        ]

    def isLicensed(self):
        return True

    def updateParameters(self, parameters):
        return

    def updateMessages(self, parameters):
        path = parameters[0].valueAsText
        if path:
            _validated(parameters, lambda: commands.verify_exchange_set(path), parameters[0])

    def execute(self, parameters, messages):
        path = _text(parameters[0])
        try:
            arguments = commands.verify_exchange_set(path, bool(parameters[1].value))
        except ValueError as error:
            _fail(str(error))
        program = _program()
        name = os.path.basename(path.rstrip("\\/"))
        result = _run(program, arguments, "Verifying {}...".format(name), echo_stdout=False)
        data = reports.parse(result.output)
        if data is None or result.exit_code not in (0, 6):
            _fail(runner.exit_message(result.exit_code))
        report = reports.exchange_set_report(data, name)
        for level, text in report.messages:
            _emit(level, text, findings=True)
        if _text(parameters[2]):
            reports.write(_text(parameters[2]), data)
            arcpy.AddMessage("Wrote {}.".format(_text(parameters[2])))
        arcpy.SetParameter(3, report.valid)


# ---------------------------------------------------------------------------
# Convert Dataset
# ---------------------------------------------------------------------------

class ConvertDataset(object):
    def __init__(self):
        self.label = "Convert Dataset"
        self.description = (
            "Converts an S-57 cell to S-101 (or an inland ENC cell to S-401), or an S-101 dataset to an S-57 "
            "cell or exchange set. Runs s100 s57 convert or s100 s101 convert. An S-57 cell from S-101 is for "
            "testing and for tools that read only S-57; it isn't a production ENC. " + _NOT_FOR_NAVIGATION)
        self.canRunInBackground = True

    def getParameterInfo(self):
        return [
            _parameter("Input cell", "in_cell", "DEFile", extensions=["000"]),
            _parameter("Conversion", "conversion", "GPString", values=list(commands.CONVERSIONS),
                       value="S-57 to S-101"),
            _parameter("Output cell or folder", "out_data", ["DEFile", "DEFolder"], direction="Output"),
            _parameter("Write an S-57 exchange set", "exchange_set", "GPBoolean", required=False, value=False),
            _parameter("Cell name", "cell_name", "GPString", required=False),
            _parameter("Output translation report (JSON)", "out_report", "DEFile", required=False,
                       direction="Output", extensions=["json"]),
            _parameter("Ignore update files", "no_updates", "GPBoolean", required=False, value=False),
        ]

    def isLicensed(self):
        return True

    def updateParameters(self, parameters):
        to_s57 = parameters[1].valueAsText == "S-101 to S-57"
        parameters[3].enabled = to_s57
        parameters[4].enabled = to_s57 and bool(parameters[3].value)

    def updateMessages(self, parameters):
        if parameters[0].valueAsText and parameters[2].valueAsText:
            _validated(parameters, lambda: self._arguments(parameters), parameters[4])

    @staticmethod
    def _arguments(parameters):
        conversion = _text(parameters[1])
        to_s57 = conversion == "S-101 to S-57"
        exchange_set = to_s57 and bool(parameters[3].value)
        return commands.convert(
            _text(parameters[0]), _text(parameters[2]), conversion, exchange_set=exchange_set,
            cell_name=_text(parameters[4]) if exchange_set else None, report=_text(parameters[5]),
            no_updates=bool(parameters[6].value))

    def execute(self, parameters, messages):
        try:
            arguments = self._arguments(parameters)
        except ValueError as error:
            _fail(str(error))
        program = _program()
        result = _run(program, arguments, "Converting...")
        _check(result)
