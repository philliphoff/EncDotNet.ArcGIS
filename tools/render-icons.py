"""Renders the add-in's ribbon icons from their SVG sources.

The sources are src/EncDotNet.ArcGIS.SoundCharts/Images/src/<Name><size>.svg,
drawn for the Pro Light theme on a 16 px or a 32 px grid (design proposal
docs/design/look-and-feel.md §4.5, §10.7). For each one this script writes:

- Images/<Name><size>.png, the image Config.daml names, at 100%, and
  Images/<Name><size>.scale-150.png and .scale-200.png at 150% and 200%;
- the same files in DarkImages/, with the Light colours swapped for the Dark
  ones below. Pro reads an add-in image from DarkImages/ instead of Images/
  in its Dark theme.

Run it after changing a source, and commit the PNGs:

    python -m pip install cairosvg
    python tools/render-icons.py
    python tools/render-icons.py --sheet icons.png   # also a contact sheet

The output is the same for the same sources and cairosvg version.
"""

import argparse
import base64
import os
import pathlib
import re
import sys

import cairosvg

ROOT = pathlib.Path(__file__).resolve().parent.parent
PROJECT = ROOT / "src" / "EncDotNet.ArcGIS.SoundCharts"
SOURCES = PROJECT / "Images" / "src"

SCALES = {"": 1.0, ".scale-150": 1.5, ".scale-200": 2.0}

# Light colour in the sources -> the Dark theme's colour.
DARK = {
    "#4D4D4D": "#DCDCDC",  # ink
    "#FEFEFE": "#363636",  # paper: fills inside an outline
    "#D6EAF7": "#1F4566",  # chart water
    "#007AC2": "#48A9E8",  # accent
    "#35AC46": "#3FAF50",  # add and check badges
    "#C98A00": "#F5D27A",  # sun outline
    "#C0661F": "#F0A870",  # dusk outline
    "#AAB6E3": "#5A6CB0",  # moon fill
    "#4A5FA8": "#B4C0F0",  # moon outline
}


def themed(svg, theme):
    if theme == "Images":
        return svg
    return re.sub("|".join(DARK), lambda m: DARK[m.group(0)], svg)


def render(svg, size):
    return cairosvg.svg2png(bytestring=svg.encode("utf-8"), output_width=size, output_height=size)


def contact_sheet(rendered, path):
    """Writes every rendering on a Pro Light and a Pro Dark background."""
    names = sorted({name for (name, _, _, _) in rendered})
    columns = [(theme, size, scale) for theme in ("Images", "DarkImages")
               for size in (16, 32) for scale in SCALES]
    cell, label = 72, 190
    width = label + cell * len(columns)
    height = 40 + cell * len(names)
    parts = [f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" '
             f'width="{width}" height="{height}">',
             f'<rect width="{width}" height="{height}" fill="#E8E8E8"/>']
    for i, (theme, size, scale) in enumerate(columns):
        x = label + i * cell
        background = "#FAFAFA" if theme == "Images" else "#2A2A2A"
        parts.append(f'<rect x="{x}" y="0" width="{cell}" height="{height}" fill="{background}"/>')
        ink = "#333" if theme == "Images" else "#DDD"
        parts.append(f'<text x="{x + 4}" y="16" font-family="sans-serif" font-size="10" fill="{ink}">'
                     f'{size}{scale or ".100"}</text>')
        parts.append(f'<text x="{x + 4}" y="30" font-family="sans-serif" font-size="10" fill="{ink}">'
                     f'{"light" if theme == "Images" else "dark"}</text>')
    for row, name in enumerate(names):
        y = 40 + row * cell
        parts.append(f'<text x="8" y="{y + cell / 2}" font-family="sans-serif" font-size="12" fill="#222">{name}</text>')
        for i, key in enumerate(columns):
            png = rendered[(name,) + key]
            px = round(key[1] * SCALES[key[2]])
            x = label + i * cell + (cell - px) // 2
            data = base64.b64encode(png).decode("ascii")
            parts.append(f'<image x="{x}" y="{y + (cell - px) // 2}" width="{px}" height="{px}" '
                         f'image-rendering="optimizeSpeed" xlink:href="data:image/png;base64,{data}"/>')
    parts.append("</svg>")
    # Twice the size, so a pixel of each icon is easy to see.
    cairosvg.svg2png(bytestring="".join(parts).encode("utf-8"), write_to=str(path), scale=2)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--sheet", type=pathlib.Path, help="also write a contact sheet PNG here")
    args = parser.parse_args()

    sources = sorted(SOURCES.glob("*.svg"))
    if not sources:
        sys.exit(f"No SVG sources in {SOURCES}.")

    rendered = {}
    for source in sources:
        match = re.fullmatch(r"(.+?)(16|32)", source.stem)
        if match is None:
            sys.exit(f"{source.name}: the name must end in 16 or 32, the size it's drawn for.")
        name, size = match.group(1), int(match.group(2))
        svg = source.read_text(encoding="utf-8")
        for theme in ("Images", "DarkImages"):
            folder = PROJECT / theme
            folder.mkdir(exist_ok=True)
            for suffix, scale in SCALES.items():
                png = render(themed(svg, theme), round(size * scale))
                (folder / f"{name}{size}{suffix}.png").write_bytes(png)
                rendered[(name, theme, size, suffix)] = png

    print(f"Rendered {len(sources)} sources into {len(rendered)} PNGs.")
    if args.sheet:
        contact_sheet(rendered, args.sheet)
        print(f"Wrote {args.sheet}.")


if __name__ == "__main__":
    main()
