# SoundCharts for ArcGIS documentation

SoundCharts for ArcGIS is an ArcGIS Pro add-in that brings S-100 nautical
chart portrayal from
[EncDotNet.S100](https://github.com/philliphoff/EncDotNet.S100) into ArcGIS
Pro. It adds a **SoundCharts** tab to the ribbon with commands to:

- Add S-57 cells, S-1xx datasets and exchange sets to a map as a SoundCharts
  chart (**Add SoundCharts Data**, **Add Folder**), or as feature layers for
  selection, attribute tables and geoprocessing (**Add as Feature Layers**).
  A `.zip` is read as an exchange set, unless it's a dataset of its own: an
  NHC tropical cyclone advisory's forecast zip (`al092022_5day_020.zip`) is
  added as one dataset, with the wind radii and best track zips beside it.
  A folder holding one storm's zips is added as that storm, one dataset,
  with any charts beside the zips. A folder holding one GRIB2 model run, a
  file per forecast hour (`gfswave.t00z.global.0p25.f000.grib2`, `f003`,
  …), is added as that run, one dataset with the run's hours.
- Browse folders, exchange sets and collection manifests without loading
  them (**Library**). A storm's folder and a GRIB2 run's folder are each one
  dataset there too, named after the folder. In a folder of several storms,
  or one that is also a GRIB2 model run, each storm is its latest forecast
  zip.
- Set the palette, display category and mariner settings of a project's
  SoundCharts layers.
- List the features and coverage values at a point (**Object Information**)
  and check a route against the chart (**Route Check**).
- Export a layout with its SoundCharts layers (**Export SoundCharts
  Chart...**).

It also adds **SoundCharts Tools**, a Python toolbox of geoprocessing tools.

Charts are drawn by `s100`, EncDotNet.S100's command-line tool, which ships
inside the add-in and runs outside Pro's process. SoundCharts layers apply
their own settings and don't change Pro's own ENC layers.

SoundCharts charts are not for navigation.

## Requirements

- ArcGIS Pro 3.7 or later, on Windows. On Arm64 Windows, Pro runs under
  emulation and the add-in runs an Arm64 build of `s100`.

## Get started

- [Install SoundCharts for ArcGIS](install.md): download a release and
  install it into Pro.
- [SoundCharts Tools](toolbox.md): the geoprocessing tools, their parameters
  and how to run them from Python.
- [Show a hurricane crossing a port](hurricane-showcase.md): download a
  storm, wave forecast and harbour charts, and step through a landfall with
  the time slider.
