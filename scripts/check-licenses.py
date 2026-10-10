#!/usr/bin/env python3
"""Review npm/NuGet package license metadata; fail on unknown or unreviewed licenses.
Container OS packages and legal deployment obligations require separate review (docs/licenses.md).
"""
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
root = Path(__file__).resolve().parent.parent
allowed = {"MIT", "Apache-2.0", "ISC", "BSD-3-Clause", "0BSD", "PostgreSQL", "CC-BY-4.0", "LGPL-3.0-or-later", "MPL-2.0"}
reviewed = json.loads((root / "docs/licenses/reviewed-packages.json").read_text())
rows = []
errors = []
def record(ecosystem, name, version, license, source):
    atoms = set(re.findall(r"[A-Za-z0-9.+-]+", license or "")) - {"AND", "OR"}
    if not atoms or not atoms.issubset(allowed):
        errors.append(f"{ecosystem} {name}@{version}: missing/unreviewed license {license!r}")
    rows.append({"ecosystem":ecosystem,"name":name,"version":version,"license":license,"source":source})
lock = json.loads((root / "frontend/buildingflow-web/package-lock.json").read_text())
for path, package in lock["packages"].items():
    if path:
        record("npm", path.split("node_modules/")[-1], package["version"], package.get("license"), "package-lock.json")
seen = set()
assets = list((root / "backend").glob("**/obj/project.assets.json"))
projects = list((root / "backend").glob("**/*.csproj"))
for project in projects:
    if not (project.parent / "obj/project.assets.json").exists():
        errors.append(f"Restore required: {project.relative_to(root)}")
for asset in assets:
    data = json.loads(asset.read_text())
    for key, package in data["libraries"].items():
        if package["type"] != "package" or key in seen:
            continue
        seen.add(key)
        base = next((Path(folder) / package["path"] for folder in data["packageFolders"] if (Path(folder) / package["path"]).exists()), None)
        if base is None:
            errors.append(f"NuGet package missing: {key}")
            continue
        nodes = {node.tag.split("}")[-1]:node for node in ET.parse(next(base.glob("*.nuspec"))).getroot().iter()}
        node = nodes.get("license")
        license = node.text if node is not None and node.get("type") == "expression" else None
        source = "package .nuspec"
        if license is None and key in reviewed:
            entry = reviewed[key]
            url = nodes.get("licenseUrl")
            evidence = root / entry["evidence"]
            if url is not None and url.text == entry["source"] and evidence.is_file():
                license = entry["license"]
                source = entry["source"]
        name,version = key.rsplit("/",1)
        record("NuGet", name, version, license, source)
if errors:
    print("\n".join(errors), file=sys.stderr)
    sys.exit(1)
rows.sort(key=lambda row:(row["ecosystem"],row["name"],row["version"]))
(root / "docs/licenses/package-inventory.json").write_text(json.dumps(rows,ensure_ascii=False,indent=2)+"\n")
print(f"PASS: {len(rows)} npm/NuGet package versions have identified, reviewed license metadata. Inventory updated.")
