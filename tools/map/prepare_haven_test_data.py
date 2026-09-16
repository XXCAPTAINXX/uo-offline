"""Prepare private, matching Haven server/client data from the tester's own UO assets.

Creates only a new output tree. Does not install, edit profiles, copy accounts, or start a server.
"""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

from plan_haven_islands import survey
from stage_haven_islands import stage as islands
from stage_haven_frontiers import stage as frontiers
from stage_original_dungeons import stage as originals


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def prepare(classic, modern, output):
    classic, modern = Path(classic).resolve(strict=True), Path(modern).resolve(strict=True)
    output = Path(output).resolve()
    if output.exists() or any(output == p or p in output.parents or output in p.parents for p in (classic, modern)):
        raise ValueError('Choose a new output folder outside both source installations')
    for name in ('map1.mul', 'map1x.mul', 'map5.mul', 'staidx1.mul', 'statics1.mul', 'tiledata.mul'):
        if not (classic/name).is_file():
            raise ValueError(f'Classic source is missing {name}; select the actual data folder, not its parent')
    for name in ('map1LegacyMUL.uop', 'map5LegacyMUL.uop', 'staidx1.mul', 'statics1.mul', 'staidx5.mul', 'statics5.mul', 'tiledata.mul', 'MainMisc.uop'):
        if not (modern/name).is_file():
            raise ValueError(f'Modern Classic source is missing {name}')
    output.mkdir(parents=True)
    print('Surveying the fixed Haven island footprints...', flush=True)
    survey(classic, output/'survey')
    plan_path = output/'survey/island-sites.json'
    plan = json.loads(plan_path.read_text())
    if plan['commons']['origin'] != [3904, 2800] or plan['pirate_estate']['origin'] != [4128, 2800]:
        raise ValueError('The surveyed island positions do not match this release; use clean supported classic data')
    islands(classic, plan_path, output/'islands')
    frontiers(output/'islands', output/'frontiers')
    frontier_plan = json.loads((output/'frontiers/haven-frontiers-manifest.json').read_text())
    if any(frontier_plan[key] != value for key, value in {'shadowguard':[4672,3104], 'blackthorn':[4800,3328], 'chelonia':[3904,3488]}.items()):
        raise ValueError('The frontier positions do not match this release')
    originals(output/'frontiers', modern, output/'server-data')
    print('Copying private client graphics and matching server geometry...', flush=True)
    client = output/'client-data'
    client.mkdir()
    # Copy game assets only. Profiles, credentials, executables and user scripts are not needed here.
    for path in modern.iterdir():
        if path.is_symlink():
            raise ValueError(f'Symlink in source data: {path.name}')
        if path.is_file() and (path.suffix.lower() in {'.uop', '.mul', '.idx', '.def', '.rle'} or path.name.lower().startswith('cliloc.')):
            if path.name.lower().startswith(('map', 'staidx', 'statics', 'stadif', 'tiledata')):
                continue  # Modern geometry or diff lists would override the matching server files.
            shutil.copy2(path, client/path.name)
    matches = []
    for path in (output/'server-data').iterdir():
        name = path.name.lower()
        if path.is_file() and (name.startswith(('map', 'statics', 'staidx', 'stadif')) or name == 'tiledata.mul') and path.suffix == '.mul':
            shutil.copy2(path, client/path.name)
            source_hash = digest(path)
            if digest(client/path.name) != source_hash:
                raise ValueError(f'Client/server geometry mismatch: {name}')
            matches.append({'file': path.name, 'sha256': source_hash})
    result = {'status':'matching_private_data_ready', 'server_data':str(output/'server-data'),
              'client_data':str(client), 'matching_geometry':matches, 'profiles_changed':False,
              'notes':'Use server_data first in ModernUO dataDirectories and client_data in the game client. Keep your own fresh saves.'}
    (output/'haven-test-data.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(result, indent=2), flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('classic_data')
    parser.add_argument('modern_classic_data')
    parser.add_argument('new_output_folder')
    args = parser.parse_args()
    prepare(args.classic_data, args.modern_classic_data, args.new_output_folder)
