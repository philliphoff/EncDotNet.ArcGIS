"""The arcpy-free half of the SoundCharts Tools toolbox (SoundChartsTools.pyt).

Everything here runs without ArcGIS Pro, so it's tested on CI:

- ``locate``: finds the ``s100`` program the add-in bundles (design brief D21).
- ``runner``: runs ``s100`` and turns its output into messages and progress.
- ``commands``: the ``s100`` arguments for each tool.
- ``datasets``: finds datasets in the inputs and reads what each one is.
- ``layerfile``: writes a SoundCharts layer document (``.lyrx``), the same
  CIM the add-in writes.
- ``reports``: turns ``s100 validate --format json`` into messages.

The toolbox file only maps tool parameters to these functions and their
results to arcpy messages.
"""
