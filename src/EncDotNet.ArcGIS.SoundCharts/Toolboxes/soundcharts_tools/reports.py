"""Turns ``s100 validate --format json`` into messages for the Geoprocessing pane."""

import json

from . import runner

MAX_MESSAGES = 200
"""Findings past this many are counted, not listed; the JSON report keeps them all."""


class Report:
    """A validation report: whether it passed, and the messages to show."""

    def __init__(self, valid, messages, data):
        """Creates a report.

        :param valid: Whether the dataset or exchange set passed.
        :param messages: ``(severity, text)`` pairs, ``severity`` from ``runner``.
        :param data: The parsed JSON.
        """
        self.valid = valid
        self.messages = messages
        self.data = data


def parse(output):
    """Parses s100's JSON output; ``None`` when it isn't JSON (s100 failed before reporting)."""
    try:
        data = json.loads(output)
    except (TypeError, ValueError):
        return None
    return data if isinstance(data, dict) else None


def _severity(text):
    text = (text or "").lower()
    if text.startswith("err") or text == "fatal":
        return runner.ERROR
    if text.startswith("warn"):
        return runner.WARNING
    return runner.INFO


def _location(finding):
    parts = []
    if finding.get("relatedFeatureId"):
        parts.append("feature " + str(finding["relatedFeatureId"]))
    point = finding.get("point")
    if isinstance(point, dict) and "latitude" in point and "longitude" in point:
        parts.append("at {:.6f}, {:.6f}".format(point["latitude"], point["longitude"]))
    return " ({})".format(", ".join(parts)) if parts else ""


def _limited(messages, extra):
    if len(messages) <= MAX_MESSAGES:
        return messages
    hidden = len(messages) - MAX_MESSAGES
    return messages[:MAX_MESSAGES] + [(runner.INFO, "{} more {} not listed. {}".format(
        hidden, "finding" if hidden == 1 else "findings", extra))]


def dataset_report(data, name):
    """Gets the messages for a dataset's validation report.

    :param data: The parsed ``validate --format json`` output.
    :param name: The dataset's name, for the summary.
    """
    findings = data.get("findings") or []
    product = data.get("specification") or "unknown product"
    edition = data.get("edition")
    title = "{} ({}{})".format(name, product, " " + edition if edition and edition != "0.0.0" else "")
    valid = bool(data.get("valid"))
    messages = []
    if not data.get("rulesAvailable", True):
        messages.append((runner.WARNING, "{}: no validation rules are available for {}, so nothing was checked."
                         .format(title, product)))
    counts = {runner.ERROR: 0, runner.WARNING: 0, runner.INFO: 0}
    listed = []
    for finding in findings:
        level = _severity(finding.get("severity"))
        counts[level] += 1
        listed.append((level, "{} {}: {}{}".format(
            name, finding.get("ruleId", "?"), finding.get("message", ""), _location(finding))))
    messages += _limited(listed, "Write a report to see them all.")
    suppressed = data.get("suppressedCount") or 0
    messages.append((runner.INFO if valid else runner.ERROR, "{}: {}. {} rule(s) evaluated, {} error(s), {} warning(s), {} information{}.".format(
        title, "passed" if valid else "failed", data.get("rulesEvaluated", 0),
        counts[runner.ERROR], counts[runner.WARNING], counts[runner.INFO],
        ", {} suppressed".format(suppressed) if suppressed else "")))
    return Report(valid, messages, data)


def exchange_set_report(data, name):
    """Gets the messages for an exchange set's signature and checksum report."""
    files = data.get("files") or []
    valid = bool(data.get("valid"))
    listed = []
    for item in files:
        if not item.get("failed"):
            continue
        detail = item.get("detail") or "signature {}, checksum {}".format(
            item.get("signatureOutcome", "?"), item.get("checksumOutcome", "?"))
        listed.append((runner.ERROR, "{}: {}".format(item.get("fileName", "?"), detail)))
    messages = _limited(listed, "Write a report to see them all.")
    notes = []
    if data.get("hasMissingFiles"):
        notes.append("files listed in the catalogue are missing")
    if data.get("hasChecksumMismatches"):
        notes.append("checksums don't match")
    if data.get("isUnsigned"):
        notes.append("it isn't signed")
    if not data.get("integrityVerified", True):
        notes.append("its integrity couldn't be verified")
    if notes:
        messages.append((runner.WARNING, "{}: {}.".format(name, "; ".join(notes))))
    messages.append((runner.INFO if valid else runner.ERROR, "{} ({} exchange set): {}. {} file(s), {} failed.".format(
        name, data.get("product", "unknown"), "passed" if valid else "failed",
        data.get("fileCount", len(files)), data.get("failedCount", len(listed)))))
    return Report(valid, messages, data)


def write(path, data):
    """Writes a report as indented JSON."""
    with open(path, "w", encoding="utf-8", newline="\n") as stream:
        json.dump(data, stream, ensure_ascii=False, indent=2)
        stream.write("\n")
