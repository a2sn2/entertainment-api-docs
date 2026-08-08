#!/usr/bin/env python3
"""Static policy checks for repository-owned application containers.

This is not a vulnerability scanner. It ensures repository-controlled runtime
hardening primitives remain present and leaves CVE scanning to dedicated
scanner/registry gates.
"""

from __future__ import annotations

import re
import sys
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


@dataclass(frozen=True)
class ContainerTarget:
    name: str
    dockerfile: Path
    compose: Path


TARGETS = (
    ContainerTarget(
        "Athar",
        ROOT / "examples/Athar/Athar.Api/Dockerfile",
        ROOT / "deploy/athar-compose.yml",
    ),
    ContainerTarget(
        "Madar",
        ROOT / "apps/Madar/Madar.Api/Dockerfile",
        ROOT / "deploy/madar-compose.yml",
    ),
)


def fail(message: str) -> None:
    print(f"container-hardening: {message}", file=sys.stderr)


def validate(target: ContainerTarget) -> list[str]:
    if not target.dockerfile.exists():
        return [f"{target.name}: Dockerfile is missing"]
    if not target.compose.exists():
        return [f"{target.name}: Compose definition is missing"]

    dockerfile = target.dockerfile.read_text(encoding="utf-8")
    compose = target.compose.read_text(encoding="utf-8")
    errors: list[str] = []

    if not re.search(
        r"(?mi)^FROM\s+mcr\.microsoft\.com/dotnet/aspnet:[^\s]+\s+AS\s+final\s*$",
        dockerfile,
    ):
        errors.append(f"{target.name}: final runtime stage must use the ASP.NET runtime image")

    if not re.search(r"(?mi)^USER\s+\S+\s*$", dockerfile):
        errors.append(f"{target.name}: final image must declare a non-root USER")

    if re.search(r"(?mi)^FROM\s+[^\s]+:latest(?:\s|$)", dockerfile):
        errors.append(f"{target.name}: Dockerfile must not use an unqualified :latest base tag")

    if "security_opt:" not in compose or "no-new-privileges:true" not in compose:
        errors.append(f"{target.name}: app Compose service must set no-new-privileges:true")

    if "cap_drop:" not in compose or "- ALL" not in compose:
        errors.append(f"{target.name}: app Compose service must drop Linux capabilities")

    if "healthcheck:" not in compose:
        errors.append(f"{target.name}: development Compose topology must define health checks")

    return errors


def main() -> int:
    errors = [error for target in TARGETS for error in validate(target)]
    if errors:
        for error in errors:
            fail(error)
        return 1

    print(f"Container hardening policy check passed for {len(TARGETS)} application targets.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
