"""Create a private, readable Haven recovery snapshot. See docs/BACKUPS.md.

Run static while online, runtime after a confirmed save and shutdown, then verify.
Never deletes source files or replaces an existing snapshot phase.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
from datetime import datetime, timezone


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2), encoding='utf-8')


def copy_tree(source, target, records, excluded=()):
    source = source.resolve(strict=True)
    for base, dirs, files in os.walk(source):
        parent = Path(base)
        dirs[:] = sorted(d for d in dirs if
            (parent / d).relative_to(source).as_posix() not in excluded)
        for directory in dirs:
            child = parent / directory
            if child.is_symlink() or child.is_junction():
                raise RuntimeError(f'Linked directory needs explicit backup handling: {child}')
        for name in sorted(files):
            src = parent / name
            relative = src.relative_to(source)
            if relative.as_posix() in excluded:
                continue
            if src.is_symlink():
                raise RuntimeError(f'Symlink needs explicit backup handling: {src}')
            dest = target / relative
            dest.parent.mkdir(parents=True, exist_ok=True)
            for attempt in range(3):
                before = src.stat()
                shutil.copy2(src, dest)
                checksum = digest(src)
                after = src.stat()
                if (before.st_size, before.st_mtime_ns) == (after.st_size, after.st_mtime_ns) and digest(dest) == checksum:
                    break
            else:
                raise RuntimeError(f'Source changed during backup: {src}')
            records.append(dict(path=dest.relative_to(snapshot).as_posix(),
                                size=after.st_size, sha256=checksum))
        if len(records) and len(records) // 1000 > copy_tree.reported:
            copy_tree.reported = len(records) // 1000
            print(f'Copied and hash checked {len(records):,} files', flush=True)


copy_tree.reported = 0
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('phase', choices=['static', 'runtime', 'verify'])
parser.add_argument('--snapshot', required=True, type=Path)
parser.add_argument('--install', type=Path, default=Path('D:/Uo Offline/uo-modernuo'))
parser.add_argument('--workspace', type=Path, default=Path('E:/(Offline UO)/uo-offline-haven-rc4'))
parser.add_argument('--client', type=Path, default=Path('D:/Ultima Online/TazUO-Launcher.win-x64'))
args = parser.parse_args()
snapshot = args.snapshot.resolve()
for source in (args.install, args.workspace, args.client):
    if snapshot.is_relative_to(source.resolve()) or source.resolve().is_relative_to(snapshot):
        raise SystemExit('Backup and source directories must not overlap.')
snapshot.mkdir(parents=True, exist_ok=True)
phase_manifest = snapshot / f'{args.phase}-manifest.json'
if args.phase != 'verify' and phase_manifest.exists():
    raise SystemExit('This phase already exists; use a new dated snapshot.')

if args.phase == 'verify':
    records = []
    for phase in ('static', 'runtime'):
        records.extend(json.loads((snapshot / f'{phase}-manifest.json').read_text())['files'])
    for record in records:
        file = (snapshot / record['path']).resolve()
        if not file.is_relative_to(snapshot) or file.stat().st_size != record['size'] or digest(file) != record['sha256']:
            raise SystemExit(f"Verification failed: {record['path']}")
    report = dict(status='verified', utc=datetime.now(timezone.utc).isoformat(),
                  snapshot=str(snapshot), files=len(records), bytes=sum(r['size'] for r in records),
                  manifests={p: digest(snapshot / f'{p}-manifest.json') for p in ('static', 'runtime')})
    write_json(snapshot / 'VERIFIED.json', report)
    write_json(snapshot.parent.parent / 'LATEST.json', report)
    print(json.dumps(report, indent=2))
else:
    records = []
    if args.phase == 'runtime':
        # Fail closed if a server is listening, including one on another address.
        probe = subprocess.run(['powershell.exe', '-NoProfile', '-Command',
            "@(Get-NetTCPConnection -State Listen -LocalPort 2593 -ErrorAction SilentlyContinue).Count"],
            check=True, capture_output=True, text=True)
        if int(probe.stdout.strip()) != 0:
            raise SystemExit('Save and stop the server before the runtime phase.')
        copy_tree(args.install / 'ModernUO/Distribution', snapshot / 'install/ModernUO/Distribution', records)
    else:
        # Current server logs and the operator inbox are transient, not restore inputs.
        excluded = ['ModernUO/Distribution', 'HavenReleaseInbox']
        excluded.extend(p.name for p in args.install.glob('*.log'))
        copy_tree(args.install, snapshot / 'install', records, excluded)
        for name in ('haven-fixes', 'artifacts', 'modern-evolution', 'uo-offline-haven-rc4', 'test-results'):
            if (args.workspace / name).is_dir():
                copy_tree(args.workspace / name, snapshot / 'workspace' / name, records,
                          ['__pycache__'])
        # Include loose project scripts without entering the unrelated/rehearsal trees.
        copy_tree(args.workspace, snapshot / 'workspace', records,
                  [p.name for p in args.workspace.iterdir() if p.is_dir()])
        copy_tree(args.client, snapshot / 'client-launcher', records)
        repo = args.workspace / 'haven-fixes'
        state = dict(head=subprocess.check_output(['git', '-C', str(repo), 'rev-parse', 'HEAD'], text=True).strip(),
                     status=subprocess.check_output(['git', '-C', str(repo), 'status', '--short'], text=True),
                     install=str(args.install), workspace=str(args.workspace), client=str(args.client),
                     excluded=['verification rehearsal trees', 'dads-games (separate Zelda project)',
                               'live operator inbox', 'install-root log files',
                               'unrelated games and shared EA client outside the Haven install'])
        write_json(snapshot / 'SOURCE-STATE.json', state)
    write_json(phase_manifest, dict(utc=datetime.now(timezone.utc).isoformat(), files=records))
    print(f'{args.phase}: {len(records):,} files / {sum(r["size"] for r in records):,} bytes verified', flush=True)
