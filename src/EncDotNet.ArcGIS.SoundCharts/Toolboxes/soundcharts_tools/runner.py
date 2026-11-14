"""Runs ``s100`` and turns what it prints into messages and progress.

``s100`` writes progress and warnings to standard error and results to
standard output. The runner reads both as they come, hands each line to a
callback with its severity, works out the share of the work done from
``tiles export``'s per-zoom lines, and stops the program when the tool is
cancelled.
"""

import os
import queue
import re
import subprocess
import threading

INFO = "info"
WARNING = "warning"
ERROR = "error"

# From the exit codes in EncDotNet.S100's docs/cli.md.
EXIT_CODES = {
    1: "s100 stopped with an unexpected error.",
    2: "s100 couldn't detect the product, or couldn't read or resolve the inputs.",
    3: "The product specification doesn't support headless rendering.",
    4: "The dataset is recognised, but its structure or encoding isn't supported.",
    5: "The dataset doesn't conform to its product specification.",
    6: "Validation found failing findings, or failed signature or checksum checks.",
}

ENVIRONMENT = {
    # No update notices in the messages, and no network access to look for one.
    "S100_NO_UPDATE_CHECK": "1",
    "NO_COLOR": "1",
    # The web host reload watcher crashed tiles serve (A2); harmless to set for every command.
    "DOTNET_hostBuilder__reloadConfigOnChange": "false",
}

_ERROR_LINE = re.compile(r"^\s*(error|fatal|unhandled exception)\b", re.IGNORECASE)
_WARNING_LINE = re.compile(r"^\s*(warning|warn)\b|\bskipp(ed|ing)\b", re.IGNORECASE)
# A count with any thousands separator but a plain space, so "zoom 13-14, 26 tile(s)" reads 26.
_COUNT = r"(?<![\d,.])(\d[\d,.  ]*)"
_TILING = re.compile(r"^Tiling .*?" + _COUNT + r"\s*tile\(s\)", re.IGNORECASE)
_ZOOM_DONE = re.compile(r"^zoom\s+\d+:\s*" + _COUNT + r"\s*tile\(s\)\s+written", re.IGNORECASE)


class Result:
    """What a run of ``s100`` returned."""

    def __init__(self, arguments, exit_code, stdout, stderr, cancelled=False):
        self.arguments = arguments
        self.exit_code = exit_code
        self.stdout = stdout
        self.stderr = stderr
        self.cancelled = cancelled

    @property
    def succeeded(self):
        """Whether ``s100`` exited with code 0 and wasn't cancelled."""
        return self.exit_code == 0 and not self.cancelled

    @property
    def output(self):
        """Standard output as one string."""
        return "\n".join(self.stdout)


def severity(line):
    """Gets a line's severity: ``error``, ``warning``, ``info``, or ``None`` for a blank line."""
    if not line.strip():
        return None
    if _ERROR_LINE.search(line):
        return ERROR
    if _WARNING_LINE.search(line):
        return WARNING
    return INFO


def exit_message(exit_code):
    """Gets what an exit code means, for the tool's error message."""
    meaning = EXIT_CODES.get(exit_code, "s100 rejected the arguments or the inputs.")
    return "{} (exit code {})".format(meaning, exit_code)


def _count(text):
    digits = re.sub(r"\D", "", text)
    return int(digits) if digits else None


class TileProgress:
    """Follows ``tiles export``: the total from "Tiling … N tile(s)", then each "zoom z: n tile(s) written"."""

    def __init__(self):
        self.total = None
        self.done = 0

    def feed(self, line):
        """Reads a line; returns the percentage done when it changed, else ``None``."""
        match = _TILING.search(line)
        if match:
            self.total = _count(match.group(1))
            self.done = 0
            return 0 if self.total else None
        match = _ZOOM_DONE.search(line)
        if match and self.total:
            self.done += _count(match.group(1)) or 0
            return max(0, min(100, int(self.done * 100 / self.total)))
        return None


def command_line(program, arguments):
    """Gets the command line as the log shows it."""
    return subprocess.list2cmdline([program] + list(arguments))


def run(program, arguments, on_line=None, is_cancelled=None, cwd=None, environ=None):
    """Runs ``s100`` and waits for it.

    :param program: The ``s100`` path.
    :param arguments: The arguments after the program.
    :param on_line: Called with ``(stream, line, severity)`` for each line, ``stream`` being ``stdout`` or ``stderr``.
    :param is_cancelled: Returns ``True`` to stop the program.
    :param cwd: The working folder; a long path (A2: an 8.3 path crashed the server).
    :param environ: The base environment; ``os.environ`` when ``None``.
    :returns: A ``Result``.
    """
    env = dict(os.environ if environ is None else environ)
    env.update(ENVIRONMENT)
    process = subprocess.Popen(
        [program] + list(arguments),
        stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        text=True, encoding="utf-8", errors="replace", cwd=cwd, env=env,
        creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))

    lines = queue.Queue()

    def pump(stream, name):
        for raw in stream:
            lines.put((name, raw.rstrip("\r\n")))
        lines.put((name, None))

    readers = [threading.Thread(target=pump, args=(process.stdout, "stdout"), daemon=True),
               threading.Thread(target=pump, args=(process.stderr, "stderr"), daemon=True)]
    for reader in readers:
        reader.start()

    stdout, stderr = [], []
    open_streams = 2
    cancelled = False
    while open_streams:
        try:
            name, line = lines.get(timeout=0.25)
        except queue.Empty:
            if not cancelled and is_cancelled is not None and is_cancelled():
                cancelled = True
                process.kill()
            continue
        if line is None:
            open_streams -= 1
            continue
        (stdout if name == "stdout" else stderr).append(line)
        if on_line is not None:
            on_line(name, line, severity(line))

    exit_code = process.wait()
    for reader in readers:
        reader.join(timeout=5)
    process.stdout.close()
    process.stderr.close()
    return Result(list(arguments), exit_code, stdout, stderr, cancelled)
