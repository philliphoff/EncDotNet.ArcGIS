"""Finding the datasets in the inputs, and reading s100 info."""

import os
import unittest

import support
from soundcharts_tools import datasets

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

    def test_skipped_inputs_warn(self):
        update = os.path.join(self.root, "S57", "US5MA1BO", "US5MA1BO.001")
        archive = support.touch(os.path.join(self.root, "set.zip"))
        empty = os.path.join(self.root, "empty")
        os.makedirs(empty)
        inputs, warnings = datasets.expand([update, archive, empty, os.path.join(self.root, "missing.000")])
        self.assertEqual([], inputs)
        self.assertEqual(4, len(warnings))
        self.assertIn("base cell", warnings[0])
        self.assertIn("extract the .zip", warnings[1])
        self.assertIn("no datasets", warnings[2])
        self.assertIn("not found", warnings[3])


if __name__ == "__main__":
    unittest.main()
