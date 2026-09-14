#!/usr/bin/env python3
"""Fail-closed policy gate for PM5 AI-generated implementation proposals."""
from __future__ import annotations
import json
import sys
from pathlib import Path

REQUIRED_APPROVALS = {
    "architecture": "ARCHITECT_AGENT",
    "evidence": "EVIDENCE_AGENT",
    "security": "SECURITY_AGENT",
    "tests": "TEST_AGENT",
    "review": "REVIEW_AGENT",
}
ALLOWED_DECISIONS = {"IMPLEMENT", "BLOCK", "RESEARCH", "HARDWARE_VERIFY"}


def fail(message: str) -> int:
    print(f"IMPLEMENTATION_GATE=BLOCKED\nREASON={message}")
    return 1


def load_decision(path: Path) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    # runner.py wraps model output in {role, model, response_id, output}.
    if isinstance(data.get("output"), str):
        text = data["output"].strip()
        candidates = [text]
        if "```json" in text:
            candidates.append(text.split("```json", 1)[1].split("```", 1)[0].strip())
        for candidate in candidates:
            try:
                parsed = json.loads(candidate)
                if isinstance(parsed, dict):
                    return parsed
            except json.JSONDecodeError:
                pass
    return data


def main() -> int:
    if len(sys.argv) != 2:
        return fail("usage: implementation_gate.py <synthesis.json>")
    path = Path(sys.argv[1])
    try:
        data = load_decision(path)
    except Exception as exc:
        return fail(f"invalid synthesis JSON: {exc}")

    decision = str(data.get("decision", "")).upper()
    if decision not in ALLOWED_DECISIONS:
        return fail("decision must be IMPLEMENT, BLOCK, RESEARCH or HARDWARE_VERIFY")
    if decision != "IMPLEMENT":
        return fail(f"orchestrator decision is {decision}")

    approvals = data.get("approvals", {})
    if not isinstance(approvals, dict):
        return fail("approvals must be an object")
    for key, role in REQUIRED_APPROVALS.items():
        value = approvals.get(key, {})
        if not isinstance(value, dict) or value.get("role") != role or value.get("status") != "APPROVE":
            return fail(f"missing approval gate: {key} ({role})")

    evidence = str(data.get("evidence_state", "UNKNOWN")).upper()
    if evidence in {"HYPOTHESIS", "UNKNOWN", "SIMULATED"}:
        return fail(f"evidence state {evidence} cannot authorize implementation")
    if data.get("requires_hardware_verification") is True:
        return fail("hardware verification is required before implementation")
    if data.get("destructive_operation") is True:
        return fail("destructive operation cannot be autonomously authorized")

    scope = data.get("implementation_scope")
    if not isinstance(scope, list) or not scope or not all(isinstance(x, str) and x.strip() for x in scope):
        return fail("implementation_scope must be a non-empty list of paths")

    print("IMPLEMENTATION_GATE=PASSED")
    print("MODE=ISOLATED_BRANCH_ONLY")
    print("MERGE=HUMAN_OR_RELEASE_GATE")
    print("HARDWARE=NO_AUTOMATIC_DEVICE_OPERATION")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
