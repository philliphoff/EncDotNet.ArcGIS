"""The arguments and parsing against the real s100, on EncDotNet.S100's test datasets.

Skips without an s100 the toolbox can find (SOUNDCHARTS_S100_PATH, the
add-in, artifacts/server from tools/build-server.ps1, or PATH) and without
SOUNDCHARTS_S100_TEST_DATASETS. CI has both.
"""

import os
import unittest

import support
from soundcharts_tools import commands, datasets, locate, reports, runner


def _program():
    try:
        return locate.locate(support.TOOLBOX)
    except locate.S100NotFound:
        return None


PROGRAM = _program()


@unittest.skipUnless(PROGRAM and support.upstream("S57/US5MA1BO/US5MA1BO.000"),
                     "Needs s100 and SOUNDCHARTS_S100_TEST_DATASETS.")
class S100Tests(unittest.TestCase):
    def setUp(self):
        self.folder = support.temp_folder(self)
        self.cell = support.upstream("S57/US5MA1BO/US5MA1BO.000")

    def run_s100(self, arguments):
        return runner.run(PROGRAM.path, arguments, cwd=self.folder)

    def test_version(self):
        self.assertRegex(PROGRAM.version(), r"^\d+\.\d+\.\d+")

    def test_info_reads_the_product(self):
        dataset, warning = datasets.inspect(PROGRAM.path, self.cell)
        self.assertIsNone(warning)
        self.assertEqual(("S-57", None, "US"), (dataset.product, dataset.edition, dataset.producer))

    def test_info_reads_s101_and_s104_editions(self):
        for relative, product in (("S101/S-101/DATASET_FILES/101AA00DS0003.000", "S-101"),
                                  ("S104/104US004SC1BO_20251217T12Z.h5", "S-104")):
            path = support.upstream(relative)
            if path is None:
                continue
            with self.subTest(product=product):
                dataset, _ = datasets.inspect(PROGRAM.path, path)
                self.assertEqual(product, dataset.product)
                self.assertRegex(dataset.edition, r"^\d+\.\d+\.\d+$")

    def test_info_skips_what_it_cant_read(self):
        bogus = support.touch(os.path.join(self.folder, "bogus.000"), b"not a cell")
        dataset, warning = datasets.inspect(PROGRAM.path, bogus)
        self.assertIsNone(dataset)
        self.assertIn("skipped", warning)

    def test_validate(self):
        result = self.run_s100(commands.validate(self.cell))
        self.assertEqual(0, result.exit_code)
        report = reports.dataset_report(reports.parse(result.output), "US5MA1BO.000")
        self.assertTrue(report.valid)

    def test_verify_an_s101_exchange_set(self):
        catalogue = support.upstream("S101/CATALOG.XML")
        if catalogue is None:
            self.skipTest("No S-101 exchange set.")
        result = self.run_s100(commands.verify_exchange_set(catalogue))
        self.assertIn(result.exit_code, (0, 6))
        report = reports.exchange_set_report(reports.parse(result.output), "S101")
        self.assertIn("exchange set", report.messages[-1][1])

    def test_export_every_container(self):
        for name in ("chart.pmtiles", "chart.mbtiles", "chart.gpkg", "chart.tif", "xyz"):
            with self.subTest(name=name):
                out = os.path.join(self.folder, name)
                progress = runner.TileProgress()
                percents = []

                def on_line(stream, line, level):
                    percent = progress.feed(line)
                    if percent is not None:
                        percents.append(percent)

                result = runner.run(PROGRAM.path, commands.tiles_export([self.cell], out, max_zoom=13),
                                    on_line=on_line, cwd=self.folder)
                self.assertEqual(0, result.exit_code, result.stderr)
                self.assertTrue(os.path.exists(out))
                self.assertEqual(100, percents[-1])

    def test_render(self):
        out = os.path.join(self.folder, "chart.png")
        result = self.run_s100(commands.render([self.cell], out, width=256, height=256, palette="night"))
        self.assertEqual(0, result.exit_code, result.stderr)
        with open(out, "rb") as stream:
            self.assertEqual(b"\x89PNG", stream.read(4))

    def test_convert_s57_to_s101_and_back(self):
        s101 = os.path.join(self.folder, "101US00MA1BO.000")
        report = os.path.join(self.folder, "report.json")
        result = self.run_s100(commands.convert(self.cell, s101, "S-57 to S-101", report=report))
        self.assertEqual(0, result.exit_code, result.stderr)
        self.assertTrue(os.path.getsize(s101) > 0 and os.path.exists(report))

        back = os.path.join(self.folder, "set")
        result = self.run_s100(commands.convert(s101, back, "S-101 to S-57", exchange_set=True, cell_name="US5MA1BO"))
        self.assertEqual(0, result.exit_code, result.stderr)
        self.assertTrue(os.path.exists(os.path.join(back, "ENC_ROOT", "CATALOG.031")))

    def test_a_bad_argument_fails_with_a_message(self):
        result = self.run_s100(["validate", os.path.join(self.folder, "missing.000"), "--format", "json"])
        self.assertNotEqual(0, result.exit_code)
        self.assertTrue(any(runner.severity(line) == runner.ERROR for line in result.stderr + result.stdout))


if __name__ == "__main__":
    unittest.main()
