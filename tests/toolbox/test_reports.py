"""Validation reports as messages, from s100 validate --format json as the pinned build writes it."""

import json
import unittest

import support  # noqa: F401
from soundcharts_tools import reports, runner

DATASET = {
    "specification": "S-101", "edition": "1.0.2", "rulesAvailable": True, "rulesEvaluated": 10,
    "rulesWithFindings": 1, "valid": False, "suppressedCount": 2,
    "findings": [
        {"ruleId": "S101-R-1.2", "severity": "Error", "message": "Feature 'UpdateInformation' (RCID 110) carries "
         "attribute 'updateDescription'.", "datasetId": "101AA00DS0003.000", "relatedFeatureId": "1810:110.3",
         "point": {"latitude": -32.6015997, "longitude": 61.847333}},
        {"ruleId": "S101-R-3.2", "severity": "Warning", "message": "Something to look at."},
    ],
}

EXCHANGE_SET = {
    "kind": "exchange-set", "product": "S-100", "fileCount": 2, "integrityVerified": True,
    "hasChecksumMismatches": False, "hasMissingFiles": False, "isUnsigned": False, "valid": False, "failedCount": 1,
    "files": [
        {"fileName": "S-101/DATASET_FILES/101AA0000DS0009.000", "signatureOutcome": "Error",
         "checksumOutcome": "NoChecksum", "detail": "Signature verification error: Provider's public key is invalid.",
         "failed": True},
        {"fileName": "S-101/DATASET_FILES/101AA00DS0003.000", "signatureOutcome": "Valid",
         "checksumOutcome": "Valid", "failed": False},
    ],
}


class DatasetReportTests(unittest.TestCase):
    def test_findings_and_summary(self):
        report = reports.dataset_report(DATASET, "101AA00DS0003.000")
        self.assertFalse(report.valid)
        self.assertEqual(
            (runner.ERROR, "101AA00DS0003.000 S101-R-1.2: Feature 'UpdateInformation' (RCID 110) carries attribute "
                           "'updateDescription'. (feature 1810:110.3, at -32.601600, 61.847333)"),
            report.messages[0])
        self.assertEqual(runner.WARNING, report.messages[1][0])
        self.assertEqual(
            (runner.ERROR, "101AA00DS0003.000 (S-101 1.0.2): failed. 10 rule(s) evaluated, 1 error(s), 1 warning(s), "
                           "0 information, 2 suppressed."),
            report.messages[-1])

    def test_a_pass(self):
        report = reports.dataset_report({"specification": "S-57", "edition": "0.0.0", "rulesAvailable": True,
                                         "rulesEvaluated": 13, "valid": True, "findings": []}, "US5MA1BO.000")
        self.assertTrue(report.valid)
        self.assertEqual([(runner.INFO, "US5MA1BO.000 (S-57): passed. 13 rule(s) evaluated, 0 error(s), "
                                        "0 warning(s), 0 information.")], report.messages)

    def test_no_rules_is_a_warning_not_a_pass(self):
        report = reports.dataset_report({"specification": "S-401", "rulesAvailable": False, "valid": True,
                                         "findings": []}, "inland.000")
        self.assertEqual(runner.WARNING, report.messages[0][0])
        self.assertIn("no validation rules", report.messages[0][1])

    def test_long_reports_are_cut(self):
        data = dict(DATASET, findings=[DATASET["findings"][1]] * (reports.MAX_MESSAGES + 5))
        report = reports.dataset_report(data, "big.000")
        self.assertEqual(reports.MAX_MESSAGES + 2, len(report.messages))
        self.assertIn("5 more findings not listed", report.messages[-2][1])


class ExchangeSetReportTests(unittest.TestCase):
    def test_failed_files_and_summary(self):
        report = reports.exchange_set_report(EXCHANGE_SET, "S101")
        self.assertFalse(report.valid)
        self.assertEqual([
            (runner.ERROR, "S-101/DATASET_FILES/101AA0000DS0009.000: Signature verification error: "
                           "Provider's public key is invalid."),
            (runner.ERROR, "S101 (S-100 exchange set): failed. 2 file(s), 1 failed."),
        ], report.messages)

    def test_notes_unsigned_sets(self):
        data = dict(EXCHANGE_SET, isUnsigned=True, valid=True, failedCount=0, files=[])
        report = reports.exchange_set_report(data, "ENC_ROOT")
        self.assertEqual((runner.WARNING, "ENC_ROOT: it isn't signed."), report.messages[0])


class ParseTests(unittest.TestCase):
    def test_parse(self):
        self.assertEqual(DATASET, reports.parse(json.dumps(DATASET)))
        self.assertIsNone(reports.parse("Error: Path not found"))
        self.assertIsNone(reports.parse("[1, 2]"))


if __name__ == "__main__":
    unittest.main()
