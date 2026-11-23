# Hurricane showcase stills

Stills rendered for the hurricane-at-a-port showcase pull request, from
`s100 tiles serve` (0.27.1-main.20261113, c62d2eb) over the data that
`tools/get-hurricane-showcase.ps1` downloads. Each landfall frame is WMTS
tiles from a Chart server (the ENCs) with a Weather server's tiles (advisory
18 and GFS-Wave) on top, at the map time in its caption. Not for navigation.

`grib-fill-render-vs-tiles-z6.png` compares `s100 render` (left) with
`s100 tiles serve` (right) for the same bounds and GFS-Wave file.
