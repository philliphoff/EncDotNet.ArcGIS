"""Just enough of arcpy to load SoundChartsTools.pyt and run its tools without ArcGIS Pro.

Records messages, progress and output parameters. ``arcpy.mp`` and extents
aren't faked: tests that need a map run in Pro (docs/toolbox.md lists the
manual checks).
"""

import types


class ExecuteError(Exception):
    pass


class _Filter:
    def __init__(self):
        self.type = None
        self.list = []


class Parameter:
    def __init__(self, displayName, name, datatype, parameterType="Required", direction="Input", multiValue=False,
                 category=None):
        self.displayName = displayName
        self.name = name
        self.datatype = datatype
        self.parameterType = parameterType
        self.direction = direction
        self.multiValue = multiValue
        self.category = category
        self.filter = _Filter()
        self.value = None
        self.enabled = True
        self.altered = False
        self.error = None

    @property
    def valueAsText(self):
        if self.value is None:
            return None
        if isinstance(self.value, (list, tuple)):
            return ";".join(str(v) for v in self.value)
        return str(self.value)

    def setErrorMessage(self, message):
        self.error = message


def install(module_name="arcpy"):
    """Makes a fresh fake arcpy module; the test puts it in sys.modules."""
    arcpy = types.ModuleType(module_name)
    arcpy.messages = []
    arcpy.progress = []
    arcpy.outputs = {}
    arcpy.ExecuteError = ExecuteError
    arcpy.Parameter = Parameter
    arcpy.env = types.SimpleNamespace(isCancelled=False, scratchFolder=None)
    arcpy.AddMessage = lambda text: arcpy.messages.append(("info", text))
    arcpy.AddWarning = lambda text: arcpy.messages.append(("warning", text))
    arcpy.AddError = lambda text: arcpy.messages.append(("error", text))
    arcpy.SetProgressor = lambda *args: arcpy.progress.append(("set",) + args)
    arcpy.SetProgressorPosition = lambda position=None: arcpy.progress.append(("position", position))
    arcpy.ResetProgressor = lambda: arcpy.progress.append(("reset",))
    arcpy.SetParameter = lambda index, value: arcpy.outputs.__setitem__(index, value)
    arcpy.SetParameterAsText = lambda index, value: arcpy.outputs.__setitem__(index, value)

    def no_project(name):
        raise OSError("No project outside ArcGIS Pro.")

    arcpy.mp = types.SimpleNamespace(ArcGISProject=no_project, LayerFile=lambda path: path)
    return arcpy
