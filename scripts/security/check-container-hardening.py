#!/usr/bin/env python3
"""Static policy checks for the Athar application container.

This is not a vulnerability scanner. It ensures repository-controlled runtime
hardening primitives remain present and leaves CVE scanning to a dedicated
scanner/registry gate.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOCKERFILE = ROOT / "examples/Athar/Athar.Api/Dockerfile"
COMPOSE = ROOT / "deploy/athar-compose.yml"


def fail(message: str) -> None:
    print(f"container-hardening: {message}", file=sys.stderr)


def main() -> int:
    dockerfile = DOCKERFILE.read_text(encoding="utf-8")
    compose = COMPOSE.read_text(encoding="utf-8")
    errors: list[str] = []

    if not re.search(r"(?mi)^FROM\s+mcr\.microsoft\.com/dotnet/aspnet:[^\s]+\s+AS\s+final\s*$", dockerfile):
        errors.append("final runtime stage must use the ASP.NET runtime image")

    if not re.search(r"(?mi)^USER\s+\S+\s*$", dockerfile):
        errors.append("final image must declare a non-root USER")

    if re.search(r"(?mi)^FROM\s+[^\s]+:latest(?:\s|$)", dockerfile):
        errors.append("Dockerfile must not use an unqualified :latest base tag")

    if "security_opt:" not in compose or "no-new-privileges:true" not in compose:
        errors.append("Athar app Compose service must set no-new-privileges:true")

    if "cap_drop:" not in compose or "- ALL" not in compose:
        errors.append("Athar app Compose service must drop Linux capabilities")

    if "healthcheck:" not in compose:
        errors.append("Athar development Compose topology must define health checks")

    if errors:
        for error in errors:
            fail(error)
        return 1

    print("Container hardening policy check passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
