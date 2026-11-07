"""Read-only Unity project structure checks; does not claim C# compilation."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
assets = root / "Assets"
errors = []
guids = {}
for path in assets.rglob("*"):
    if path.suffix == ".meta":
        match = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(), re.MULTILINE)
        if not match:
            errors.append(f"Missing GUID: {path.relative_to(root)}")
        elif match[1] in guids:
            errors.append(f"Duplicate GUID: {path.relative_to(root)}")
        else:
            guids[match[1]] = path
    elif not Path(str(path) + ".meta").exists():
        errors.append(f"Missing metadata: {path.relative_to(root)}")

assemblies = {}
for path in assets.rglob("*.asmdef"):
    value = json.loads(path.read_text())
    assemblies[value["name"]] = value
for value in assemblies.values():
    for reference in value.get("references", []):
        if reference not in assemblies:
            errors.append(f"Unresolved assembly reference: {reference}")
for path in assets.rglob("*.unity"):
    for guid in re.findall(r"m_Script: \{fileID: \d+, guid: ([0-9a-f]{32})", path.read_text()):
        if guid not in guids:
            errors.append(f"Unresolved scene script: {guid}")

version = (root / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split(": ")[1]
workflow = (root / ".github/workflows/unity-tests.yml").read_text()
if f"unityVersion: {version}" not in workflow:
    errors.append("Editor version and CI version differ")
manifest = json.loads((root / "Packages/manifest.json").read_text())["dependencies"]
lock = json.loads((root / "Packages/packages-lock.json").read_text())["dependencies"]
for name, version in manifest.items():
    if name not in lock or lock[name]["version"] != version:
        errors.append(f"Package lock mismatch: {name}")
if errors:
    raise SystemExit("\n".join(errors))
print(f"PASS: {len(guids)} unique asset GUIDs, {len(assemblies)} assemblies, scene script references, package lock and CI editor pin.")
