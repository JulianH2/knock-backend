#!/usr/bin/env python3
"""Activa los git hooks de Knock (.githooks/) en este clon. Funciona en Windows, macOS y Linux.

Uso: python scripts/install_githooks.py
"""
import os
import stat
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
subprocess.run(["git", "config", "core.hooksPath", ".githooks"], cwd=ROOT, check=True)
for hook in (ROOT / ".githooks").iterdir():
    hook.chmod(hook.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)
print("Git hooks activos (core.hooksPath=.githooks). En Windows se ejecutan con el sh de Git for Windows.")
if os.name == "nt":
    print("Asegúrate de tener Python en el PATH como 'python' o 'python3'.")
