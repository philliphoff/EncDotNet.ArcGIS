"""Finds the ``s100`` program the toolbox runs (design brief D21).

In order:

1. ``SOUNDCHARTS_S100_PATH``, the add-in's override. When it's set, it's
   used or nothing is: a wrong override is an error, not a reason to run a
   different build.
2. The build bundled in the installed add-in, which Pro unpacks to
   ``%LOCALAPPDATA%\\ESRI\\ArcGISPro\\AssemblyCache\\{add-in id}\\server\\{rid}``:
   win-arm64 on Arm64 Windows, where it runs natively although Pro runs as
   x64, then win-x64.
3. A ``server\\{rid}`` folder beside the toolbox or above it, and a source
   checkout's ``artifacts\\server\\{rid}`` (``tools/build-server.ps1``).
4. ``s100`` on ``PATH``.
"""

import ctypes
import os
import shutil
import subprocess
import sys

ADDIN_ID = "{ffae4aca-2edb-461d-8702-45b62936cf11}"
"""The add-in's id, its AssemblyCache folder name (Config.daml)."""

OVERRIDE_VARIABLE = "SOUNDCHARTS_S100_PATH"
"""The environment variable the add-in reads for another ``s100.exe``."""

FILE_NAME = "s100.exe" if sys.platform == "win32" else "s100"

_IMAGE_FILE_MACHINE_ARM64 = 0xAA64


class S100NotFound(Exception):
    """No ``s100`` program was found; the message says where the toolbox looked."""


class S100Program:
    """An ``s100`` program and where it came from."""

    def __init__(self, path, source):
        """Creates the program.

        :param path: The program's full path.
        :param source: Where it was found: ``override``, ``add-in``, ``toolbox``, ``build`` or ``PATH``.
        """
        self.path = path
        self.source = source

    def version(self, timeout=30):
        """Gets ``s100 --version`` without the commit, such as ``0.27.1-main.20261113``, or ``None``."""
        try:
            completed = subprocess.run(
                [self.path, "--version"], capture_output=True, text=True, encoding="utf-8", errors="replace",
                timeout=timeout, env=dict(os.environ, S100_NO_UPDATE_CHECK="1"),
                creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
        except (OSError, subprocess.SubprocessError):
            return None
        return parse_version(completed.stdout)

    def __repr__(self):
        return "S100Program({!r}, {!r})".format(self.path, self.source)


def parse_version(text):
    """Gets the version from ``s100 --version`` output, without the ``+commit`` suffix."""
    for line in (text or "").splitlines():
        line = line.strip()
        if line:
            return line.split("+", 1)[0]
    return None


def native_machine():
    """Gets ``ARM64`` or ``AMD64`` for the operating system, not this (possibly emulated) process."""
    if sys.platform == "win32":
        try:
            kernel32 = ctypes.windll.kernel32
            process_machine = ctypes.c_ushort()
            machine = ctypes.c_ushort()
            if kernel32.IsWow64Process2(kernel32.GetCurrentProcess(), ctypes.byref(process_machine), ctypes.byref(machine)):
                return "ARM64" if machine.value == _IMAGE_FILE_MACHINE_ARM64 else "AMD64"
        except (AttributeError, OSError):
            pass
    machine = os.environ.get("PROCESSOR_ARCHITEW6432") or os.environ.get("PROCESSOR_ARCHITECTURE") or ""
    return "ARM64" if machine.upper() == "ARM64" else "AMD64"


def runtime_identifiers(machine):
    """Gets the bundled builds to try on a machine, preferred first."""
    return ["win-arm64", "win-x64"] if machine == "ARM64" else ["win-x64"]


def candidates(toolbox_directory, environ, machine):
    """Lists ``(path, source)`` pairs to try, in order, after the override.

    :param toolbox_directory: The folder that holds the ``.pyt``.
    :param environ: The environment variables.
    :param machine: ``ARM64`` or ``AMD64``.
    """
    rids = runtime_identifiers(machine)
    found = []
    local = environ.get("LOCALAPPDATA")
    if local:
        cache = os.path.join(local, "ESRI", "ArcGISPro", "AssemblyCache", ADDIN_ID, "server")
        found += [(os.path.join(cache, rid, FILE_NAME), "add-in") for rid in rids]

    # Beside the toolbox, or above it, for a toolbox unpacked with the add-in.
    folder = os.path.abspath(toolbox_directory)
    for _ in range(3):
        found += [(os.path.join(folder, "server", rid, FILE_NAME), "toolbox") for rid in rids]
        folder = os.path.dirname(folder)

    # A source checkout: src/EncDotNet.ArcGIS.SoundCharts/Toolboxes → artifacts/server.
    folder = os.path.abspath(toolbox_directory)
    for _ in range(5):
        found += [(os.path.join(folder, "artifacts", "server", rid, FILE_NAME), "build") for rid in rids]
        parent = os.path.dirname(folder)
        if parent == folder:
            break
        folder = parent
    return found


def locate(toolbox_directory, environ=None, machine=None, which=shutil.which):
    """Finds the ``s100`` program.

    :param toolbox_directory: The folder that holds the ``.pyt``.
    :param environ: The environment variables; ``os.environ`` when ``None``.
    :param machine: ``ARM64`` or ``AMD64``; the operating system's when ``None``.
    :param which: Finds a program on ``PATH``.
    :raises S100NotFound: No program was found.
    """
    environ = os.environ if environ is None else environ
    override = (environ.get(OVERRIDE_VARIABLE) or "").strip().strip('"')
    if override:
        path = os.path.abspath(os.path.expandvars(override))
        if os.path.isfile(path):
            return S100Program(path, "override")
        raise S100NotFound("{} is set to {}, but there's no file there.".format(OVERRIDE_VARIABLE, path))

    tried = candidates(toolbox_directory, environ, machine or native_machine())
    for path, source in tried:
        if os.path.isfile(path):
            return S100Program(path, source)

    on_path = which("s100")
    if on_path:
        return S100Program(os.path.abspath(on_path), "PATH")

    raise S100NotFound(
        "Couldn't find the s100 program. Install the SoundCharts for ArcGIS add-in, which includes it, "
        "or set {} to the full path of s100.exe. Looked in {}, and on PATH.".format(
            OVERRIDE_VARIABLE, os.path.dirname(os.path.dirname(tried[0][0])) if tried else "the add-in"))
