#!/usr/bin/env python3
"""Validate a Unity timestep sensitivity study without inventing measurements."""
import json
import math
import pathlib
import sys

path = pathlib.Path(sys.argv[1])
output = pathlib.Path(sys.argv[2])
data = json.loads(path.read_text())
trials = data.get("trials", [])
if len(trials) != 3:
    raise SystemExit("Expected three independent membrane convergence trials")
for trial in trials:
    for key in ("timestep", "minimumRatio", "finalRatio", "finalArea", "controlFinalRatio", "controlFinalArea", "actuationEffect"):
        x = trial.get(key)
        if not isinstance(x, (float, int)) or not math.isfinite(x):
            raise SystemExit(f"Invalid or nonfinite convergence measure: {key}")
    if trial["timestep"] <= 0 or trial["finalArea"] <= 0:
        raise SystemExit("Nonpositive timestep or membrane area")
    if not trial.get("controlValid", False):
        raise SystemExit("Unactuated control produced invalid geometry")
    if abs((trial["controlFinalRatio"]-trial["finalRatio"])-trial["actuationEffect"])>1e-4:
        raise SystemExit("Reported actuation effect inconsistent with matched control")
    if trial["invalidSamples"] != 0:
        raise SystemExit("Nonzero invalid geometry samples")
if not data.get("allValid"):
    raise SystemExit("Unity convergence study flagged invalid geometry")
ratio_difference = max(abs(x["finalRatio"] - trials[0]["finalRatio"]) for x in trials)
area_difference = max(
    abs(x["finalArea"]-trials[0]["finalArea"])/trials[0]["finalArea"] for x in trials
)
if abs(ratio_difference-data["maximumFinalRatioDifference"])>1e-4:
    raise SystemExit("Reported ratio convergence disagrees with trials")
if abs(area_difference-data["maximumFinalAreaRelativeDifference"])>1e-4:
    raise SystemExit("Reported area convergence disagrees with trials")
lines = ["# Membrane timestep sensitivity", "",
         "Measured Unity solver values; no scission is simulated.", "",
         "| dt | Status | Actuated ratio | Control ratio | Change vs control | Last area |",
         "|---:|---|---:|---:|---:|---:|"]
for trial in trials:
    lines.append(f"| {trial['timestep']:.3f} | {trial['status']} | {trial['finalRatio']:.5f} | {trial['controlFinalRatio']:.5f} | {trial['actuationEffect']:.5f} | {trial['finalArea']:.5f} |")
lines += ["", f"Maximum ratio change vs dt={trials[0]['timestep']:.3f}: {ratio_difference:.5f}",
          f"Maximum relative area change: {area_difference:.3%}", "",
          "These are numerical convergence diagnostics; do not equate them with biological calibration."]
output.mkdir(parents=True, exist_ok=True)
(output/"membrane-convergence-summary.md").write_text("\n".join(lines)+"\n")
print("\n".join(lines))
