#!/usr/bin/env python3
"""Validate actual Unity two-solver comparison telemetry; no performance ranking."""
import csv
import json
import math
import pathlib
import sys

source=pathlib.Path(sys.argv[1])
output=pathlib.Path(sys.argv[2])
report=json.loads(source.read_text(encoding="utf-8"))
samples=report.get("samples",[])
if not samples:
    raise SystemExit("Comparison has no measured samples")
if report.get("status") not in ("completed","boundary-rejected","invalid-geometry"):
    raise SystemExit("Unrecognized comparison outcome")
fields=["step","time","legacy_neckRatio","boundary_neckRatio",
        "legacy_absoluteArea","boundary_absoluteArea",
        "legacy_perimeter","boundary_perimeter",
        "legacy_normalizedAreaDrift","boundary_normalizedAreaDrift",
        "legacy_valid","boundary_valid"]
rows=[]
for i,s in enumerate(samples):
    if s.get("step")!=i:
        raise SystemExit("Nonsequential comparison steps")
    time=s.get("time")
    if not isinstance(time,(int,float)) or not math.isfinite(time):
        raise SystemExit("Nonfinite sample time")
    row={"step":i,"time":time}
    for solver in ("legacy","boundary"):
        data=s.get(solver)
        if not isinstance(data,dict):
            raise SystemExit("Missing solver measurement")
        row[solver+"_valid"]=data.get("valid")
        for metric in ("neckRatio","absoluteArea","perimeter","normalizedAreaDrift"):
            v=data.get(metric)
            if not isinstance(v,(float,int)) or not math.isfinite(v):
                raise SystemExit(f"Nonfinite {solver} {metric}")
            row[solver+"_"+metric]=v
    rows.append(row)
if abs(rows[0]["legacy_absoluteArea"]-rows[0]["boundary_absoluteArea"])>1e-4 or abs(
        rows[0]["legacy_neckRatio"]-rows[0]["boundary_neckRatio"])>1e-4:
    raise SystemExit("Solvers do not share baseline geometry")
if report["status"]=="completed" and any(
    not row["legacy_valid"] or not row["boundary_valid"] for row in rows):
    raise SystemExit("Completed comparison includes invalid geometry")
output.mkdir(parents=True,exist_ok=True)
with (output/"membrane-comparison.csv").open("w",encoding="utf-8",newline="") as f:
    writer=csv.DictWriter(f,fieldnames=fields)
    writer.writeheader()
    writer.writerows(rows)
lines=["# Membrane solver comparison","",
       "Actual exported Unity observations, not force-equivalent or biologically calibrated.",
       "",f"Outcome: **{report['status']}**",f"Recorded steps: **{len(rows)}**","",
       "| Solver | Final neck ratio | Final area drift | Valid final geometry |",
       "|---|---:|---:|---|"]
for name in ("legacy","boundary"):
    last=rows[-1]
    lines.append(f"| {name} | {last[name+'_neckRatio']:.5f} | "
                 f"{last[name+'_normalizedAreaDrift']:.5f} | {last[name+'_valid']} |")
lines.extend(["","Different actuator models prevent claims of comparative physical accuracy.",
              "A stopped or rejected run is useful failure evidence, not successful scission."])
(output/"membrane-comparison-summary.md").write_text("\n".join(lines)+"\n",encoding="utf-8")
print("\n".join(lines))
