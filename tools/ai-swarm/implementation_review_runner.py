#!/usr/bin/env python3
"""Adversarial model-backed review of an already-applied implementation diff.

This reviewer is deliberately read-only: it inspects the synthesis and actual
staged diff, then returns a machine-checkable verdict. It never changes repository
files or touches PM5 hardware.
"""
from __future__ import annotations

import json
import os
import pathlib
import re
import sys
import urllib.error
import urllib.request

API_URL = os.getenv("AI_API_URL", "https://api.openai.com/v1/responses")
MODEL = os.getenv("AI_MODEL", "gpt-5.6-luna")

SYSTEM = """You are REVIEW_AGENT performing the final adversarial review of a PM5 AI implementation candidate.
Review the ACTUAL staged unified diff, not an imagined change.
Return ONLY valid JSON with these fields:
{
  "verdict":"APPROVE|REVISE|BLOCK",
  "findings":["..."],
  "unsupported_claims":["..."],
  "scope_issues":["..."],
  "security_issues":["..."],
  "test_gaps":["..."],
  "evidence_issues":["..."],
  "caller_issues":["..."],
  "duplication_issues":["..."],
  "required_changes":["..."]
}

Be adversarial. Reject unsupported PM5 hardware claims, PM3-as-PM5 assumptions,
new destructive operations, weakened gates, scope violations, hidden dependency
changes, duplicated subsystems, and changes whose tests do not cover the contract.
CI/source/simulator evidence must never be treated as physical hardware proof.
APPROVE only when no blocking issue remains and the staged diff is coherent with
the final synthesis. This review does not authorize merge or hardware operation.
"""


def read(path: str) -> str:
    return pathlib.Path(path).read_text(encoding="utf-8")


def call_model(prompt: str) -> dict:
    key = os.environ.get("OPENAI_API_KEY")
    if not key:
        raise RuntimeError("OPENAI_API_KEY is required")
    payload = {"model": MODEL, "instructions": SYSTEM, "input": prompt, "store": False}
    req = urllib.request.Request(
        API_URL,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=300) as response:
            return json.loads(response.read().decode())
    except urllib.error.HTTPError as exc:
        raise RuntimeError(f"AI API HTTP {exc.code}: {exc.read().decode(errors='replace')}") from exc


def output_text(response: dict) -> str:
    if isinstance(response.get("output_text"), str):
        return response["output_text"]
    chunks = []
    for item in response.get("output", []):
        for content in item.get("content", []):
            if content.get("type") == "output_text" and isinstance(content.get("text"), str):
                chunks.append(content["text"])
    return "\n".join(chunks).strip()


def main() -> int:
    if len(sys.argv) != 4:
        print("usage: implementation_review_runner.py SYNTHESIS_JSON DIFF_FILE OUTPUT_JSON", file=sys.stderr)
        return 2
    synthesis_file, diff_file, output_file = sys.argv[1:]
    synthesis = json.loads(read(synthesis_file))
    diff = read(diff_file)
    prompt = (
        "FINAL SYNTHESIS:\n" + json.dumps(synthesis, ensure_ascii=False, indent=2)
        + "\n\nACTUAL STAGED IMPLEMENTATION DIFF:\n" + diff
    )
    text = output_text(call_model(prompt))
    match = re.search(r"\{.*\}", text, flags=re.DOTALL)
    if not match:
        raise RuntimeError("review agent did not return a JSON object")
    data = json.loads(match.group(0))
    if data.get("verdict") not in {"APPROVE", "REVISE", "BLOCK"}:
        raise RuntimeError("review verdict must be APPROVE, REVISE or BLOCK")
    pathlib.Path(output_file).write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(data, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
