"""Paths and helpers shared by the toolbox tests.

Run from the repository root with any Python 3.9+, ArcGIS Pro's included:

    python -I -m unittest discover -s tests/toolbox -v

``-I`` keeps the current folder off ``sys.path``; this module puts the
toolbox folder there.
"""

import os
import sys
import tempfile

TESTS = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(TESTS))
TOOLBOX = os.path.join(REPO, "src", "EncDotNet.ArcGIS.SoundCharts", "Toolboxes")
SNAPSHOTS = os.path.join(REPO, "tests", "EncDotNet.ArcGIS.SoundCharts.Tests", "ChartLayers", "Snapshots")
STRINGS = os.path.join(REPO, "src", "EncDotNet.ArcGIS.SoundCharts", "Resources", "Strings.resx")
CONFIG = os.path.join(REPO, "src", "EncDotNet.ArcGIS.SoundCharts", "Config.daml")

DATASETS_VARIABLE = "SOUNDCHARTS_S100_TEST_DATASETS"
"""EncDotNet.S100's tests/datasets folder, as for the C# tests; tests that need real data skip without it."""

if TOOLBOX not in sys.path:
    sys.path.insert(0, TOOLBOX)

# Don't leave __pycache__ folders in the toolbox: the add-in packages that folder.
sys.dont_write_bytecode = True


def upstream(relative):
    """Gets a file under EncDotNet.S100's tests/datasets, or None."""
    root = os.environ.get(DATASETS_VARIABLE)
    if not root:
        return None
    path = os.path.join(root, *relative.split("/"))
    return path if os.path.exists(path) else None


def temp_folder(test):
    """Makes a temporary folder removed after ``test``."""
    folder = tempfile.TemporaryDirectory(prefix="soundcharts-toolbox-")
    test.addCleanup(folder.cleanup)
    return folder.name


def touch(path, content=b""):
    """Writes a file and its folders."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as stream:
        stream.write(content)
    return path
