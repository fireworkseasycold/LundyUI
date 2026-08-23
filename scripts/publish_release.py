#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""LundyUI NuGet 发布流水线（git 标签触发式，无需 Token / 浏览器）。

工作机制：
    1) 把 <Version> 升到目标版本（Directory.Build.props 单一来源），并同步 README / NUGET_README 版本表
    2) commit 版本变更
    3) 打 v<版本> 标签
    4) push 分支 + 标签 —— GitHub Actions 监测到 v* 标签后自动打包并 Trusted Publishing 到 nuget.org

零外部依赖：仅用 Python 标准库（subprocess / argparse / re / pathlib）。
运行前请确保已提交/暂存该清理，else 会因脏工作区而中止（-f 强制）。

用法示例：
    python scripts/publish_release.py --version 1.0.6            # 正常发布
    python scripts/publish_release.py --version 1.0.6 --dry-run  # 只预览，不改任何东西
    python scripts/publish_release.py --version 1.0.6 -f         # 允许带未提交改动强行走流程
"""
from __future__ import annotations

import argparse, re, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent          # 仓库根
VERSION_PROPS = ROOT / "Directory.Build.props"         # 版本单一来源
MD_FILES = [ROOT / "LundyUI" / "README.md", ROOT / "LundyUI" / "NUGET_README.md"]
WORKFLOW = "nuget-publish.yml"


def run(cmd: list[str], dry: bool) -> str:
    """执行 git 命令；dry=True 时只打印不执行。"""
    label = " ".join(cmd)
    if dry:
        print(f"[dry-run] $ {label}")
        return ""
    proc = subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if proc.returncode != 0:
        sys.exit(f"命令失败: {label}\n{proc.stdout}\n{proc.stderr}")
    out = proc.stdout.strip()
    if out:
        print(out)
    return out


def current_version() -> str:
    m = re.search(r"<Version>(\d+\.\d+\.\d+)</Version>", VERSION_PROPS.read_text(encoding="utf-8"))
    if not m:
        sys.exit(f"未在 {VERSION_PROPS} 中找到 <Version> 标签")
    return m.group(1)


def set_version(new_ver: str, dry: bool) -> None:
    old = current_version()
    print(f"版本: {old} -> {new_ver}")

    def patch(path: Path, text: str) -> str:
        s = text
        s = s.replace(f"<Version>{old}</Version>", f"<Version>{new_ver}</Version>")
        # 版本表里的旧版本替换（含包名行下的版本号）
        s = s.replace(f"**{old}**", f"**{new_ver}**")
        s = s.replace(f"** {old} **", f"** {new_ver} **")
        s = s.replace(f"| {old} |", f"| {new_ver} |")
        s = s.replace(f"{old}*/", f"{new_ver}*/")
        return s

    if dry:
        return
    VERSION_PROPS.write_text(patch(VERSION_PROPS, VERSION_PROPS.read_text(encoding="utf-8")), encoding="utf-8")
    for f in MD_FILES:
        f.write_text(patch(f, f.read_text(encoding="utf-8")), encoding="utf-8")


def is_dirty() -> bool:
    rc = subprocess.run(["git", "status", "--porcelain"], cwd=ROOT, capture_output=True, text=True).stdout.strip()
    return bool(rc)


def main() -> None:
    ap = argparse.ArgumentParser(description="LundyUI NuGet 发布流水线（git 标签触发）")
    ap.add_argument("--version", required=True, help="目标版本号，如 1.0.6")
    ap.add_argument("--branch", default="main", help="目标分支（默认 main）")
    ap.add_argument("-f", "--force", action="store_true", help="允许工作区非干净时继续")
    ap.add_argument("--dry-run", action="store_true", help="只预览，不修改仓库")
    a = ap.parse_args()

    if not re.fullmatch(r"\d+\.\d+\.\d+", a.version):
        sys.exit("版本号格式错误，应为 X.Y.Z")
    tag = f"v{a.version}"

    if is_dirty() and not a.force and not a.dry_run:
        sys.exit("工作区存在未提交改动，请先 commit/clean（或用 -f 强制后仍会提交这些改动）...")

    if current_version() == a.version and not a.force:
        sys.exit(f"当前已是最新版本 {a.version}，无需重复发布（如需重打请用 -f）")

    set_version(a.version, a.dry_run)

    files = [str(VERSION_PROPS.relative_to(ROOT))]
    files += [str(f.relative_to(ROOT)) for f in MD_FILES if f.exists()]
    message = f"release: 版本升至 {a.version}（NuGet {WORKFLOW} 自动发布）"

    if a.dry_run:
        print(f"[dry-run] git add {' '.join(files)}")
        print(f"[dry-run] git commit -m \"{message}\"")
        print(f"[dry-run] git tag {tag}")
        print(f"[dry-run] git push origin {a.branch}")
        print(f"[dry-run] git push origin {tag}")
        return

    run(["git", "add", "--", *files])
    run(["git", "commit", "-m", message])
    # 若标签已存在则先删再打，保证幂等
    run(["git", "tag", "-d", tag]) if run(["git", "tag", "-l", tag]) else None
    run(["git", "tag", tag])
    run(["git", "push", "origin", a.branch])
    run(["git", "push", "origin", tag])

    print(f"\n发布已触发: https://github.com/fireworkseasycold/LundyUI/actions/workflows/{WORKFLOW}")


if __name__ == "__main__":
    main()