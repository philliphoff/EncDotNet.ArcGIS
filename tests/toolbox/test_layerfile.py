"""The toolbox writes the same layer document as the add-in.

The add-in's snapshots (tests/EncDotNet.ArcGIS.SoundCharts.Tests/ChartLayers/
Snapshots) are the contract between the two. These tests build the snapshot
layers (ChartLayerSamples.cs) here and compare the text byte for byte, so a
change to the CIM on either side fails until both agree.
"""

import json
import os
import re
import unittest
import xml.etree.ElementTree as ElementTree

import support
from soundcharts_tools import layerfile

CAPABILITIES = "http://127.0.0.1:52100/abc-123/wmts/1.0.0/WMTSCapabilities.xml"
VERSION = "0.27.1-main.20261113"
ZOOM_11 = 591_657_527.591555 / 2 ** 11

CELL = layerfile.ChartDataset(r"C:\Charts\US5MA1BO\US5MA1BO.000", "S-57", None, (-71.06, 42.31, -70.94, 42.38), "US")
S101 = layerfile.ChartDataset(r"C:\Charts\S101\101AA00DS0003.000", "S-101", "2.0.0", (-1.0, 50.0, 0.0, 51.0), "AA00",
                              not_for_navigation=True)
TIDE = layerfile.ChartDataset(r"C:\Charts\S104\104US004SC1BO_20251217T12Z.h5", "S-104", "2.0.0",
                              (-80.1, 32.6, -79.8, 32.9), "US")
CURRENTS = layerfile.ChartDataset(r"C:\Charts\S111\111US00_DBOFS_20260320T18Z_US4DE1BB.h5", "S-111", "2.0.0",
                                  (-75.6, 38.7, -75.0, 39.9), "US")


def definition(kind, *datasets):
    return layerfile.ChartLayer(kind, datasets, CAPABILITIES, min_scale=ZOOM_11, server_version=VERSION)


def snapshot(name):
    with open(os.path.join(support.SNAPSHOTS, name), encoding="utf-8") as stream:
        return stream.read().replace("\r\n", "\n").rstrip("\n")


class SnapshotTests(unittest.TestCase):
    def test_group_matches_the_add_in_snapshot(self):
        text = layerfile.group_document(
            "SoundCharts · 3 datasets", ["US5MA1BO", "S104"],
            [definition(layerfile.CHART, CELL, S101), definition(layerfile.WATER_LEVEL, TIDE)])
        self.assertEqual(snapshot("group.lyrx"), text)

    def test_layer_matches_the_add_in_snapshot(self):
        text = layerfile.layer_document(definition(layerfile.SURFACE_CURRENTS, CURRENTS))
        self.assertEqual(snapshot("surface-currents.lyrx"), text)


class StringsTests(unittest.TestCase):
    """The names and credits are the add-in's Strings.resx values."""

    @classmethod
    def setUpClass(cls):
        root = ElementTree.parse(support.STRINGS).getroot()
        cls.strings = {d.get("name"): d.findtext("value") for d in root.findall("data")}

    def test_names_and_credits_match_the_add_in(self):
        expected = {
            "Group_Name": layerfile.GROUP_NAME,
            "Group_DatasetCount": layerfile.GROUP_DATASET_COUNT,
            "Credits_Description": layerfile.CREDITS_DESCRIPTION,
            "Credits_Attribution": layerfile.CREDITS_ATTRIBUTION,
            "Credits_UnknownProducer": layerfile.CREDITS_UNKNOWN_PRODUCER,
            "Layer_Chart": layerfile.display_name(layerfile.CHART),
            "Layer_WaterLevel": layerfile.display_name(layerfile.WATER_LEVEL),
            "Layer_SurfaceCurrents": layerfile.display_name(layerfile.SURFACE_CURRENTS),
        }
        for key, value in expected.items():
            with self.subTest(key=key):
                self.assertEqual(self.strings[key], value)


class ComposeTests(unittest.TestCase):
    def test_one_input_names_the_group_after_it(self):
        name, text = layerfile.compose(["US5MA1BO"], [CELL])
        self.assertEqual("SoundCharts · US5MA1BO", name)
        document = json.loads(text)
        self.assertEqual(["CIMPATH=soundcharts/soundcharts.json"], document["layers"])
        self.assertEqual(["CIMGroupLayer", "CIMTiledServiceLayer"], [d["type"] for d in document["layerDefinitions"]])

    def test_several_inputs_count_the_datasets(self):
        name, _ = layerfile.compose(["US5MA1BO", "S104"], [CELL, TIDE, CELL])
        self.assertEqual("SoundCharts · 2 datasets", name)

    def test_datasets_go_to_the_layer_of_their_kind_time_layers_on_top(self):
        _, text = layerfile.compose(["Mixed"], [CELL, CURRENTS, TIDE, S101])
        group = json.loads(text)["layerDefinitions"][0]
        self.assertEqual(["CIMPATH=soundcharts/surface-currents.json", "CIMPATH=soundcharts/water-level.json",
                          "CIMPATH=soundcharts/chart.json"], group["layers"])

    def test_layers_point_at_the_placeholder_until_the_add_in_connects_them(self):
        _, text = layerfile.compose(["US5MA1BO"], [CELL], style="night")
        layer = json.loads(text)["layerDefinitions"][1]
        self.assertEqual(layerfile.PLACEHOLDER_CAPABILITIES, layer["serviceConnection"]["serverConnection"]["url"])
        self.assertEqual("night", layer["serviceConnection"]["style"])
        self.assertEqual(0, layer["minScale"])
        self.assertIn("Portrayal: SoundCharts (unknown)", layer["attribution"])

    def test_the_marker_is_one_the_add_in_reads(self):
        _, text = layerfile.compose(["US5MA1BO"], [CELL])
        layer = json.loads(text)["layerDefinitions"][1]
        marker = layerfile.read_marker(layer["customProperties"][0]["value"])
        self.assertEqual("layer", marker["role"])
        self.assertEqual("chart", marker["kind"])
        self.assertEqual(r"C:\Charts\US5MA1BO\US5MA1BO.000", marker["datasets"][0]["path"])

    def test_no_datasets_is_an_error(self):
        with self.assertRaises(ValueError):
            layerfile.compose(["Empty"], [])

    def test_an_unknown_palette_is_an_error(self):
        with self.assertRaises(ValueError):
            layerfile.compose(["US5MA1BO"], [CELL], style="sunset")


class CreditsTests(unittest.TestCase):
    def test_producers_by_count_then_name(self):
        datasets = [layerfile.ChartDataset("a", "S-57", producer="US"),
                    layerfile.ChartDataset("b", "S-57", producer="ca"),
                    layerfile.ChartDataset("c", "S-57", producer="CA "),
                    layerfile.ChartDataset("d", "S-57", producer="AA")]
        self.assertEqual("ca, AA, US", layerfile.producers(datasets))

    def test_unknown_producer(self):
        self.assertEqual("unknown producer", layerfile.producers([layerfile.ChartDataset("a", "S-57")]))

    def test_products_in_ordinal_order(self):
        self.assertEqual("S-101, S-57", layerfile.products([CELL, S101, CELL]))


class WriteTests(unittest.TestCase):
    def test_writes_utf8_with_a_trailing_newline(self):
        path = os.path.join(support.temp_folder(self), "chart.lyrx")
        _, text = layerfile.compose(["US5MA1BO"], [CELL])
        layerfile.write(path, text)
        with open(path, "rb") as stream:
            data = stream.read()
        self.assertTrue(data.endswith(b"}\n"))
        self.assertNotIn(b"\r\n", data)
        self.assertIn("SoundCharts · US5MA1BO".encode("utf-8"), data)


class BrandTests(unittest.TestCase):
    def test_layer_text_says_soundcharts_not_s_100(self):
        _, text = layerfile.compose(["US5MA1BO"], [CELL])
        for definition_ in json.loads(text)["layerDefinitions"]:
            self.assertFalse(re.search(r"S-100\b", definition_["name"] + definition_["description"]))


if __name__ == "__main__":
    unittest.main()
