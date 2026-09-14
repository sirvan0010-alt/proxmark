#!/usr/bin/env python3
"""Minimal provider-backed AI runner for the PM5 engineering swarm.

The runner is deliberately stateless: GitHub Actions supplies the task/role
context, this process calls the configured model, and the result is written as
an auditable artifact. It never mutates the repository or touches PM5 hardware.
"""
from __future__ import annotations

import json
import os
import pathlib
import sys
import urllib.error
import urllib.request

API_URL = os.getenv("AI_API_URL", "https://api.openai.com/v1/responses")
MODEL = os.getenv("AI_MODEL", "gpt-5.6-luna")

SYSTEM = """You are one specialist in the Proxmark5 Control Center engineering swarm.
Work only from the repository files and context supplied to you. Never invent
physical PM5 facts. Keep PM3 reference, source evidence, simulator evidence,
CI evidence and physical-hardware evidence distinct.

Return a structured engineering message with these sections:
1. finding / proposed function
2. alternative or improvement over the original request
3. evidence/source and exact commit where applicable
4. affected code/docs
5. recommendation and implementation location
6. tests/verification
7. security/safety constraints
8. evidence state
9. questions for other agents
10. next handoff or blocker

A better function is allowed and should be proposed when evidence supports it.
Do not implement code, flash firmware, erase data, write charger/FPGA state, or
claim hardware verification from source/simulator/CI alone.
"""


def read(path: str) -> str:
    return pathlib.Path(path).read_text(encoding="utf-8")


def call_model(instructions: str, input_text: str) -> dict:
    key = os.environ.get("OPENAI_API_KEY")
    if not key:
        raise RuntimeError("OPENAI_API_KEY is required for the real AI runner")
    payload = {
        "model": MODEL,
        "instructions": instructions,
        "input": input_text,
        "store": False,
    }
    req = urllib.request.Request(
        API_URL,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=300) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"AI API HTTP {exc.code}: {body}") from exc


def output_text(response: dict) -> str:
    if isinstance(response.get("output_text"), str):
        return response["output_text"]
    chunks: list[str] = []
    for item in response.get("output", []):
        for content in item.get("content", []):
            if content.get("type") == "output_text" and isinstance(content.get("text"), str):
                chunks.append(content["text"])
    return "\n".join(chunks).strip()


def main() -> int:
    if len(sys.argv) != 5:
        print("usage: runner.py ROLE TASK_FILE CONTEXT_FILE OUTPUT_FILE", file=sys.stderr)
        return 2
    role, task_file, context_file, output_file = sys.argv[1:]
    task = read(task_file)
    context = read(context_file)
    prompt = f"ROLE: {role}\n\nTASK CONTRACT:\n{task}\n\nREPOSITORY CONTEXT:\n{context}"
    response = call_model(SYSTEM + f"\nYour assigned role is {role}.", prompt)
    text = output_text(response)
    if not text:
        raise RuntimeError("AI model returned no output text")
    result = {
        "role": role,
        "model": MODEL,
        "response_id": response.get("id"),
        "task_file": task_file,
        "output": text,
    }
    pathlib.Path(output_file).parent.mkdir(parents=True, exist_ok=True)
    pathlib.Path(output_file).write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(text)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
