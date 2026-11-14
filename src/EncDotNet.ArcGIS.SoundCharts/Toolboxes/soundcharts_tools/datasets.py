"""Finds the datasets in a tool's inputs and reads what each one is.

The add-in reads datasets in-process with EncDotNet.S100; the toolbox asks
``s100 info``, which prints the product and edition. ``s100 info`` has no
JSON output yet, so its table is read leniently: only the ``Specification``
and ``Edition`` rows, which are short enough never to wrap.
"""

import os
import re
from concurrent.futures import ThreadPoolExecutor, as_completed

from . import layerfile, runner

DATASET_EXTENSIONS = (".000", ".h5", ".hdf5", ".gml")
CATALOGUES = ("CATALOG.XML", "CATALOG.031")

_SPECIFICATION = re.compile(r"^\W*Specification\W+(S-\d{2,3})\b", re.MULTILINE)
_EDITION = re.compile(r"^\W*Edition\W+(\d+(?:\.\d+)*)\b", re.MULTILINE)


class Input:
    """One thing the user gave: a dataset, an exchange set or a folder, with its dataset files."""

    def __init__(self, path, name, files):
        self.path = path
        self.name = name
        self.files = files


def is_update_file(path):
    """Whether a file is an S-57 or S-101 update (``.001`` to ``.999``), which s100 folds into its base cell."""
    extension = os.path.splitext(path)[1]
    return len(extension) == 4 and extension[1:].isdigit() and extension != ".000"


def is_dataset_file(path):
    """Whether a file looks like a dataset s100 can read: ``.000``, ``.h5`` or ``.gml``."""
    return os.path.splitext(path)[1].lower() in DATASET_EXTENSIONS


def is_exchange_set_catalogue(path):
    """Whether a file is an exchange set's ``CATALOG.XML`` or ``CATALOG.031``."""
    return os.path.basename(path).upper() in CATALOGUES


def producer_from_name(path):
    """Gets the producer code in a dataset's file name, as the add-in does.

    The two letters after an S-1xx file's three-digit product number
    (``101AA00DS0003`` gives ``AA``), or the first two letters of an S-57
    cell name (``US5MA1BO`` gives ``US``); ``None`` otherwise.
    """
    name = os.path.splitext(os.path.basename(path))[0]
    if len(name) >= 5 and name[:3].isdigit() and name[:3].isascii() and _upper(name[3:5]):
        return name[3:5]
    if len(name) >= 8 and _upper(name[:2]) and name[2].isdigit() and name[2].isascii():
        return name[:2]
    return None


def _upper(text):
    return all("A" <= c <= "Z" for c in text)


def _dataset_files(folder):
    found = []
    for root, folders, files in os.walk(folder):
        folders.sort()
        for name in sorted(files):
            path = os.path.join(root, name)
            if is_dataset_file(path):
                found.append(os.path.abspath(path))
    return found


def expand(paths):
    """Finds the dataset files in each input.

    A dataset file is itself; a ``CATALOG.XML`` or ``CATALOG.031`` stands for
    its folder; a folder is searched recursively. Update files are left to
    s100, which applies them to their base cell.

    :returns: ``(inputs, warnings)``: ``Input`` objects, and messages for inputs that were skipped.
    """
    inputs, warnings = [], []
    for path in paths:
        path = os.path.abspath(path)
        if os.path.isdir(path):
            files = _dataset_files(path)
            name = os.path.basename(path.rstrip("\\/")) or path
        elif os.path.isfile(path) and is_exchange_set_catalogue(path):
            folder = os.path.dirname(path)
            files = _dataset_files(folder)
            name = os.path.basename(folder) or folder
        elif os.path.isfile(path) and path.lower().endswith(".zip"):
            warnings.append("{}: extract the .zip first, then add its folder.".format(path))
            continue
        elif os.path.isfile(path):
            if is_update_file(path):
                warnings.append("{}: an update file; add its base cell (.000) instead.".format(path))
                continue
            files = [path]
            name = os.path.splitext(os.path.basename(path))[0]
        else:
            warnings.append("{}: not found.".format(path))
            continue
        if not files:
            warnings.append("{}: no datasets found.".format(path))
            continue
        inputs.append(Input(path, name, files))
    return inputs, warnings


def parse_info(text):
    """Reads ``(product, edition)`` from ``s100 info`` output; either may be ``None``."""
    product = _SPECIFICATION.search(text or "")
    edition = _EDITION.search(text or "")
    return (product.group(1) if product else None, edition.group(1) if edition else None)


def inspect(program, path, cwd=None):
    """Asks ``s100 info`` what a dataset is.

    :returns: ``(ChartDataset or None, warning or None)``.
    """
    result = runner.run(program, ["info", path], cwd=cwd)
    product, edition = parse_info(result.output)
    if result.exit_code != 0 or product is None:
        detail = next((line.strip() for line in result.stderr + result.stdout
                       if runner.severity(line) == runner.ERROR), None)
        reason = detail or runner.exit_message(result.exit_code)
        return None, "{}: skipped, s100 can't read it. {}".format(path, reason)
    return layerfile.ChartDataset(path, product, edition, producer=producer_from_name(path)), None


def inspect_all(program, inputs, on_done=None, workers=4, cwd=None):
    """Inspects every input's files, a few at a time.

    :param on_done: Called with the number of files inspected so far.
    :returns: ``(datasets, warnings, input names that gave datasets)``.
    """
    files = [(item.name, path) for item in inputs for path in item.files]
    results = [None] * len(files)

    # on_done runs on this thread: arcpy's progressor isn't for worker threads.
    with ThreadPoolExecutor(max_workers=max(1, workers)) as pool:
        futures = {pool.submit(inspect, program, path, cwd): index for index, (_, path) in enumerate(files)}
        for done, future in enumerate(as_completed(futures), start=1):
            results[futures[future]] = future.result()
            if on_done is not None:
                on_done(done)

    datasets, warnings, names = [], [], []
    for (name, _), (dataset, warning) in zip(files, results):
        if warning:
            warnings.append(warning)
        if dataset is not None:
            datasets.append(dataset)
            if name not in names:
                names.append(name)
    return datasets, warnings, names
