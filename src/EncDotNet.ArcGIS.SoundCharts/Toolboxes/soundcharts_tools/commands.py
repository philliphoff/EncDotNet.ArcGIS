"""The ``s100`` arguments for each SoundCharts tool.

Each function checks its options the way the tool's validation would and
raises ``ValueError`` with a message for the user when they can't work, so
the tool fails before it starts ``s100``. Options are those of the pinned
build (``tools/s100-server.json``); see EncDotNet.S100's ``docs/cli.md``.
"""

import os

from . import datasets

PALETTES = ("day", "dusk", "night")

# Display label → --container, for the Export Tiles tool.
CONTAINERS = {
    "XYZ folder": "xyz",
    "PMTiles": "pmtiles",
    "MBTiles": "mbtiles",
    "GeoPackage": "gpkg",
    "Cloud-Optimized GeoTIFF": "geotiff",
}

_CONTAINER_EXTENSIONS = {
    ".pmtiles": "pmtiles",
    ".mbtiles": "mbtiles",
    ".gpkg": "gpkg",
    ".tif": "geotiff",
    ".tiff": "geotiff",
}

TILE_FORMATS = ("png", "jpeg", "webp")
IMAGE_EXTENSIONS = {".png": "png", ".jpg": "jpeg", ".jpeg": "jpeg", ".webp": "webp"}

# Display label → (command, target), for the Convert tool.
CONVERSIONS = {
    "S-57 to S-101": ("s57", "s101"),
    "S-57 inland ENC to S-401": ("s57", "s401"),
    "S-101 to S-57": ("s101", "s57"),
}


def container_for_output(path):
    """Gets the container an output path implies: an archive by its extension, else an XYZ folder."""
    return _CONTAINER_EXTENSIONS.get(os.path.splitext(path)[1].lower(), "xyz")


def container_label(container):
    """Gets the display label of a container."""
    return next(label for label, value in CONTAINERS.items() if value == container)


def _number(value):
    text = repr(float(value)) if not isinstance(value, int) else str(value)
    return text[:-2] if text.endswith(".0") else text


def bbox(extent):
    """Formats a WGS-84 ``(west, south, east, north)`` extent as s100's ``--bbox``."""
    west, south, east, north = extent
    if not (-180 <= west < east <= 180 and -90 <= south < north <= 90):
        raise ValueError("The extent must be a WGS-84 box with west < east and south < north.")
    return ",".join(_number(v) for v in (west, south, east, north))


def inputs(paths):
    """Gets the input arguments: one positional input, or ``--layer`` per dataset file.

    s100 takes one exchange set or folder, or any number of dataset files,
    but not a mix (upstream U2).
    """
    paths = [os.path.abspath(p) for p in paths if p]
    if not paths:
        raise ValueError("Choose at least one dataset, exchange set or folder.")
    if len(paths) == 1:
        return [paths[0]]
    collections = [p for p in paths if not (os.path.isfile(p) and datasets.is_dataset_file(p))]
    if collections:
        raise ValueError(
            "s100 can't combine an exchange set or folder with other inputs ({}). "
            "Choose one exchange set or folder, or only dataset files.".format(collections[0]))
    arguments = []
    for path in paths:
        arguments += ["--layer", path]
    return arguments


def _palette(palette):
    palette = (palette or "day").lower()
    if palette not in PALETTES:
        raise ValueError("The palette must be Day, Dusk or Night.")
    return ["--palette", palette]


def _background(color):
    if not color:
        return []
    text = color.strip()
    digits = text[1:] if text.startswith("#") else text
    if len(digits) not in (6, 8) or any(c not in "0123456789abcdefABCDEF" for c in digits):
        raise ValueError("The background colour must be #RRGGBB or #AARRGGBB.")
    return ["--background", "#" + digits.upper()]


def _quality(quality, image_format):
    if quality is None:
        return []
    if not 1 <= int(quality) <= 100:
        raise ValueError("The quality must be from 1 to 100.")
    return ["--quality", str(int(quality))] if image_format in ("jpeg", "webp") else []


def _display(time_step, no_text, offline_basemap):
    arguments = []
    if time_step:
        if int(time_step) < 0:
            raise ValueError("The time step can't be negative.")
        arguments += ["--time-step", str(int(time_step))]
    if no_text:
        arguments.append("--no-text")
    if offline_basemap:
        arguments += ["--basemap", "offline"]
    return arguments


def _only(only):
    products = [p.strip() for p in (only or "").replace(";", ",").split(",") if p.strip()]
    return ["--only", ",".join(products)] if products else []


def tiles_export(paths, output, container=None, min_zoom=None, max_zoom=None, extent=None, tile_size=256,
                 image_format="png", quality=None, palette="day", time_step=0, background=None, no_text=False,
                 offline_basemap=False, skip_empty=False, only=None, allow_large=False):
    """Gets the ``tiles export`` arguments."""
    if not output:
        raise ValueError("Choose where to write the tiles.")
    output = os.path.abspath(output)
    container = container or container_for_output(output)
    if container not in CONTAINERS.values():
        raise ValueError("Unknown tile container {!r}.".format(container))
    implied = container_for_output(output)
    if container != "xyz" and implied != container:
        raise ValueError("A {} output needs a file ending in {}.".format(
            container_label(container),
            " or ".join(e for e, c in _CONTAINER_EXTENSIONS.items() if c == container)))
    if container == "xyz" and implied != "xyz":
        raise ValueError("An XYZ folder output can't end in {}.".format(os.path.splitext(output)[1]))
    parent = os.path.dirname(output)
    if parent and not os.path.isdir(parent):
        raise ValueError("The output's folder {} doesn't exist.".format(parent))

    arguments = ["tiles", "export"] + inputs(paths) + ["-o", output, "--container", container]
    for name, zoom in (("--min-zoom", min_zoom), ("--max-zoom", max_zoom)):
        if zoom is not None:
            if not 0 <= int(zoom) <= 24:
                raise ValueError("Zoom levels go from 0 to 24.")
            arguments += [name, str(int(zoom))]
    if min_zoom is not None and max_zoom is not None and int(min_zoom) > int(max_zoom):
        raise ValueError("The minimum zoom is above the maximum zoom.")
    if extent is not None:
        arguments += ["--bbox", bbox(extent)]
    if int(tile_size) not in (256, 512):
        raise ValueError("The tile size must be 256 or 512.")
    if int(tile_size) != 256:
        arguments += ["--tile-size", str(int(tile_size))]
    image_format = (image_format or "png").lower()
    if image_format not in TILE_FORMATS:
        raise ValueError("The tile format must be PNG, JPEG or WebP.")
    if image_format != "png":
        arguments += ["--format", image_format]
    arguments += _quality(quality, image_format)
    arguments += _palette(palette)
    arguments += _display(time_step, no_text, offline_basemap)
    arguments += _background(background)
    if skip_empty:
        arguments.append("--skip-empty")
    arguments += _only(only)
    if allow_large:
        arguments.append("--yes")
    return arguments


def render(paths, output, width=1024, height=768, extent=None, palette="day", time_step=0, background=None,
           no_text=False, offline_basemap=False, quality=None, only=None):
    """Gets the ``render`` arguments. The image format comes from the output's extension."""
    if not output:
        raise ValueError("Choose where to write the image.")
    output = os.path.abspath(output)
    image_format = IMAGE_EXTENSIONS.get(os.path.splitext(output)[1].lower())
    if image_format is None:
        raise ValueError("The image must end in .png, .jpg, .jpeg or .webp.")
    for value in (width, height):
        if not 1 <= int(value) <= 16384:
            raise ValueError("The width and height must be from 1 to 16384 pixels.")
    arguments = ["render"] + inputs(paths) + ["-o", output, "--width", str(int(width)), "--height", str(int(height))]
    if extent is not None:
        arguments += ["--bbox", bbox(extent)]
    arguments += _quality(quality, image_format)
    arguments += _palette(palette)
    arguments += _display(time_step, no_text, offline_basemap)
    arguments += _background(background)
    arguments += _only(only)
    return arguments


def validate(path, strict=False, suppress=None, no_updates=False):
    """Gets the ``validate`` arguments for a dataset, with a JSON report."""
    if not path:
        raise ValueError("Choose a dataset.")
    if is_exchange_set(path):
        raise ValueError("{} is an exchange set; use Verify Exchange Set.".format(path))
    arguments = ["validate", os.path.abspath(path), "--format", "json"]
    if strict:
        arguments.append("--strict")
    rules = [r.strip() for r in (suppress or "").replace(";", ",").split(",") if r.strip()]
    if rules:
        arguments += ["--suppress", ",".join(rules)]
    if no_updates:
        arguments.append("--no-updates")
    return arguments


def is_exchange_set(path):
    """Whether a path is an exchange set: a folder, a catalogue file, or a .zip."""
    return os.path.isdir(path) or datasets.is_exchange_set_catalogue(path) or path.lower().endswith(".zip")


def verify_exchange_set(path, strict=False):
    """Gets the ``validate`` arguments for an exchange set's signatures and checksums, with a JSON report."""
    if not path:
        raise ValueError("Choose an exchange set.")
    if not is_exchange_set(path):
        raise ValueError("{} isn't an exchange set: choose its folder, CATALOG.XML, CATALOG.031 or .zip.".format(path))
    arguments = ["validate", os.path.abspath(path), "--format", "json"]
    if strict:
        arguments.append("--strict")
    return arguments


def convert(source, output, conversion, exchange_set=False, cell_name=None, report=None, no_updates=False):
    """Gets the ``s57 convert`` or ``s101 convert`` arguments.

    :param conversion: A key of ``CONVERSIONS``.
    :param exchange_set: S-101 to S-57 only: write an S-57 exchange set folder.
    :param cell_name: With ``exchange_set``, the cell's name, such as ``XX5CELL1``.
    """
    if conversion not in CONVERSIONS:
        raise ValueError("Unknown conversion {!r}.".format(conversion))
    if not source or not output:
        raise ValueError("Choose the source cell and the output.")
    command, target = CONVERSIONS[conversion]
    arguments = [command, "convert", os.path.abspath(source), "-o", os.path.abspath(output)]
    if command == "s57":
        arguments += ["--target", target]
        if exchange_set or cell_name:
            raise ValueError("An exchange set and a cell name only apply to S-101 to S-57.")
    else:
        arguments += ["--to", "s57"]
        if exchange_set:
            arguments.append("--exchange-set")
            if cell_name:
                if len(cell_name) != 8 or not cell_name.isalnum():
                    raise ValueError("An S-57 cell name is eight letters and digits, such as XX5CELL1.")
                arguments += ["--cell-name", cell_name.upper()]
        elif cell_name:
            raise ValueError("A cell name only applies when writing an exchange set.")
    if report:
        arguments += ["--report", os.path.abspath(report)]
    if no_updates:
        arguments.append("--no-updates")
    return arguments
