# SoundCharts features: the plugin datasource

This note records how SoundCharts makes S-1xx features into ordinary Pro
feature classes (issue
[#15](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/15), slice A13
of the epic), so selection, attribute tables, joins and geoprocessing work
on them. It covers the problem, how the plugin is packaged, how features
map to tables, and what is left open. The design brief's §3.2, D14 and D16
set the layer names and provenance.

## Problem

The chart layers are pictures from the chart server: Pro can't select,
query or analyze what they show. Pro's own ENC support reads S-57 and
S-101 cells, but not S-124 or S-421, and it isn't SoundCharts. Pro's
extension point for a data format it can't read is the plugin datasource
(`ArcGIS.Core.Data.PluginDatastore`): read-only tables and feature classes
that Pro treats like any other.

Constraints:

- The add-in claims no file extension and no Catalog item type (epic #1).
  Data is opened through SoundCharts commands only.
- Product logic stays in EncDotNet.S100. The plugin adapts the library's
  feature models; it doesn't read files itself.
- Tests run without Pro, so the table model must be free of the Pro API.
- Symbology is Pro's default for now. Native portrayal is A14 (#16).

## Packaging

A plugin datasource is a package of its own. Findings, checked against the
Pro 3.7 SDK (`Esri.ArcGISPro.Extensions30` 3.7.0.1901) and a Pro 3.7.3
install:

- Esri's packaging targets build an `.esriPlugin`, with a `Config.xml`, when
  a project sets `<PackageType>Plugin</PackageType>`. An add-in project
  (`PackageType` `Addin`, the default) builds an `.esriAddinX` with a
  `Config.daml`. One project builds one package, so the plugin is its own
  project: `src/EncDotNet.ArcGIS.SoundCharts.Features`.
- `Config.daml` can't declare a plugin datasource. Pro's
  `bin\AddInSettings.xml` maps the `CLR:PluginDS` language to its native
  plugin factory (`CoreInterop.dll`) and registers every
  `//AddIn/ArcGISPro/PluginDatasources/*` element of a `Config.xml` as a
  workspace factory. The plugin's `Config.xml` declares
  `SoundCharts_FeaturesDatasource`, class `FeaturesPluginDatasource`.
- Esri's documentation says a plugin can't hold UI (no DAML), and that a
  plugin mustn't use the `ArcGIS.Desktop.*` APIs. So the commands that open
  data live in the add-in, which opens the plugin by its id:
  `new PluginDatastore(new PluginDatasourceConnectionPath("SoundCharts_FeaturesDatasource", uri))`.
- `RegisterAddIn.exe` installs an `.esriPlugin` to
  `Documents\ArcGIS\AddIns\ArcGISPro<version>\{plugin id}\`, where
  `<version>` is the `Targets` version in its `Config.xml` (3.7 here). Pro
  scans those versioned folders, from the running release back, and loads
  plugins when a client first opens one. Add-ins go to the unversioned
  `ArcGISPro` folder. `tools/deploy-addin.ps1` installs both the same way.
- Both packages carry their own copies of `EncDotNet.ArcGIS.SoundCharts.Core`
  and the EncDotNet.S100 assemblies. They're built from the same commit, so
  the versions match whichever copy Pro loads first.
- CI uploads both packages in the **addin** artifact.

An alternative was to embed the `.esriPlugin` in the `.esriAddinX` and have
the module install it on first start. It was rejected: it writes outside
the add-in's folder, needs a second Pro start, and would bypass an
administrator's add-in policy.

> [!WARNING]
> Open on the development machine (an Arm64 VM with a redirected Documents
> folder): Pro 3.7.3 didn't find the installed plugin.
> `PluginDatastore` failed with `CLASS_E_CLASSNOTAVAILABLE`, and Pro's
> diagnostic log said "Add-In: SoundCharts_FeaturesDatasource : Add-In ID
> not found." It did the same with the plugin in the `ArcGISPro`,
> `ArcGISPro3.3` and `ArcGISPro3.7` folders under each candidate Documents
> path, with a `Config.xml` copied from Esri's SimplePointPlugin sample,
> and with a project open. Pro never unpacked the plugin to its assembly
> cache. Check on another machine before relying on it (pull request
> checklist).

## Table model

`EncDotNet.SoundCharts.Features` in `EncDotNet.ArcGIS.SoundCharts.Core` is
Pro-free and tested; the plugin project adapts it to the SDK's
`PluginDatasourceTemplate`, `PluginTableTemplate` and
`PluginCursorTemplate`.

### One table per feature type and geometry kind

`DatasetTables` makes one table per feature type, split by geometry kind
where a type has more than one: `DepthArea_Area`, `Landmark_Point` and
`Landmark_Area`, `Sounding_Multipoint`, `Route_Table` for a type without
geometry. The kind is always in the name, so a table keeps its name when a
later edition adds the type's other geometry, and saved layers still find
it.

Per feature type, because a layer per type is what the brief names
(**{feature type} · {dataset}**), what users select and symbolize by, and
what keeps each table's columns to that type's attributes. Rejected: a
table per geometry kind alone, which would carry the union of every type's
attributes (hundreds of mostly empty columns) and need a definition query
to find one type. Pro needs one shape type per feature class, hence the
split.

### Columns

- Every table: `OBJECTID`, `Shape` (feature classes), `FeatureRef` (the
  feature's id in its dataset: the S-101 record id, the GML `gml:id`) and
  `FeatureType` (the code).
- Then the attributes the dataset's features of that type have, in
  first-seen order. The schema comes from the data, not the feature
  catalogue, which lists every attribute a type may have.
- A complex attribute's sub-attributes are columns named by the path of
  codes joined by `_`: `featureName_name`. The alias joins the display names:
  `Feature Name: Name`.
- Repeated values (multi-valued attributes, two instances of a complex
  attribute) are joined with `; `.
- Values are the library's display text: an enumeration's label, not its
  code. A number shown with a unit (`12.5 m`) keeps the bare number.
- A column is an integer, double or date column when every value in the
  dataset is one; otherwise it's text. Text fields are as long as the
  longest value, and at least 16.
- Names are cleaned to letters, digits and `_`, start with a letter, are at
  most 64 characters (a file geodatabase's limit, so **Export Features**
  copies them unchanged) and are unique ignoring case.

Object ids are each feature's position in the library's enumeration, plus
one: unique across a dataset's tables and stable for the same file.

### Associations

- **Information associations.** The library lists the information types a
  feature's information associations reach as attributes with children
  (EncDotNet.S100 0.27.0 and later), so they become columns of the
  feature's row: `NauticalInformation_information_text`.
- **Feature associations** are rows of a `FeatureAssociations` table:
  `FeatureRef`, `FeatureType`, `Association`, `Role` and `TargetRef`. Relate
  or join it to a feature layer on `FeatureRef`, and back on `TargetRef`.

### Geometry

- GML products (S-124, S-421): from the dataset processor's
  `GetFeatureGeometryAt`.
- ISO 8211 products (S-101, S-57): the processor doesn't give geometry, so
  it comes from the library's `S101VectorSource`, over
  `S101Dataset.OpenWithUpdates` with the cell's update files (S-57 through
  `S57ToS101Translator`, as the processor does), matched to the
  processor's features by feature reference.
- Everything is in WGS 84. Polygons are closed with clockwise exteriors and
  counter-clockwise holes, as Esri's model expects. A multi-surface becomes
  one polygon with several exterior rings.

### Searches

- Spatial searches use EncDotNet.S100's spatial index
  (`IVectorSpatialIndex`, an STR-packed R-tree) over each feature's
  bounding rectangle, built once per table. Pro's geometry engine then tests
  the relationship asked for, in WGS 84.
- `ObjectIDs` limit any search. `IsQueryLanguageSupported` is false, so Pro
  evaluates where clauses itself on the rows a search returns.
- Rows are counted natively.
- Pro makes a plugin instance per thread and per opened source; they share
  each parsed dataset through `DatasetTablesCache`, which reads a file
  again when its size or time changes.
- `CanOpen` answers true only for a dataset the plugin has already read, so
  the plugin never offers to open files Pro finds elsewhere.

## Opening data

**Add as Feature Layers** opens datasets, exchange sets and folders as
feature layers: on the SoundCharts tab's **Data** group, and as **Open with
SoundCharts** > **Add as Feature Layers** in Catalog, and the **Add as
Feature Layers** button of the SoundCharts Library pane, beside **Add to
Map**. All three call `AddSoundChartsFeatures.RunAsync(view, paths)`. Exchange sets
and folders expand through A3's `DatasetInspector`. S-101, S-57, S-124 and
S-421 datasets open; others (S-102, S-104, S-111) are reported and skipped.

Per dataset, the add-in adds a **SoundCharts features · {dataset}** group
at the top of the map, outside the chart group (D14). It holds a feature
layer per feature class, named **{feature type} · {dataset}** (or
**{feature type} ({geometry}) · {dataset}** for a split type), areas at the
bottom and points on top, and a standalone table per table. The group
carries a D16 marker, the CIM custom property `SoundCharts` with the role
`features`, the dataset's path and product. A3's chart code reads that as
no marker and leaves the group alone. Layer **Properties** > **Source**
shows the data source as **SoundCharts features**.

Debug builds open the dataset named by `SOUNDCHARTS_DEBUG_FEATURES` through
the plugin 45 seconds after Pro starts, and log each table's counts, to
check the plugin without clicking.

## Open questions

- **Plugin loading** on the development machine (warning above).
- **Soundings' depths.** `Sounding_Multipoint` has no Z: the vector
  source's depths aren't carried yet. A Z-aware multipoint, or one point
  row per sounding with its depth, would make them queryable.
- **Information types as tables.** The processor doesn't enumerate
  information types, so they're only reachable through the features that
  use them. An upstream enumeration would allow one table per information
  type, related by id.
- **Feature catalogue types.** Columns are typed from the data, so the
  same attribute can be an integer in one dataset and text in another. The
  feature catalogue's value types would make schemas stable across
  datasets.
- **S-101 updates.** The vector source applies update files beside the
  base cell; check that the processor's attributes match after an update
  that deletes or renumbers features.
- **Large exchange sets** add one group per dataset. A merged layer per
  feature type across datasets may suit them better.
