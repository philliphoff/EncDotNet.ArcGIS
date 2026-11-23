"""SoundChartsTools.pyt loads, names its tools as the design brief says, and maps parameters.

Runs against fake_arcpy; with s100 and EncDotNet.S100's test datasets it
also runs tools end to end (see test_s100.py for the CLI checks).
"""

import importlib.machinery
import importlib.util
import json
import os
import re
import sys
import unittest

import fake_arcpy
import support
from soundcharts_tools import locate

TOOLBOX_FILE = os.path.join(support.TOOLBOX, "SoundChartsTools.pyt")


def load_toolbox():
    arcpy = fake_arcpy.install()
    sys.modules["arcpy"] = arcpy
    loader = importlib.machinery.SourceFileLoader("soundcharts_toolbox_under_test", TOOLBOX_FILE)
    spec = importlib.util.spec_from_loader(loader.name, loader)
    module = importlib.util.module_from_spec(spec)
    loader.exec_module(module)
    return module, arcpy


class ToolboxTests(unittest.TestCase):
    def setUp(self):
        self.module, self.arcpy = load_toolbox()
        self.addCleanup(sys.modules.pop, "arcpy", None)

    def test_the_toolbox_is_soundcharts_tools(self):
        toolbox = self.module.Toolbox()
        self.assertEqual("SoundCharts Tools", toolbox.label)
        self.assertEqual("soundcharts", toolbox.alias)
        labels = [tool().label for tool in toolbox.tools]
        self.assertEqual(["Add SoundCharts Layer", "Export Tiles", "Render to Image", "Validate Dataset",
                          "Verify Exchange Set", "Convert Dataset"], labels)

    def test_no_tool_text_says_s_100_for_the_brand(self):
        # D36: SoundCharts, never "S-100", in the UI. Product names (S-100 Part 15) are fine in descriptions.
        for tool in self.module.Toolbox().tools:
            instance = tool()
            with self.subTest(tool=instance.label):
                self.assertNotIn("S-100", instance.label)
                for parameter in instance.getParameterInfo():
                    self.assertNotRegex(parameter.displayName, r"S-?100\b")

    def test_parameters_are_well_formed(self):
        for tool in self.module.Toolbox().tools:
            instance = tool()
            parameters = instance.getParameterInfo()
            names = [p.name for p in parameters]
            with self.subTest(tool=instance.label):
                self.assertEqual(len(names), len(set(names)))
                for parameter in parameters:
                    self.assertRegex(parameter.name, r"^[a-z][a-z0-9_]*$")
                    if parameter.parameterType == "Derived":
                        self.assertEqual("Output", parameter.direction)
                instance.updateParameters(parameters)
                instance.updateMessages(parameters)

    def test_export_tiles_picks_the_container_from_the_output(self):
        tool = self.module.ExportTiles()
        parameters = tool.getParameterInfo()
        parameters[1].value = r"C:\out\chart.mbtiles"
        parameters[1].altered = True
        tool.updateParameters(parameters)
        self.assertEqual("MBTiles", parameters[2].value)

    def test_export_tiles_shows_a_contradiction_on_the_output(self):
        folder = support.temp_folder(self)
        cell = support.touch(os.path.join(folder, "US5MA1BO.000"))
        tool = self.module.ExportTiles()
        parameters = tool.getParameterInfo()
        parameters[0].value = [cell]
        parameters[1].value = os.path.join(folder, "chart.mbtiles")
        parameters[2].value = "PMTiles"
        tool.updateMessages(parameters)
        self.assertIn("needs a file ending in .pmtiles", parameters[1].error)

    def test_convert_enables_the_exchange_set_options_for_s101_to_s57(self):
        tool = self.module.ConvertDataset()
        parameters = tool.getParameterInfo()
        tool.updateParameters(parameters)
        self.assertFalse(parameters[3].enabled)
        parameters[1].value = "S-101 to S-57"
        parameters[3].value = True
        tool.updateParameters(parameters)
        self.assertTrue(parameters[3].enabled and parameters[4].enabled)

    def test_add_layer_without_datasets_fails_with_a_message(self):
        tool = self.module.AddSoundChartsLayer()
        parameters = tool.getParameterInfo()
        parameters[0].value = [os.path.join(support.temp_folder(self), "missing.000")]
        with self.assertRaises(fake_arcpy.ExecuteError):
            tool.execute(parameters, None)
        self.assertEqual("warning", self.arcpy.messages[0][0])
        self.assertEqual(("error", "No datasets found in the inputs."), self.arcpy.messages[-1])


def _s100():
    try:
        return locate.locate(support.TOOLBOX)
    except locate.S100NotFound:
        return None


@unittest.skipUnless(_s100() and support.upstream("S57/US5MA1BO/US5MA1BO.000"),
                     "Needs s100 and SOUNDCHARTS_S100_TEST_DATASETS.")
class EndToEndTests(unittest.TestCase):
    def setUp(self):
        self.module, self.arcpy = load_toolbox()
        self.addCleanup(sys.modules.pop, "arcpy", None)
        self.folder = support.temp_folder(self)
        self.arcpy.env.scratchFolder = self.folder
        self.cell = support.upstream("S57/US5MA1BO/US5MA1BO.000")

    def test_add_soundcharts_layer_writes_the_layer_file(self):
        tool = self.module.AddSoundChartsLayer()
        parameters = tool.getParameterInfo()
        tide = support.upstream("S104/104US004SC1BO_20251217T12Z.h5")
        parameters[0].value = [self.cell] + ([tide] if tide else [])
        tool.execute(parameters, None)

        path = self.arcpy.outputs[2]
        with open(path, encoding="utf-8") as stream:
            document = json.load(stream)
        group, *layers = document["layerDefinitions"]
        self.assertTrue(group["name"].startswith("SoundCharts · "))
        chart = next(layer for layer in layers if layer["name"] == "Chart")
        marker = json.loads(chart["customProperties"][0]["value"])
        self.assertEqual([("S-57", "US")], [(d["product"], d["producer"]) for d in marker["datasets"]])
        self.assertRegex(chart["attribution"], r"Portrayal: SoundCharts \(\d+\.\d+")
        self.assertIn(("warning", "No map to add the layer to. Add {} to a map in ArcGIS Pro.".format(path)),
                      self.arcpy.messages)

    def test_export_tiles_reports_progress(self):
        tool = self.module.ExportTiles()
        parameters = tool.getParameterInfo()
        out = os.path.join(self.folder, "chart.pmtiles")
        parameters[0].value = [self.cell]
        parameters[1].value = out
        parameters[4].value = 14
        tool.execute(parameters, None)
        self.assertTrue(os.path.getsize(out) > 0)
        self.assertIn(("position", 100), self.arcpy.progress)
        self.assertTrue(any(re.match(r"zoom 14: \d+ tile\(s\) written", text) for _, text in self.arcpy.messages))

    def test_validate_dataset_sets_valid(self):
        tool = self.module.ValidateDataset()
        parameters = tool.getParameterInfo()
        report = os.path.join(self.folder, "report.json")
        parameters[0].value = [self.cell]
        parameters[4].value = report
        tool.execute(parameters, None)
        self.assertIs(True, self.arcpy.outputs[5])
        with open(report, encoding="utf-8") as stream:
            self.assertEqual("S-57", json.load(stream)["datasets"][0]["report"]["specification"])

    def test_a_failing_run_fails_the_tool(self):
        tool = self.module.RenderToImage()
        parameters = tool.getParameterInfo()
        bogus = support.touch(os.path.join(self.folder, "bogus.000"), b"not a cell")
        parameters[0].value = [bogus]
        parameters[1].value = os.path.join(self.folder, "out.png")
        with self.assertRaises(fake_arcpy.ExecuteError):
            tool.execute(parameters, None)
        self.assertEqual("error", self.arcpy.messages[-1][0])
        self.assertIn("exit code", self.arcpy.messages[-1][1])


if __name__ == "__main__":
    unittest.main()
