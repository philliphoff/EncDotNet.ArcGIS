"""The s100 arguments each tool builds, and the mistakes each one catches."""

import os
import unittest

import support
from soundcharts_tools import commands


class InputsTests(unittest.TestCase):
    def setUp(self):
        self.folder = support.temp_folder(self)
        self.cell = support.touch(os.path.join(self.folder, "US5MA1BO.000"))
        self.tide = support.touch(os.path.join(self.folder, "104US00_X.h5"))
        self.exchange_set = os.path.join(self.folder, "ENC_ROOT")
        support.touch(os.path.join(self.exchange_set, "CATALOG.031"))

    def test_one_input_is_positional(self):
        self.assertEqual([self.exchange_set], commands.inputs([self.exchange_set]))

    def test_several_datasets_are_layers(self):
        self.assertEqual(["--layer", self.cell, "--layer", self.tide], commands.inputs([self.cell, self.tide]))

    def test_a_folder_with_other_inputs_is_an_error(self):
        with self.assertRaisesRegex(ValueError, "can't combine"):
            commands.inputs([self.cell, self.exchange_set])

    def test_no_input_is_an_error(self):
        with self.assertRaises(ValueError):
            commands.inputs([])


class TilesExportTests(unittest.TestCase):
    def setUp(self):
        self.folder = support.temp_folder(self)
        self.cell = support.touch(os.path.join(self.folder, "US5MA1BO.000"))

    def out(self, name):
        return os.path.join(self.folder, name)

    def test_the_container_follows_the_output(self):
        for name, container in (("chart.pmtiles", "pmtiles"), ("chart.mbtiles", "mbtiles"), ("chart.gpkg", "gpkg"),
                                ("chart.tif", "geotiff"), ("chart.TIFF", "geotiff"), ("tiles", "xyz")):
            with self.subTest(name=name):
                arguments = commands.tiles_export([self.cell], self.out(name))
                self.assertEqual(["tiles", "export", self.cell, "-o", self.out(name), "--container", container],
                                 arguments[:7])

    def test_defaults_add_only_the_palette(self):
        arguments = commands.tiles_export([self.cell], self.out("chart.pmtiles"))
        self.assertEqual(["--palette", "day"], arguments[7:])

    def test_every_option(self):
        arguments = commands.tiles_export(
            [self.cell], self.out("chart.mbtiles"), container="mbtiles", min_zoom=10, max_zoom=15,
            extent=(-70.35, 41.25, -70.275, 41.325), tile_size=512, image_format="webp", quality=80,
            palette="Night", time_step=3, background="80ffffff", no_text=True, offline_basemap=True,
            skip_empty=True, only="S101, S102", allow_large=True)
        self.assertEqual(
            ["--min-zoom", "10", "--max-zoom", "15", "--bbox", "-70.35,41.25,-70.275,41.325", "--tile-size", "512",
             "--format", "webp", "--quality", "80", "--palette", "night", "--time-step", "3", "--no-text",
             "--basemap", "offline", "--background", "#80FFFFFF", "--skip-empty", "--only", "S101,S102", "--yes"],
            arguments[7:])

    def test_quality_is_left_out_for_png(self):
        self.assertNotIn("--quality", commands.tiles_export([self.cell], self.out("t.pmtiles"), quality=50))

    def test_a_container_that_contradicts_the_output_is_an_error(self):
        with self.assertRaisesRegex(ValueError, r"PMTiles output needs a file ending in \.pmtiles"):
            commands.tiles_export([self.cell], self.out("chart.mbtiles"), container="pmtiles")
        with self.assertRaisesRegex(ValueError, "XYZ folder"):
            commands.tiles_export([self.cell], self.out("chart.pmtiles"), container="xyz")

    def test_bad_values_are_errors(self):
        cases = [dict(min_zoom=25), dict(min_zoom=12, max_zoom=10), dict(tile_size=300), dict(image_format="gif"),
                 dict(palette="sunset"), dict(background="white"), dict(quality=0), dict(time_step=-1),
                 dict(extent=(10, 0, 5, 1))]
        for case in cases:
            with self.subTest(case=case), self.assertRaises(ValueError):
                commands.tiles_export([self.cell], self.out("t.pmtiles"), **case)

    def test_the_output_folder_must_exist(self):
        with self.assertRaisesRegex(ValueError, "doesn't exist"):
            commands.tiles_export([self.cell], os.path.join(self.folder, "missing", "t.pmtiles"))

    def test_every_container_has_a_label(self):
        for label, container in commands.CONTAINERS.items():
            self.assertEqual(label, commands.container_label(container))


class RenderTests(unittest.TestCase):
    def setUp(self):
        self.folder = support.temp_folder(self)
        self.cell = support.touch(os.path.join(self.folder, "US5MA1BO.000"))

    def test_render(self):
        out = os.path.join(self.folder, "chart.jpg")
        arguments = commands.render([self.cell], out, width=2048, height=1536, extent=(-1.5, 50, -1, 50.5),
                                    quality=85, palette="dusk")
        self.assertEqual(["render", self.cell, "-o", out, "--width", "2048", "--height", "1536",
                          "--bbox", "-1.5,50,-1,50.5", "--quality", "85", "--palette", "dusk"], arguments)

    def test_the_image_needs_a_known_extension(self):
        with self.assertRaisesRegex(ValueError, r"\.png"):
            commands.render([self.cell], os.path.join(self.folder, "chart.bmp"))


class ValidateTests(unittest.TestCase):
    def setUp(self):
        self.folder = support.temp_folder(self)
        self.cell = support.touch(os.path.join(self.folder, "US5MA1BO.000"))

    def test_validate(self):
        self.assertEqual(["validate", self.cell, "--format", "json", "--strict", "--suppress", "S101-R-1.2,S101-*",
                          "--no-updates"],
                         commands.validate(self.cell, strict=True, suppress="S101-R-1.2; S101-*", no_updates=True))

    def test_an_exchange_set_goes_to_verify(self):
        with self.assertRaisesRegex(ValueError, "Verify Exchange Set"):
            commands.validate(self.folder)
        catalogue = support.touch(os.path.join(self.folder, "S101", "CATALOG.XML"))
        self.assertEqual(["validate", catalogue, "--format", "json"], commands.verify_exchange_set(catalogue))
        self.assertEqual(["validate", self.folder, "--format", "json", "--strict"],
                         commands.verify_exchange_set(self.folder, strict=True))

    def test_a_dataset_isnt_an_exchange_set(self):
        with self.assertRaisesRegex(ValueError, "isn't an exchange set"):
            commands.verify_exchange_set(self.cell)


class ConvertTests(unittest.TestCase):
    def setUp(self):
        self.folder = support.temp_folder(self)
        self.cell = os.path.join(self.folder, "US5MA1BO.000")
        self.out = os.path.join(self.folder, "out.000")

    def test_s57_to_s101(self):
        self.assertEqual(["s57", "convert", self.cell, "-o", self.out, "--target", "s101"],
                         commands.convert(self.cell, self.out, "S-57 to S-101"))

    def test_inland_to_s401(self):
        self.assertEqual("s401", commands.convert(self.cell, self.out, "S-57 inland ENC to S-401")[6])

    def test_s101_to_an_s57_exchange_set(self):
        report = os.path.join(self.folder, "report.json")
        self.assertEqual(
            ["s101", "convert", self.cell, "-o", self.folder, "--to", "s57", "--exchange-set", "--cell-name",
             "XX5CELL1", "--report", report, "--no-updates"],
            commands.convert(self.cell, self.folder, "S-101 to S-57", exchange_set=True, cell_name="xx5cell1",
                             report=report, no_updates=True))

    def test_mistakes(self):
        with self.assertRaises(ValueError):
            commands.convert(self.cell, self.out, "S-57 to S-101", exchange_set=True)
        with self.assertRaises(ValueError):
            commands.convert(self.cell, self.out, "S-101 to S-57", cell_name="XX5CELL1")
        with self.assertRaises(ValueError):
            commands.convert(self.cell, self.out, "S-101 to S-57", exchange_set=True, cell_name="TOOLONGNAME")
        with self.assertRaises(ValueError):
            commands.convert(self.cell, self.out, "S-57 to S-63")


if __name__ == "__main__":
    unittest.main()
