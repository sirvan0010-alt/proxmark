#!/usr/bin/env python3
"""Controlled model-backed implementation helper.

The helper asks the model for a unified diff only. It never commits, pushes,
merges, flashes hardware, or performs device operations. The surrounding
workflow applies additional scope and git checks before creating a draft PR.
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

SYSTEM = """You are the PM5 IMPLEMENTATION_AGENT. Implement only the final synthesis decision supplied to you.
Return ONLY one unified git diff inside a ```diff fenced block. Do not return prose outside that block.
Use only files explicitly listed by implementation_scope. Do not add dependencies unless the synthesis explicitly allows it.
Do not modify workflows, security gates, firmware flashing logic, charger/FPGA operations, or hardware-control paths unless they are explicitly in scope and the synthesis says they are safe.
Never claim hardware verification. Preserve existing evidence states and read-only boundaries.
If implementation is impossible from the supplied context, return an empty diff block rather than guessing.
"""


def read(path: str) -> str:
    return pathlib.Path(path).read_text(encoding="utf-8")


def call_model(prompt: str) -> dict:
    key = os.environ.get("OPENAI_API_KEY")
    if not key:
        raise RuntimeError("OPENAI_API_KEY is required")
    payload = {"model": MODEL, "instructions": SYSTEM, "input": prompt, "store": False}
    req = urllib.request.Request(API_URL, data=json.dumps(payload).encode(),
                                 headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"}, method="POST")
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
        print("usage: implementation_runner.py SYNTHESIS_JSON CONTEXT_FILE OUTPUT_DIFF", file=sys.stderr)
        return 2
    synthesis_file, context_file, output_file = sys.argv[1:]
    synthesis = json.loads(read(synthesis_file))
    context = read(context_file)
    prompt = "FINAL SYNTHESIS:\n" + json.dumps(synthesis, ensure_ascii=False, indent=2) + "\n\nCURRENT SCOPED SOURCE CONTEXT:\n" + context
    text = output_text(call_model(prompt))
    match = re.search(r"```diff\s*(.*?)```", text, flags=re.DOTALL)
    if not match:
        raise RuntimeError("implementation agent did not return a diff fenced block")
    diff = match.group(1).strip() + "\n"
    pathlib.Path(output_file).write_text(diff, encoding="utf-8")
    print(diff)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
