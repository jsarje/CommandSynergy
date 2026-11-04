#!/usr/bin/env bash
# update-context.sh — Copilot integration: update AGENTS.md
#
# GitHub Copilot reads AGENTS.md as the canonical repository guidance.
# .github/copilot-instructions.md remains a pointer to that file.
# The shared dispatcher reads .specify/integration.json and calls this script.
#
# Delegate to the cross-platform PowerShell implementation.

set -euo pipefail

# Derive repo root from script location (walks up to find .specify/)
_script_dir="$(cd "$(dirname "$0")" && pwd)"
_root="$_script_dir"
while [ "$_root" != "/" ] && [ ! -d "$_root/.specify" ]; do _root="$(dirname "$_root")"; done
if [ -z "${REPO_ROOT:-}" ]; then
  if [ -d "$_root/.specify" ]; then
    REPO_ROOT="$_root"
  else
    git_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
    if [ -n "$git_root" ] && [ -d "$git_root/.specify" ]; then
      REPO_ROOT="$git_root"
    else
      REPO_ROOT="$_root"
    fi
  fi
fi

exec pwsh -NoLogo -NoProfile -File "$REPO_ROOT/.specify/scripts/powershell/update-agent-context.ps1" -AgentType copilot
