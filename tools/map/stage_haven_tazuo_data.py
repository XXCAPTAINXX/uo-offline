"""Make a new, independent TazUO asset directory with server-matching Trammel.

Never edits the source client, active profile, or an existing output directory.
The newer graphics remain available while only Trammel geometry is replaced.
"""
import argparse
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path


TRAMMEL_FILES = (
    "map1.mul", "map1x.mul", "staidx1.mul", "staidx1x.mul",
    "statics1.mul", "statics1x.mul", "mapdif1.mul", "mapdifl1.mul",
    "stadif1.mul", "stadifl1.mul", "stadifi1.mul",
)
TRAMMEL_UOPS = ("map1LegacyMUL.uop", "map1xLegacyMUL.uop")


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as source:
        while chunk := source.read(1024 * 1024):
            h.update(chunk)
    return h.hexdigest()


def stage(client, terrain, output):
    client = Path(client).resolve(strict=True)
    terrain = Path(terrain).resolve(strict=True)
    output = Path(output).resolve()
    if output.exists() or client == output or terrain == output:
        raise ValueError("Output must be a new, unused directory")
    if client in output.parents or terrain in output.parents:
        raise ValueError("Output must be outside both source directories")
    if output in client.parents or output in terrain.parents:
        raise ValueError("Output must not contain a source directory")
    if not (client / "MainMisc.uop").is_file():
        raise ValueError("Expected the verified newer UOP client assets")
    for name in TRAMMEL_FILES:
        if not (terrain / name).is_file():
            raise ValueError(f"Missing server-matching Trammel dependency: {name}")
    expected = json.loads((terrain / "haven-islands-manifest.json").read_text(encoding="utf-8"))
    if digest(terrain / "map1.mul") != expected["staged_map_sha256"]:
        raise ValueError("Staged map1 does not match its island manifest")
    manifest = {
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": "staging_incomplete",
        "source_client": str(client), "source_terrain": str(terrain),
        "output": str(output), "copies_are_independent": True,
        "active_profile_changed": False, "files": [],
        "client_data_profile_field": "ultimaonlinedirectory",
        "client_data_profile_value": str(output),
        "uop_policy": "Trammel UOPs are retained under disabled names; map1 MUL fallback is used.",
    }
    output.mkdir(parents=True)
    manifest_path = output / "haven-client-data-manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    sources = sorted(client.rglob("*"))
    count = 0
    for source in sources:
        if source.is_symlink():
            raise ValueError(f"Source symlink requires explicit review: {source}")
        relative = source.relative_to(client)
        destination = output / relative
        if source.is_dir():
            destination.mkdir(exist_ok=True)
            continue
        if relative.parent == Path(".") and source.name in TRAMMEL_FILES:
            continue
        disabled = relative.parent == Path(".") and source.name in TRAMMEL_UOPS
        if disabled:
            destination = destination.with_name(destination.name + ".disabled-for-haven")
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)
        source_hash = digest(source)
        if digest(destination) != source_hash:
            raise ValueError(f"Independent asset copy failed verification: {relative}")
        manifest["files"].append({
            "file": str(destination.relative_to(output)),
            "source": str(source), "sha256": source_hash,
            "size": destination.stat().st_size,
            "role": "disabled_shared_trammel_uop" if disabled else "preserved_client_asset",
        })
        count += 1
        if count % 50 == 0:
            print(f"Verified {count} independent client file copies", flush=True)
    for name in TRAMMEL_FILES:
        source, destination = terrain / name, output / name
        shutil.copy2(source, destination)
        source_hash = digest(source)
        if digest(destination) != source_hash:
            raise ValueError(f"Trammel geometry copy failed verification: {name}")
        manifest["files"].append({
            "file": name, "source": str(source), "sha256": source_hash,
            "size": destination.stat().st_size, "role": "server_matching_trammel",
        })
    for name in TRAMMEL_UOPS:
        if (output / name).exists():
            raise ValueError(f"UOP would override the staged MUL: {name}")
    manifest["status"] = "verified_private_client_data_ready_profile_unchanged"
    manifest["file_count"] = len(manifest["files"])
    manifest["total_bytes"] = sum(entry["size"] for entry in manifest["files"])
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps({key: value for key, value in manifest.items() if key != "files"}, indent=2), flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("client")
    parser.add_argument("terrain")
    parser.add_argument("output")
    args = parser.parse_args()
    stage(args.client, args.terrain, args.output)
