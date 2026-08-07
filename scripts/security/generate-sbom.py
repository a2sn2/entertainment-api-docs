#!/usr/bin/env python3
"""Convert `dotnet list package --format json` output to a compact CycloneDX SBOM.

Only the Python standard library is used. The output is deterministic for a
fixed dependency graph so it can be archived with release evidence.
"""

from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path
from urllib.parse import quote


def load_dotnet_json(path: Path) -> dict:
    raw = path.read_text(encoding="utf-8")
    start = raw.find("{")
    if start < 0:
        raise ValueError("dotnet package output did not contain JSON")
    return json.loads(raw[start:])


def collect_components(document: dict) -> list[dict]:
    packages: dict[tuple[str, str], dict] = {}

    for project in document.get("projects", []):
        for framework in project.get("frameworks", []):
            for collection_name, scope in (
                ("topLevelPackages", "required"),
                ("transitivePackages", "required"),
            ):
                for package in framework.get(collection_name, []):
                    name = package.get("id") or package.get("name")
                    version = (
                        package.get("resolvedVersion")
                        or package.get("resolved")
                        or package.get("version")
                    )
                    if not name or not version:
                        continue

                    key = (str(name), str(version))
                    packages.setdefault(
                        key,
                        {
                            "type": "library",
                            "name": str(name),
                            "version": str(version),
                            "scope": scope,
                            "purl": f"pkg:nuget/{quote(str(name), safe='')}@{quote(str(version), safe='')}",
                        },
                    )

    return [packages[key] for key in sorted(packages)]


def build_bom(document: dict) -> dict:
    components = collect_components(document)
    canonical = json.dumps(components, sort_keys=True, separators=(",", ":")).encode("utf-8")
    fingerprint = hashlib.sha256(canonical).hexdigest()

    return {
        "bomFormat": "CycloneDX",
        "specVersion": "1.5",
        "serialNumber": f"urn:uuid:{fingerprint[0:8]}-{fingerprint[8:12]}-{fingerprint[12:16]}-{fingerprint[16:20]}-{fingerprint[20:32]}",
        "version": 1,
        "metadata": {
            "component": {
                "type": "application",
                "name": "foundationkit-dotnet",
            },
            "properties": [
                {
                    "name": "foundationkit:dependency-graph-sha256",
                    "value": fingerprint,
                }
            ],
        },
        "components": components,
    }


def main() -> int:
    if len(sys.argv) != 3:
        print(
            "Usage: generate-sbom.py <dotnet-packages.json> <bom.cdx.json>",
            file=sys.stderr,
        )
        return 2

    source = Path(sys.argv[1])
    destination = Path(sys.argv[2])
    bom = build_bom(load_dotnet_json(source))

    if not bom["components"]:
        print("No NuGet components were found in the dotnet package output.", file=sys.stderr)
        return 1

    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(
        json.dumps(bom, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"CycloneDX SBOM created with {len(bom['components'])} unique NuGet components: {destination}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
