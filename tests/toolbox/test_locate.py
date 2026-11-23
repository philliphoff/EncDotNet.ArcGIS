"""Finding s100: the override, the add-in's bundle, a build, then PATH."""

import os
import re
import unittest

import support
from soundcharts_tools import locate


class LocateTests(unittest.TestCase):
    def setUp(self):
        self.root = support.temp_folder(self)
        self.local = os.path.join(self.root, "Local")
        self.toolbox = os.path.join(self.local, "ESRI", "ArcGISPro", "Toolboxes", locate.ADDIN_ID)
        os.makedirs(self.toolbox)
        self.environ = {"LOCALAPPDATA": self.local}

    def bundled(self, rid):
        return support.touch(os.path.join(self.local, "ESRI", "ArcGISPro", "AssemblyCache", locate.ADDIN_ID,
                                          "server", rid, locate.FILE_NAME))

    def find(self, machine="AMD64", which=lambda name: None):
        return locate.locate(self.toolbox, self.environ, machine, which)

    def test_the_add_ins_x64_build(self):
        path = self.bundled("win-x64")
        program = self.find()
        self.assertEqual((path, "add-in"), (program.path, program.source))

    def test_arm64_prefers_the_native_build(self):
        self.bundled("win-x64")
        path = self.bundled("win-arm64")
        self.assertEqual(path, self.find("ARM64").path)
        self.assertNotEqual(path, self.find("AMD64").path)

    def test_arm64_falls_back_to_x64(self):
        path = self.bundled("win-x64")
        self.assertEqual(path, self.find("ARM64").path)

    def test_the_override_wins(self):
        self.bundled("win-x64")
        override = support.touch(os.path.join(self.root, "custom", locate.FILE_NAME))
        self.environ[locate.OVERRIDE_VARIABLE] = '"' + override + '"'
        program = self.find()
        self.assertEqual((override, "override"), (program.path, program.source))

    def test_a_missing_override_is_an_error_not_a_fallback(self):
        self.bundled("win-x64")
        self.environ[locate.OVERRIDE_VARIABLE] = os.path.join(self.root, "nothing", locate.FILE_NAME)
        with self.assertRaisesRegex(locate.S100NotFound, "there's no file there"):
            self.find()

    def test_a_source_checkout_build(self):
        toolbox = os.path.join(self.root, "repo", "src", "EncDotNet.ArcGIS.SoundCharts", "Toolboxes")
        os.makedirs(toolbox)
        path = support.touch(os.path.join(self.root, "repo", "artifacts", "server", "win-x64", locate.FILE_NAME))
        program = locate.locate(toolbox, {}, "AMD64", lambda name: None)
        self.assertEqual((path, "build"), (program.path, program.source))

    def test_path_last(self):
        on_path = support.touch(os.path.join(self.root, "bin", locate.FILE_NAME))
        program = self.find(which=lambda name: on_path if name == "s100" else None)
        self.assertEqual((on_path, "PATH"), (program.path, program.source))

    def test_not_found_says_how_to_fix_it(self):
        with self.assertRaises(locate.S100NotFound) as caught:
            self.find()
        self.assertIn("SoundCharts for ArcGIS add-in", str(caught.exception))
        self.assertIn(locate.OVERRIDE_VARIABLE, str(caught.exception))

    def test_the_add_in_id_matches_config_daml(self):
        with open(support.CONFIG, encoding="utf-8") as stream:
            daml = stream.read()
        self.assertRegex(daml, r'<AddInInfo id="' + re.escape(locate.ADDIN_ID) + '"')


class VersionTests(unittest.TestCase):
    def test_drops_the_commit(self):
        self.assertEqual("0.27.1-main.20261113",
                         locate.parse_version("0.27.1-main.20261113+c62d2eb8df13d5280ebb63d0740d50baf90103c4\n"))

    def test_nothing(self):
        self.assertIsNone(locate.parse_version(""))

    def test_native_machine(self):
        self.assertIn(locate.native_machine(), ("ARM64", "AMD64"))


if __name__ == "__main__":
    unittest.main()
