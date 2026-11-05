#!/usr/bin/env python3
"""Run and independently audit the existing scripted baseline, without Unity."""
import argparse
import csv
import hashlib
import json
import math
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
PROTOCOL = ROOT / 'experiments/experiment001/baseline-v1.json'
FILES = ('episodes.csv', 'agents.csv', 'steps.csv', 'paired.csv', 'summary.csv', 'manifest.json')


def require(condition, message):
    if not condition:
        raise ValueError(message)


def rows(folder, name):
    with (folder / name).open(newline='', encoding='utf-8-sig') as stream:
        return list(csv.DictReader(stream))


def close(a, b):
    require(math.isfinite(a) and math.isfinite(b) and math.isclose(a, b, abs_tol=1e-7),
            f'Accounting mismatch: {a} != {b}')


def verify(folder, protocol):
    seeds = set(range(protocol['first_seed'], protocol['first_seed'] + protocol['seeds']))
    episode_rows = rows(folder, 'episodes.csv')
    agent_rows = rows(folder, 'agents.csv')
    steps = rows(folder, 'steps.csv')
    pairs = rows(folder, 'paired.csv')
    expected = {(s, d) for s in seeds for d in ('off', 'on')}
    episodes = {(int(r['seed']), r['delivery']): r for r in episode_rows}
    agents = {(int(r['seed']), r['delivery'], int(r['seat'])): r for r in agent_rows}
    trace = {(int(r['seed']), r['delivery'], int(r['step']), int(r['seat'])): r for r in steps}
    require(set(episodes) == expected and len(episode_rows) == len(expected), 'Episode coordinates')
    require(set(agents) == {(*key, seat) for key in expected for seat in range(4)}
            and len(agent_rows) == len(expected)*4, 'Agent coordinates')
    require(set(trace) == {(*key, t, seat) for key in expected for t in range(80) for seat in range(4)}
            and len(steps) == len(expected)*320, 'Trace coordinates')
    for key, episode in episodes.items():
        require(int(episode['invalid_actions']) == 0, 'Invalid action')
        close(float(episode['collective_return']), sum(float(agents[*key, i]['realized_return']) for i in range(4)))
        signals = 0
        for seat in range(4):
            trajectory = [trace[*key, t, seat] for t in range(80)]
            close(float(agents[*key, seat]['realized_return']), sum(float(r['reward']) for r in trajectory))
            for r in trajectory:
                action = int(r['action'])
                require(0 <= action <= 6, 'Action outside declared space')
                if key[1] == 'off':
                    require(int(r['received_signals']) == 0, 'Signal leaked through disabled channel')
                if 3 <= action <= 5:
                    signals += 1
                    close(float(r['reward']), -.4)
                elif action == 6:
                    close(float(r['reward']), .1)
        require(signals == int(episode['signals']), 'Signal count mismatch')
    require(len(pairs) == len(seeds) and {int(r['seed']) for r in pairs} == seeds, 'Pair coordinates')
    for r in pairs:
        seed = int(r['seed'])
        for condition in ('off', 'on'):
            close(float(r[condition+'_return']), float(episodes[seed, condition]['collective_return']))
        close(float(r['on_minus_off']), float(r['on_return'])-float(r['off_return']))
    summary = rows(folder, 'summary.csv')
    require(len(summary) == 1, 'Missing summary')
    return {'verified': True, 'paired_worlds': len(seeds), 'episodes': len(episodes),
            'agent_outcomes': len(agents), 'action_records': len(trace), 'summary': summary[0]}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--dotnet', default='dotnet')
    args = parser.parse_args()
    protocol = json.loads(PROTOCOL.read_text())
    output = args.output.resolve()
    require(not output.exists(), 'Use a new output directory; existing evidence is never overwritten')
    project = 'Tests/Headless/Cells.HeadlessChecks.csproj'
    subprocess.run([args.dotnet, 'build', project, '-c', 'Release'], cwd=ROOT, check=True)
    run = [args.dotnet, str(ROOT / 'Tests/Headless/bin/Release/net8.0/Cells.HeadlessChecks.dll')]
    subprocess.run(run + ['--experiment001-checks'], cwd=ROOT, check=True)
    def execute(target):
        subprocess.run(run + ['--experiment001', str(target),
                              str(protocol['first_seed']), str(protocol['seeds'])], cwd=ROOT, check=True)
    execute(output)
    audit = verify(output, protocol)
    with tempfile.TemporaryDirectory(prefix='cells-repeat-') as temp:
        execute(Path(temp))
        for name in FILES:
            require((output/name).read_bytes() == (Path(temp)/name).read_bytes(), 'Nondeterministic '+name)
    sources = [ROOT/project, PROTOCOL, Path(__file__).resolve()]
    sources += sorted((ROOT/'Tests/Headless').glob('*.cs'))
    sources += sorted((ROOT/'Assets/CellsDemo/Scripts/Experiments').glob('*.cs'))
    sources += sorted((ROOT/'Assets/CellsDemo/Scripts/Automata').glob('*.cs'))
    sources += [ROOT/'Assets/CellsDemo/Scripts/Physiology/PlanarDragBalance.cs']
    version = subprocess.run([args.dotnet, '--version'], text=True, capture_output=True)
    audit.update(source_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
                 dirty_worktree=bool(subprocess.check_output(['git','status','--porcelain'],cwd=ROOT,text=True).strip()),
                 dotnet_version=version.stdout.strip() if version.returncode == 0 else 'unavailable: CLI version query failed',
                 identical_repeat=True, protocol=protocol,
                 source_sha256={str(p.relative_to(ROOT)): digest(p) for p in sources},
                 output_sha256={name: digest(output/name) for name in FILES})
    (output/'verification.json').write_text(json.dumps(audit, indent=2)+'\n')
    print(json.dumps({k: v for k,v in audit.items() if k not in ('source_sha256','output_sha256','protocol')}, indent=2))


if __name__ == '__main__':
    main()
