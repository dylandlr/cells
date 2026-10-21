#!/usr/bin/env python3
"""Validate and summarize actual Unity membrane assay JSON, no third-party dependencies."""
import csv
import json
import math
import pathlib
import sys

source = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else pathlib.Path("artifacts/membrane-assay.json")
output = pathlib.Path(sys.argv[2]) if len(sys.argv) > 2 else source.parent
output.mkdir(parents=True, exist_ok=True)
report = json.loads(source.read_text(encoding="utf-8"))
samples = report.get("samples")
if not isinstance(samples, list) or len(samples) < 2:
    raise SystemExit("Invalid membrane assay: no time-series data")
fields = ["time", "actuation", "equatorWidth", "shoulderWidth", "ratio", "area", "maxStress", "valid", "narrowed"]
previous = -math.inf
invalid = 0
narrowed = 0
for index, sample in enumerate(samples):
    if not all(field in sample for field in fields):
        raise SystemExit(f"Missing sample field at index {index}")
    for field in fields[:7]:
        value = sample[field]
        if not isinstance(value, (int, float)) or not math.isfinite(value):
            raise SystemExit(f"Nonfinite {field} at sample {index}")
    if sample["time"] <= previous:
        raise SystemExit("Non-increasing sample times")
    previous = sample["time"]
    invalid += not bool(sample["valid"])
    narrowed += bool(sample["narrowed"])
if invalid != report["invalidSamples"] or narrowed != report["narrowedSamples"]:
    raise SystemExit("Report sample counters disagree with observations")
if report["status"] != ("invalid-geometry" if invalid else "narrowed" if narrowed else "not-narrowed"):
    raise SystemExit("Reported status disagrees with observations")
with (output / "membrane-trace.csv").open("w", newline="", encoding="utf-8") as f:
    writer = csv.DictWriter(f, fieldnames=fields)
    writer.writeheader()
    writer.writerows({k: s[k] for k in fields} for s in samples)
summary = [
    "# cells — membrane constriction assay",
    "",
    "These are measured outputs from the Unity 2D soft-body assay, not a scission event.",
    "",
    f"- Status: **{report['status']}**",
    f"- Observations: **{len(samples)}**",
    f"- First ratio: **{samples[0]['ratio']:.5f}**",
    f"- Minimum valid ratio: **{min((s['ratio'] for s in samples if s['valid']), default=float('nan')):.5f}**",
    f"- Last ratio: **{samples[-1]['ratio']:.5f}**",
    f"- Invalid geometry observations: **{invalid}**",
    f"- Narrowed observations: **{narrowed}**",
    "",
    "A narrowed result is a geometrical diagnostic, not demonstrated membrane scission.",
    "No two-loop topology transition is simulated in this assay.",
]
(output / "membrane-summary.md").write_text("\n".join(summary) + "\n", encoding="utf-8")
print("\n".join(summary))
if invalid:
    raise SystemExit("Invalid membrane geometry occurred; check samples and solver")
