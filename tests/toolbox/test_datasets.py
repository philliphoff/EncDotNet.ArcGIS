"""Finding the datasets in the inputs, and reading s100 info."""

import json
import os
import re
import unittest

import support
from soundcharts_tools import datasets, layerfile

INFO = os.path.join(support.TESTS, "info")


def recorded(name):
    """Gets s100 info output recorded from the pinned s100 on EncDotNet.S100's test datasets (info/*.txt)."""
    with open(os.path.join(INFO, name), encoding="utf-8") as stream:
        return stream.read()


def mangled(text, replacement):
    """Gets output whose box characters arrived in another code page: each non-ASCII character replaced."""
    return re.sub(r"[^\x00-\x7f]", replacement, text)

# s100 info as Pro's Python may read it: the box characters in whatever code page .NET chose.
INFO_S101 = """\
\ufffd\ufffd\ufffd\ufffd\ufffd
\ufffd Property          \ufffd Value                                                    \ufffd
\ufffd Dataset           \ufffd C:/Charts/S101/S-101/DATASET_FILES/101AA00DS0003.000      \ufffd
\ufffd Specification     \ufffd S-101                                                    \ufffd
\ufffd Edition           \ufffd 1.0.2                                                    \ufffd
\ufffd Supported edition \ufffd 1.2.0, 2.0.0                                             \ufffd
\ufffd Catalogue version \ufffd S-101 2.0.0                                              \ufffd
"""

INFO_S57 = """\
\u2502 Property        \u2502 Value                                                      \u2502
\u2502 Specification   \u2502 S-57                                                       \u2502
\u2502 Edition         \u2502 (not declared)                                             \u2502
"""


class InfoTests(unittest.TestCase):
    def test_reads_product_and_edition(self):
        self.assertEqual(("S-101", "1.0.2"), datasets.parse_info(INFO_S101))

    def test_an_undeclared_edition_is_none(self):
        self.assertEqual(("S-57", None), datasets.parse_info(INFO_S57))

    def test_no_table(self):
        self.assertEqual((None, None), datasets.parse_info("Error: Path not found"))

    def test_reads_products_that_arent_s100(self):
        self.assertEqual(("TC", "1.0.0"), datasets.parse_info(recorded("tc-al092022-5day-020.txt")))
        self.assertEqual(("CAP", "1.2.0"), datasets.parse_info(recorded("cap-nws-gale-warning-pzz560.txt")))
        self.assertEqual(("S-57", None), datasets.parse_info(recorded("s57-US5MA1BO.txt")))
        self.assertEqual(("GRIB2", None), datasets.parse_info("\u2502 Specification   \u2502 GRIB2      \u2502"))
        self.assertEqual(("NetCDF", None), datasets.parse_info("| Specification | NetCDF |"))


class TimeStepTests(unittest.TestCase):
    """The time steps decide the layer, by the add-in's rule: more than one distinct step is a time layer."""

    def test_reads_the_advisory_forecast_points_not_the_forecast_table(self):
        times = datasets.parse_times(recorded("tc-al092022-5day-020.txt"))
        self.assertEqual(["2022-09-28 00:00:00", "2022-09-28 12:00:00", "2022-09-29 00:00:00", "2022-09-29 12:00:00",
                          "2022-09-30 00:00:00", "2022-09-30 12:00:00", "2022-10-01 00:00:00",
                          "2022-10-02 00:00:00"], times)

    def test_reads_every_s104_step(self):
        times = datasets.parse_times(recorded("s104-104US004SC1BO.txt"))
        self.assertEqual(180, len(times))
        self.assertEqual(("2025-12-17 13:00:00", "2025-12-25 00:00:00"), (times[0], times[-1]))

    def test_reads_cap_steps_not_the_warnings_table(self):
        self.assertEqual(["2026-09-13 22:00:00", "2026-09-14 04:00:00"],
                         datasets.parse_times(recorded("cap-nws-gale-warning-pzz560.txt")))

    def test_a_chart_has_no_steps(self):
        self.assertEqual([], datasets.parse_times(recorded("s57-US5MA1BO.txt")))
        self.assertEqual([], datasets.parse_times(INFO_S101))

    def test_reads_steps_in_another_code_page(self):
        for replacement in ("\ufffd", "?"):
            with self.subTest(replacement=replacement):
                text = mangled(recorded("tc-al092022-5day-020.txt"), replacement)
                self.assertEqual(("TC", "1.0.0"), datasets.parse_info(text))
                self.assertEqual(8, len(datasets.parse_times(text)))

    def test_time_varying_needs_more_than_one_distinct_step(self):
        self.assertFalse(datasets.is_time_varying([]))
        self.assertFalse(datasets.is_time_varying(["2001-04-22 00:00:00"]))
        self.assertFalse(datasets.is_time_varying(["2001-04-22 00:00:00", "2001-04-22 00:00:00"]))
        self.assertTrue(datasets.is_time_varying(["2001-04-22 00:00:00", "2001-04-23 00:00:00"]))


class DatasetFromInfoTests(unittest.TestCase):
    """What a dataset's s100 info output makes of it, and the layer it goes in."""

    def dataset(self, name, path):
        return datasets.dataset_from_info(path, recorded(name))

    def test_an_advisory_is_weather(self):
        storm = self.dataset("tc-al092022-5day-020.txt", r"C:\Charts\TC\al092022_5day_020.zip")
        self.assertEqual(("TC", "1.0.0", None, True), (storm.product, storm.edition, storm.producer, storm.time_varying))
        self.assertEqual(layerfile.WEATHER, storm.kind)

    def test_cap_with_two_steps_is_weather(self):
        self.assertEqual(layerfile.WEATHER, self.dataset("cap-nws-gale-warning-pzz560.txt", "gale.xml").kind)

    def test_one_ice_chart_date_stays_in_chart(self):
        ice = self.dataset("s411-iho_4112C00TDS001.txt", "iho_4112C00TDS001.gml")
        self.assertEqual(("S-411", False, layerfile.CHART), (ice.product, ice.time_varying, ice.kind))

    def test_s104_is_water_level_and_time_varying(self):
        tide = self.dataset("s104-104US004SC1BO.txt", "104US004SC1BO_20251217T12Z.h5")
        self.assertEqual(("S-104", "2.0.0", "US", True, layerfile.WATER_LEVEL),
                         (tide.product, tide.edition, tide.producer, tide.time_varying, tide.kind))

    def test_a_cell_is_chart(self):
        cell = self.dataset("s57-US5MA1BO.txt", "US5MA1BO.000")
        self.assertEqual(("S-57", False, layerfile.CHART), (cell.product, cell.time_varying, cell.kind))

    def test_no_product_is_none(self):
        self.assertIsNone(datasets.dataset_from_info("x.000", "Error: Dataset file not found"))

    def test_an_advisory_makes_the_add_ins_weather_layer(self):
        # The toolbox records no footprint; give the advisory the snapshot's, and the rest comes from s100 info.
        storm = self.dataset("tc-al092022-5day-020.txt", r"C:\Charts\TC\al092022_5day_020.zip")
        storm.extent = (-85.41, 12.3, -66.3, 38.35)
        layer = layerfile.ChartLayer(storm.kind, [storm], "http://127.0.0.1:52100/abc-123/wmts/1.0.0/WMTSCapabilities.xml",
                                     min_scale=591_657_527.591555 / 2 ** 11, server_version="0.27.1-main.20261113")
        with open(os.path.join(support.SNAPSHOTS, "weather.lyrx"), encoding="utf-8") as stream:
            expected = stream.read().replace("\r\n", "\n").rstrip("\n")
        self.assertEqual(expected, layerfile.layer_document(layer))

    def test_a_group_of_a_cell_and_an_advisory_has_chart_and_weather(self):
        cell = self.dataset("s57-US5MA1BO.txt", r"C:\Charts\US5MA1BO.000")
        storm = self.dataset("tc-al092022-5day-020.txt", r"C:\Charts\al092022_5day_020.zip")
        _, text = layerfile.compose(["US5MA1BO", "al092022_5day_020"], [cell, storm])
        names = [layer["name"] for layer in json.loads(text)["layerDefinitions"][1:]]
        self.assertEqual(["Weather", "Chart"], names)


class ProducerTests(unittest.TestCase):
    def test_names(self):
        cases = {
            r"C:\Charts\101AA00DS0003.000": "AA",
            "104US004SC1BO_20251217T12Z.h5": "US",
            "US5MA1BO.000": "US",
            "warnings.gml": None,
            "1a1BB.000": None,
        }
        for path, expected in cases.items():
            with self.subTest(path=path):
                self.assertEqual(expected, datasets.producer_from_name(path))


class ExpandTests(unittest.TestCase):
    def setUp(self):
        self.root = support.temp_folder(self)
        self.cell = support.touch(os.path.join(self.root, "S57", "US5MA1BO", "US5MA1BO.000"))
        support.touch(os.path.join(self.root, "S57", "US5MA1BO", "US5MA1BO.001"))
        support.touch(os.path.join(self.root, "S57", "US5MA1BO", "US347BOA.TXT"))
        self.catalogue = support.touch(os.path.join(self.root, "S101", "CATALOG.XML"))
        self.s101 = [support.touch(os.path.join(self.root, "S101", "S-101", "DATASET_FILES", name))
                     for name in ("101AA00DS0004.000", "101AA00DS0003.000")]

    def test_a_dataset_is_itself(self):
        inputs, warnings = datasets.expand([self.cell])
        self.assertEqual([], warnings)
        self.assertEqual(("US5MA1BO", [self.cell]), (inputs[0].name, inputs[0].files))

    def test_a_catalogue_is_its_folder(self):
        inputs, _ = datasets.expand([self.catalogue])
        self.assertEqual("S101", inputs[0].name)
        self.assertEqual(sorted(self.s101), inputs[0].files)

    def test_a_folder_is_searched_without_update_files(self):
        inputs, _ = datasets.expand([os.path.join(self.root, "S57")])
        self.assertEqual([self.cell], inputs[0].files)

    def test_a_zip_is_left_to_s100(self):
        # An NHC advisory zip is a dataset; s100 info says whether a .zip is one.
        archive = support.touch(os.path.join(self.root, "al092022_5day_020.zip"))
        inputs, warnings = datasets.expand([archive])
        self.assertEqual([], warnings)
        self.assertEqual(("al092022_5day_020", [archive]), (inputs[0].name, inputs[0].files))

    def test_skipped_inputs_warn(self):
        update = os.path.join(self.root, "S57", "US5MA1BO", "US5MA1BO.001")
        empty = os.path.join(self.root, "empty")
        os.makedirs(empty)
        inputs, warnings = datasets.expand([update, empty, os.path.join(self.root, "missing.000")])
        self.assertEqual([], inputs)
        self.assertEqual(3, len(warnings))
        self.assertIn("base cell", warnings[0])
        self.assertIn("no datasets", warnings[1])
        self.assertIn("not found", warnings[2])


if __name__ == "__main__":
    unittest.main()
