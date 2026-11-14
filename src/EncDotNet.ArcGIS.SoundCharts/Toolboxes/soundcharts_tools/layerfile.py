"""Writes SoundCharts layers as an ArcGIS Pro layer document (``.lyrx``).

The document is the one the add-in writes (``ChartLayerDocument`` in
``src/EncDotNet.ArcGIS.SoundCharts/ChartLayers``): a ``CIMGroupLayer`` named
**SoundCharts · {source}** holding a ``CIMTiledServiceLayer`` per layer kind,
each with a ``CIMWMTSServiceConnection`` and the SoundCharts marker, a CIM
custom property that says what the layer draws (design brief §3, D16).

The add-in's snapshots in ``tests/EncDotNet.ArcGIS.SoundCharts.Tests/
ChartLayers/Snapshots`` are the contract: ``tests/toolbox/test_layerfile.py``
builds the same layers here and checks the text matches them byte for byte,
so a change on either side fails a test until both agree.

arcpy can't add a WMTS layer from a URL (``addDataFromPath`` has no WMTS
type), so the toolbox writes this document and adds it. When the add-in is
loaded, it finds the marker on the new layers, starts a chart server for
their datasets and points the layers at it, as it does for a project it opens.
"""

import json
import math
import os

CIM_VERSION = "3.7.0"
"""The CIM version the documents declare: the Pro release the add-in targets."""

MARKER_KEY = "SoundCharts"
"""The CIM custom property key that holds the marker."""

MARKER_VERSION = 1
"""The marker format's version."""

WMTS_LAYER = "s100"
WMTS_TILE_MATRIX_SET = "WebMercatorQuad"
DEFAULT_STYLE = "day"
STYLES = ("day", "dusk", "night")

PLACEHOLDER_CAPABILITIES = "http://127.0.0.1:1/soundcharts/wmts/1.0.0/WMTSCapabilities.xml"
"""
Where a layer points before the add-in connects it. Nothing listens on port 1,
so Pro fails fast instead of waiting; the add-in replaces the URL with its
chart server's when it sees the layer.
"""

_URI_PREFIX = "CIMPATH=soundcharts/"
GROUP_URI = _URI_PREFIX + "soundcharts.json"

CHART = "chart"
WATER_LEVEL = "water-level"
SURFACE_CURRENTS = "surface-currents"

TOP_FIRST = (SURFACE_CURRENTS, WATER_LEVEL, CHART)
"""Layer kinds in drawing order, top first: time layers overlay the chart (§3.1)."""

_NAMES = {
    CHART: "Chart",
    WATER_LEVEL: "Water level",
    SURFACE_CURRENTS: "Surface currents",
}

# The add-in's Strings.resx values; test_layerfile.py checks they still match.
GROUP_NAME = "SoundCharts · {0}"
GROUP_DATASET_COUNT = "{0} datasets"
CREDITS_DESCRIPTION = "SoundCharts portrayal of {0} from {1}. Not for navigation."
CREDITS_ATTRIBUTION = "Data: {0}. Portrayal: SoundCharts ({1}). Not for navigation."
CREDITS_UNKNOWN_PRODUCER = "unknown producer"
UNKNOWN_VERSION = "unknown"


class ChartDataset:
    """One dataset of a SoundCharts layer, as the marker keeps it.

    Mirrors the add-in's ``ChartLayerDataset``.
    """

    def __init__(self, path, product, edition=None, extent=None, producer=None, not_for_navigation=False):
        """Creates a dataset.

        :param path: The dataset file's full path.
        :param product: The product specification, such as ``S-101`` or ``S-57``.
        :param edition: The edition the dataset declares, or ``None``.
        :param extent: The WGS-84 footprint ``(west, south, east, north)``, or ``None``.
        :param producer: The producer's name or code, or ``None``.
        :param not_for_navigation: Whether the producer marks it not for navigation.
        """
        if not path or not product:
            raise ValueError("A dataset needs a path and a product.")
        self.path = path
        self.product = product
        self.edition = edition
        self.extent = tuple(extent) if extent is not None else None
        self.producer = producer
        self.not_for_navigation = bool(not_for_navigation)

    @property
    def name(self):
        """The file name without its extension, such as ``US5MA1BO``."""
        return os.path.splitext(os.path.basename(self.path.replace("\\", "/")))[0]

    @property
    def kind(self):
        """The layer the dataset belongs to."""
        return kind_for_product(self.product)

    def __repr__(self):
        return "ChartDataset({!r}, {!r})".format(self.path, self.product)


def kind_for_product(product):
    """Gets the layer kind for a product: S-104 water level, S-111 surface currents, else chart."""
    if product == "S-104":
        return WATER_LEVEL
    if product == "S-111":
        return SURFACE_CURRENTS
    return CHART


def display_name(kind):
    """Gets a layer kind's name in Contents."""
    return _NAMES[kind]


def _ignore_case(text):
    # .NET's OrdinalIgnoreCase compares upper-cased characters.
    return text.upper()


def layer_datasets(datasets):
    """Gets a layer marker's datasets: one per path (ignoring case), ordered by path."""
    seen = set()
    result = []
    for dataset in datasets:
        key = _ignore_case(dataset.path)
        if key not in seen:
            seen.add(key)
            result.append(dataset)
    result.sort(key=lambda d: _ignore_case(d.path))
    return result


def products(datasets):
    """Gets the distinct products, in ordinal order, such as ``S-101, S-57``."""
    return ", ".join(sorted({d.product for d in datasets}))


def producers(datasets):
    """Gets the producers, by dataset count then name, or "unknown producer"."""
    groups = {}
    order = []
    for dataset in datasets:
        if dataset.producer is None or not dataset.producer.strip():
            continue
        name = dataset.producer.strip()
        key = _ignore_case(name)
        if key not in groups:
            groups[key] = [name, 0]
            order.append(key)
        groups[key][1] += 1
    if not groups:
        return CREDITS_UNKNOWN_PRODUCER
    ranked = sorted(order, key=lambda k: (-groups[k][1], k))
    return ", ".join(groups[k][0] for k in ranked)


def description(datasets):
    """Gets a layer's description: what it portrays and who produced it."""
    return CREDITS_DESCRIPTION.format(products(datasets), producers(datasets))


def attribution(datasets, server_version):
    """Gets a layer's credits."""
    version = server_version if server_version and server_version.strip() else UNKNOWN_VERSION
    return CREDITS_ATTRIBUTION.format(producers(datasets), version)


def group_name(inputs, dataset_count):
    """Gets the group's name: the one input's name, or "{n} datasets" for several (§3.3)."""
    source = inputs[0] if len(inputs) == 1 else GROUP_DATASET_COUNT.format(dataset_count)
    return GROUP_NAME.format(source)


def _number(value):
    # System.Text.Json writes a whole double without ".0".
    if isinstance(value, float) and math.isfinite(value) and value == int(value):
        return int(value)
    return value


def layer_marker(kind, datasets):
    """Gets a chart layer's marker, compact JSON, as the add-in writes it."""
    items = []
    for d in layer_datasets(datasets):
        item = {"path": d.path, "product": d.product}
        if d.edition is not None:
            item["edition"] = d.edition
        if d.producer is not None:
            item["producer"] = d.producer
        if d.not_for_navigation:
            item["notForNavigation"] = True
        if d.extent is not None:
            item["extent"] = [_number(v) for v in d.extent]
        items.append(item)
    if not items:
        raise ValueError("A chart layer needs at least one dataset.")
    return _compact({"version": MARKER_VERSION, "role": "layer", "kind": kind, "datasets": items})


def group_marker(inputs):
    """Gets the group's marker: the names of the inputs added to it."""
    unique = []
    seen = set()
    for name in inputs:
        if _ignore_case(name) not in seen:
            seen.add(_ignore_case(name))
            unique.append(name)
    return _compact({"version": MARKER_VERSION, "role": "group", "inputs": unique})


def read_marker(text):
    """Reads a marker's JSON, or returns ``None`` when it isn't a marker this version understands."""
    try:
        marker = json.loads(text)
    except (TypeError, ValueError):
        return None
    if not isinstance(marker, dict) or marker.get("version") != MARKER_VERSION:
        return None
    if marker.get("role") not in ("group", "layer"):
        return None
    return marker


def _compact(value):
    return json.dumps(value, ensure_ascii=False, separators=(",", ":"))


def layer_uri(kind):
    """Gets the CIM URI of a chart layer of ``kind`` in a document."""
    return _URI_PREFIX + kind + ".json"


def connection(capabilities_url, style=DEFAULT_STYLE):
    """Gets the ``CIMWMTSServiceConnection`` to a chart server."""
    if style not in STYLES:
        raise ValueError("The style must be one of {}.".format(", ".join(STYLES)))
    return {
        "type": "CIMWMTSServiceConnection",
        "serverConnection": {
            "type": "CIMInternetServerConnection",
            "anonymous": True,
            "hideUserProperty": True,
            "url": capabilities_url,
        },
        "layerName": WMTS_LAYER,
        "style": style,
        "tileMatrixSet": WMTS_TILE_MATRIX_SET,
        "imageFormat": "image/png",
    }


class ChartLayer:
    """What one SoundCharts WMTS layer is made from. Mirrors ``ChartLayerDefinition``."""

    def __init__(self, kind, datasets, capabilities_url=PLACEHOLDER_CAPABILITIES, style=DEFAULT_STYLE,
                 min_scale=0.0, server_version=None):
        """Creates a layer.

        :param kind: ``chart``, ``water-level`` or ``surface-currents``.
        :param datasets: The ``ChartDataset`` objects it draws.
        :param capabilities_url: The chart server's WMTS capabilities URL.
        :param style: The palette: ``day``, ``dusk`` or ``night``.
        :param min_scale: The minimum scale denominator, or 0 for none.
        :param server_version: The ``s100`` version for the credits, or ``None``.
        """
        if kind not in _NAMES:
            raise ValueError("Unknown layer kind {!r}.".format(kind))
        self.kind = kind
        self.datasets = layer_datasets(datasets)
        self.capabilities_url = capabilities_url
        self.style = style
        self.min_scale = min_scale
        self.server_version = server_version

    @property
    def name(self):
        """The layer's name in Contents."""
        return display_name(self.kind)


def layer_definition(layer):
    """Gets a chart layer's ``CIMTiledServiceLayer`` definition."""
    return {
        "type": "CIMTiledServiceLayer",
        "name": layer.name,
        "uRI": layer_uri(layer.kind),
        "description": description(layer.datasets),
        "attribution": attribution(layer.datasets, layer.server_version),
        "layerType": "Operational",
        "minScale": _number(round(max(0.0, float(layer.min_scale)), 3)),
        "showLegends": True,
        "visibility": True,
        "displayCacheType": "InSession",
        "maxDisplayCacheAge": 5,
        "customProperties": _marker_property(layer_marker(layer.kind, layer.datasets)),
        "serviceConnection": connection(layer.capabilities_url, layer.style),
    }


def _marker_property(marker):
    return [{"type": "CIMStringMap", "key": MARKER_KEY, "value": marker}]


def _ordered(layers):
    return sorted(layers, key=lambda layer: TOP_FIRST.index(layer.kind))


def group_document(name, inputs, layers):
    """Gets a layer document holding the group and its chart layers, top first.

    :param name: The group's name, **SoundCharts · {source}**.
    :param inputs: The names of the inputs, for the group's marker.
    :param layers: The ``ChartLayer`` objects.
    :returns: The ``.lyrx`` JSON text, LF line endings, no trailing newline.
    """
    if not name or not name.strip():
        raise ValueError("The group needs a name.")
    ordered = _ordered(layers)
    if not ordered:
        raise ValueError("The group needs at least one layer.")
    datasets = [d for layer in ordered for d in layer.datasets]
    group = {
        "type": "CIMGroupLayer",
        "name": name,
        "uRI": GROUP_URI,
        "description": description(datasets),
        "layerType": "Operational",
        "showLegends": True,
        "visibility": True,
        "expanded": True,
        "customProperties": _marker_property(group_marker(inputs)),
        "layers": [layer_uri(layer.kind) for layer in ordered],
    }
    return _document([GROUP_URI], [group] + [layer_definition(layer) for layer in ordered])


def layer_document(layer):
    """Gets a layer document holding one chart layer."""
    return _document([layer_uri(layer.kind)], [layer_definition(layer)])


def _document(top_level, definitions):
    document = {
        "type": "CIMLayerDocument",
        "version": CIM_VERSION,
        "build": 0,
        "layers": list(top_level),
        "layerDefinitions": definitions,
    }
    return json.dumps(document, ensure_ascii=False, indent=2)


def compose(inputs, datasets, capabilities_url=PLACEHOLDER_CAPABILITIES, style=DEFAULT_STYLE, server_version=None):
    """Gets the group document for datasets added as ``inputs``.

    Each dataset joins the layer of its kind, as Add SoundCharts Data does.

    :param inputs: The names of what was added (cells, exchange sets, folders).
    :param datasets: The ``ChartDataset`` objects found in them.
    :returns: ``(group name, .lyrx JSON text)``.
    """
    by_kind = {}
    for dataset in datasets:
        by_kind.setdefault(dataset.kind, []).append(dataset)
    if not by_kind:
        raise ValueError("There are no datasets to add.")
    layers = [ChartLayer(kind, items, capabilities_url, style, server_version=server_version)
              for kind, items in by_kind.items()]
    count = sum(len(layer.datasets) for layer in layers)
    name = group_name(list(inputs), count)
    return name, group_document(name, inputs, layers)


def write(path, text):
    """Writes a layer document as UTF-8 with LF line endings."""
    with open(path, "w", encoding="utf-8", newline="\n") as stream:
        stream.write(text)
        stream.write("\n")
