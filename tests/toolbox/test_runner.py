"""Running s100: lines, severities, tile progress, exit codes and cancelling.

A small Python script stands in for s100.
"""

import sys
import time
import unittest

import support  # noqa: F401  (puts the toolbox on sys.path)
from soundcharts_tools import runner

FAKE = r"""
import os, sys, time
print("[S57] Pipeline produced 1996 drawing instructions", flush=True)
print("Tiling 1 dataset(s) (S-57): zoom 13-14, 1,026 tile(s).", flush=True)
sys.stderr.write("zoom 13: 6 tile(s) written (1,8s)\n"); sys.stderr.flush()
sys.stderr.write("Warning: US5MA1BP.000 skipped\n"); sys.stderr.flush()
sys.stderr.write("zoom 14: 1,020 tile(s) written (1,9s)\n"); sys.stderr.flush()
print("update check " + os.environ.get("S100_NO_UPDATE_CHECK", "on"), flush=True)
sys.exit(int(sys.argv[1]))
"""


class SeverityTests(unittest.TestCase):
    def test_severities(self):
        cases = {
            "Error: Path not found:": runner.ERROR,
            "  error  something": runner.ERROR,
            "Unhandled exception. System.IO": runner.ERROR,
            "Warning: no rules": runner.WARNING,
            "US5MA1BP.000 was skipped: unreadable": runner.WARNING,
            "zoom 13: 6 tile(s) written": runner.INFO,
            "Errors found in 0 files": runner.INFO,
            "   ": None,
        }
        for line, expected in cases.items():
            with self.subTest(line=line):
                self.assertEqual(expected, runner.severity(line))

    def test_exit_messages(self):
        self.assertIn("exit code 2", runner.exit_message(2))
        self.assertIn("couldn't detect the product", runner.exit_message(2))
        self.assertIn("rejected the arguments", runner.exit_message(255))


class TileProgressTests(unittest.TestCase):
    def test_follows_the_zoom_lines(self):
        progress = runner.TileProgress()
        self.assertIsNone(progress.feed("[S57] Pipeline produced 1996 drawing instructions"))
        self.assertEqual(0, progress.feed("Tiling 1 dataset(s) (S-57): zoom 13-14, 26 tile(s)."))
        self.assertEqual(23, progress.feed("zoom 13: 6 tile(s) written (1,8s)"))
        self.assertEqual(100, progress.feed("zoom 14: 20 tile(s) written (1.9s)"))

    def test_reads_thousands_separators(self):
        progress = runner.TileProgress()
        progress.feed("Tiling 4 dataset(s) (S-101, S-102): zoom 9-16, 12,345 tile(s).")
        self.assertEqual(12345, progress.total)
        self.assertEqual(50, progress.feed("zoom 9: 6\u00a0173 tile(s) written (3.2s)"))

    def test_no_total_no_progress(self):
        self.assertIsNone(runner.TileProgress().feed("zoom 13: 6 tile(s) written"))


class RunTests(unittest.TestCase):
    def run_fake(self, exit_code=0):
        lines = []
        result = runner.run(sys.executable, ["-I", "-c", FAKE, str(exit_code)],
                            on_line=lambda stream, line, level: lines.append((stream, line, level)))
        return result, lines

    def test_reads_both_streams_with_severities(self):
        result, lines = self.run_fake()
        self.assertTrue(result.succeeded)
        self.assertIn(("stderr", "Warning: US5MA1BP.000 skipped", runner.WARNING), lines)
        self.assertIn(("stdout", "Tiling 1 dataset(s) (S-57): zoom 13-14, 1,026 tile(s).", runner.INFO), lines)
        self.assertEqual(3, len(result.stderr))

    def test_turns_off_the_update_check(self):
        result, _ = self.run_fake()
        self.assertIn("update check 1", result.stdout)

    def test_returns_the_exit_code(self):
        result, _ = self.run_fake(6)
        self.assertEqual(6, result.exit_code)
        self.assertFalse(result.succeeded)

    def test_cancelling_stops_the_program(self):
        start = time.monotonic()
        result = runner.run(sys.executable, ["-I", "-c", "import time; print('started', flush=True); time.sleep(60)"],
                            is_cancelled=lambda: time.monotonic() - start > 0.5)
        self.assertTrue(result.cancelled)
        self.assertFalse(result.succeeded)
        self.assertLess(time.monotonic() - start, 30)

    def test_command_line_quotes_spaces(self):
        self.assertEqual(r'"C:\Program Files\s100.exe" info "C:\My Charts\a.000"',
                         runner.command_line(r"C:\Program Files\s100.exe", ["info", r"C:\My Charts\a.000"]))


if __name__ == "__main__":
    unittest.main()
