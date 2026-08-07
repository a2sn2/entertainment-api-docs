#!/usr/bin/env python3
"""Fail CI on high-confidence committed secret material.

This gate intentionally uses only the Python standard library. It scans
Git-tracked text files and reports file/line/pattern type without printing a
candidate secret value. Generic credential detection is deliberately limited
to quoted literal assignments; expressions and variable-to-variable
assignments are not secrets by themselves and are handled by Trivy/CodeQL and
review instead of generating noisy false positives here.
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

TOKEN_PATTERNS: tuple[tuple[str, re.Pattern[str]], ...] = (
    ("private-key", re.compile(r"-----BEGIN (?:RSA |EC |DSA |OPENSSH )?PRIVATE KEY-----")),
    ("github-token", re.compile(r"\bgh[pousr]_[A-Za-z0-9]{30,}\b")),
    ("github-fine-grained-token", re.compile(r"\bgithub_pat_[A-Za-z0-9_]{50,}\b")),
    ("aws-access-key", re.compile(r"\bAKIA[0-9A-Z]{16}\b")),
    ("slack-token", re.compile(r"\bxox[baprs]-[A-Za-z0-9-]{20,}\b")),
    ("stripe-live-secret", re.compile(r"\bsk_live_[A-Za-z0-9]{16,}\b")),
    ("openai-style-secret", re.compile(r"\bsk-[A-Za-z0-9]{32,}\b")),
)

LITERAL_CREDENTIAL_ASSIGNMENT = re.compile(
    r'''(?ix)
    ["']?
    (?P<key>
        password|pwd|client[_-]?secret|api[_-]?key|access[_-]?token|secret
    )
    ["']?
    \s*[:=]\s*
    (?P<quote>["'])
    (?P<value>[^"'\r\n]+)
    (?P=quote)
    '''
)

DYNAMIC_MARKERS = ("$", "%", "${", "$(", "{{", "}}", "<", ">")
PLACEHOLDER_WORDS = (
    "example",
    "placeholder",
    "changeme",
    "change_me",
    "your_",
    "redacted",
    "masked",
    "required",
    "runtime",
    "environment",
    "secret-manager",
    "secret manager",
    "vault",
    "null",
    "true",
    "false",
)


def tracked_files() -> list[Path]:
    result = subprocess.run(
        ["git", "ls-files", "-z"],
        cwd=ROOT,
        check=True,
        stdout=subprocess.PIPE,
    )
    return [
        ROOT / entry.decode("utf-8")
        for entry in result.stdout.split(b"\0")
        if entry
    ]


def is_probably_binary(data: bytes) -> bool:
    return b"\0" in data[:8192]


def looks_dynamic_or_placeholder(value: str) -> bool:
    normalized = value.strip().lower()
    if not normalized:
        return True
    if any(marker in value for marker in DYNAMIC_MARKERS):
        return True
    if normalized.startswith(("env:", "secret:", "file:")):
        return True
    return any(word in normalized for word in PLACEHOLDER_WORDS)


def scan_line(path: Path, line_number: int, line: str) -> list[tuple[str, int, str]]:
    findings: list[tuple[str, int, str]] = []

    for name, pattern in TOKEN_PATTERNS:
        if pattern.search(line):
            findings.append((path.relative_to(ROOT).as_posix(), line_number, name))

    for match in LITERAL_CREDENTIAL_ASSIGNMENT.finditer(line):
        value = match.group("value")
        if looks_dynamic_or_placeholder(value):
            continue
        if len(value.strip()) < 12:
            continue
        findings.append((
            path.relative_to(ROOT).as_posix(),
            line_number,
            "literal-credential-assignment",
        ))

    return findings


def main() -> int:
    findings: list[tuple[str, int, str]] = []
    scanned = 0

    for path in tracked_files():
        if not path.is_file():
            continue

        data = path.read_bytes()
        if is_probably_binary(data):
            continue

        try:
            text = data.decode("utf-8")
        except UnicodeDecodeError:
            continue

        scanned += 1
        for line_number, line in enumerate(text.splitlines(), start=1):
            findings.extend(scan_line(path, line_number, line))

    if findings:
        print("High-confidence secret scan findings:", file=sys.stderr)
        for path, line_number, finding_type in findings:
            print(f"  {path}:{line_number}: {finding_type}", file=sys.stderr)
        print(
            "Candidate values are intentionally not printed. Remove the secret, rotate it if real, and use runtime secret injection.",
            file=sys.stderr,
        )
        return 1

    print(f"Repository secret scan passed: {scanned} tracked text files inspected.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
