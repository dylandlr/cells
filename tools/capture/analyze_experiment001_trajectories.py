#!/usr/bin/env python3
"""Experiment 001 descriptive paired trajectory divergence from observed CSV.
Does not estimate counterfactual causal mediation or assert a mechanism.
"""
import csv
import pathlib
import sys

source=pathlib.Path(sys.argv[1])
destination=pathlib.Path(sys.argv[2])
with source.open(newline="",encoding="utf-8") as stream:
    rows=list(csv.DictReader(stream))
expected={"seed","delivery","step","seat","action","reward","foragers_on_selected_patch"}
if not rows or not expected.issubset(rows[0]):
    raise SystemExit("Missing trajectory observations")
grouped={}
for row in rows:
    seed=int(row["seed"])
    step=int(row["step"])
    seat=int(row["seat"])
    condition=row["delivery"]
    if condition not in ("on","off") or seat not in range(4) or step not in range(80):
        raise SystemExit("Unexpected experimental coordinate")
    key=(seed,step,condition,seat)
    if key in grouped:
        raise SystemExit("Duplicate sample "+str(key))
    grouped[key]=row
seeds=sorted({int(r["seed"]) for r in rows})
results=[]
for seed in seeds:
    first=None
    divergent=0
    on_sum=off_sum=0.0
    on_crowding=off_crowding=0.0
    on_forage=off_forage=0
    for step in range(80):
        try:
            on=[grouped[seed,step,"on",seat] for seat in range(4)]
            off=[grouped[seed,step,"off",seat] for seat in range(4)]
        except KeyError as exc:
            raise SystemExit("Missing sample "+str(exc))
        if any(a["action"]!=b["action"] for a,b in zip(on,off)):
            divergent+=1
            if first is None:
                first=step
        for row in on:
            on_sum+=float(row["reward"])
            if int(row["action"])<3:
                on_crowding+=int(row["foragers_on_selected_patch"])
                on_forage+=1
        for row in off:
            off_sum+=float(row["reward"])
            if int(row["action"])<3:
                off_crowding+=int(row["foragers_on_selected_patch"])
                off_forage+=1
    results.append({
        "seed":seed,"first_action_divergence":first if first is not None else -1,
        "divergent_steps":divergent,"on_return":on_sum,"off_return":off_sum,
        "on_minus_off":on_sum-off_sum,
        "on_crowding":on_crowding/on_forage if on_forage else 0.0,
        "off_crowding":off_crowding/off_forage if off_forage else 0.0
    })
destination.mkdir(parents=True,exist_ok=True)
with (destination/"trajectory-diagnostics.csv").open("w",newline="",encoding="utf-8") as stream:
    writer=csv.DictWriter(stream,fieldnames=list(results[0]))
    writer.writeheader()
    writer.writerows(results)
summary=[
    "# Experiment 001: paired trajectory diagnostics","",
    "Descriptive comparison of complete ON/OFF trajectories by world seed; not a randomized mediation analysis.",
    "",
    "| Seed | First differing step | Steps with differing actions | Utility ON−OFF | Crowding ON | Crowding OFF |",
    "|---|---:|---:|---:|---:|---:|"
]
for r in results:
    summary.append(f"| {r['seed']} | {r['first_action_divergence']} | {r['divergent_steps']} | {r['on_minus_off']:.3f} | {r['on_crowding']:.3f} | {r['off_crowding']:.3f} |")
summary+=["","First differing step refers to any seat selecting a different action in the two realized trajectories.",
          "Differences after that point can reflect changed history; do not interpret each subsequent difference as a direct signal response."]
(destination/"trajectory-diagnostics.md").write_text("\n".join(summary)+"\n",encoding="utf-8")
print("\n".join(summary))
