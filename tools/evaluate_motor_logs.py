"""Validate completed Unity episode logs and compare exactly matched seeds.

Inputs must be separate evaluation runs, not training logs mixed across policies.
No third-party Python dependencies. Prints JSON; never invents absent episodes.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import statistics

MARKER = 'CELL_ML_EPISODE '
METRICS = ('nutrientAbsorbed', 'nutrientDigested', 'motorEnergy', 'pathLength', 'reward', 'elapsedSeconds')


def finite_number(row, key):
    value = row.get(key)
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value):
        raise ValueError(f'{key} must be a finite number')
    return value


def read_episodes(path):
    episodes = {}
    for number, line in enumerate(Path(path).read_text(encoding='utf-8-sig').splitlines(), 1):
        if MARKER not in line:
            continue
        try:
            row = json.loads(line.split(MARKER, 1)[1])
            if not isinstance(row, dict):
                raise ValueError('episode must be a JSON object')
            if row.get('schema') != 'cells.ml-episode.v2':
                raise ValueError('requires v2 episode metadata; rerun evaluation with the updated adapter')
            seed = row.get('seed')
            if type(seed) is not int or seed in episodes:
                raise ValueError('invalid or duplicate seed; use one complete evaluation run per file')
            for key in METRICS:
                value = finite_number(row, key)
                if key != 'reward' and value < 0:
                    raise ValueError(f'{key} cannot be negative')
            steps = row.get('recordedSteps')
            if type(steps) is not int or steps <= 0:
                raise ValueError('recordedSteps must be a positive integer')
            dt = finite_number(row, 'fixedDeltaTime')
            horizon = finite_number(row, 'horizonSeconds')
            penalty = finite_number(row, 'motorPenalty')
            if dt <= 0 or horizon <= 0 or penalty < 0:
                raise ValueError('invalid timing or reward configuration')
            if not math.isclose(row['elapsedSeconds'], steps * dt, rel_tol=1e-4, abs_tol=1e-4):
                raise ValueError('elapsed time disagrees with recorded steps')
            if type(row.get('alive')) is not bool:
                raise ValueError('alive must be boolean')
            ending = row.get('ending')
            if ending not in ('death', 'time_limit') or row['alive'] != (ending == 'time_limit'):
                raise ValueError('termination and survival disagree')
            if ending == 'time_limit' and abs(row['elapsedSeconds'] - horizon) > dt + 1e-3:
                raise ValueError('time limit outside one physics step of horizon')
            expected = row['nutrientDigested'] - penalty * row['motorEnergy'] - (ending == 'death')
            if not math.isclose(row['reward'], expected, rel_tol=1e-4, abs_tol=1e-4):
                raise ValueError('reward does not match digestion, motor cost and death penalty')
            if row.get('behaviorType') not in ('InferenceOnly', 'HeuristicOnly'):
                raise ValueError('evaluation requires explicit InferenceOnly or HeuristicOnly behavior')
            if not isinstance(row.get('evaluationLabel'), str) or row['evaluationLabel'] in ('', 'unlabeled'):
                raise ValueError('evaluationLabel must identify the policy/model')
            episodes[seed] = row
        except (ValueError, TypeError, KeyError) as error:
            raise ValueError(f'{path}:{number}: {error}') from error
    if not episodes:
        raise ValueError(f'{path}: no completed episodes found')
    configs = {(r['fixedDeltaTime'], r['horizonSeconds'], r['motorPenalty'],
                r['behaviorType'], r['evaluationLabel'], r.get('heuristicActivation')) for r in episodes.values()}
    if len(configs) != 1:
        raise ValueError(f'{path}: configuration changed within evaluation run')
    return episodes


def compare(candidate, baseline):
    if set(candidate) != set(baseline):
        raise ValueError('seed sets differ; refusing an unmatched comparison')
    seeds = sorted(candidate)
    for seed in seeds:
        for key in ('fixedDeltaTime', 'horizonSeconds', 'motorPenalty'):
            if candidate[seed][key] != baseline[seed][key]:
                raise ValueError(f'seed {seed}: mismatched {key}')
    def summary(rows):
        return {**{key: statistics.mean(r[key] for r in rows.values()) for key in METRICS},
                'survivalRate': statistics.mean(int(r['alive']) for r in rows.values())}
    paired = {}
    for key in METRICS:
        deltas = [candidate[s][key] - baseline[s][key] for s in seeds]
        paired[key] = {'meanDelta': statistics.mean(deltas),
                       'sampleStdDev': statistics.stdev(deltas) if len(deltas) > 1 else None}
    return {'schema': 'cells.motor-comparison.v1', 'episodesPerPolicy': len(seeds),
            'seeds': seeds, 'candidate': summary(candidate), 'baseline': summary(baseline),
            'pairedCandidateMinusBaseline': paired,
            'note': 'Descriptive comparison only; does not establish training success or statistical significance.'}


def file_identity(path):
    path = Path(path)
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return {'filename': path.name, 'sha256': digest.hexdigest(), 'bytes': path.stat().st_size}


def evaluation_report(candidate_path, baseline_path, candidate_model=None, baseline_model=None):
    candidate = read_episodes(candidate_path)
    baseline = read_episodes(baseline_path)
    report = compare(candidate, baseline)
    sources = {}
    for label, path, rows, model in (
            ('candidate', candidate_path, candidate, candidate_model),
            ('baseline', baseline_path, baseline, baseline_model)):
        first = next(iter(rows.values()))
        inference = first['behaviorType'] == 'InferenceOnly'
        if inference and model is None:
            raise ValueError(f'{label}: provide the evaluated model file for an inference run')
        if not inference and model is not None:
            raise ValueError(f'{label}: heuristic runs must not supply a model file')
        sources[label] = {'log': file_identity(path), 'behaviorType': first['behaviorType'],
                          'evaluationLabel': first['evaluationLabel'],
                          'heuristicActivation': first.get('heuristicActivation'),
                          'model': file_identity(model) if model is not None else None}
    report['schema'] = 'cells.motor-comparison.v2'
    report['sources'] = sources
    report['provenanceLimit'] = ('Checksums identify supplied files. They do not prove Unity loaded '
                                 'the supplied model or that evaluation seeds were held out.')
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('candidate', type=Path)
    parser.add_argument('baseline', type=Path)
    parser.add_argument('--candidate-model', type=Path)
    parser.add_argument('--baseline-model', type=Path)
    parser.add_argument('--output', type=Path, help='Create a new JSON report; refuses to overwrite an existing file')
    args = parser.parse_args()
    try:
        report = evaluation_report(args.candidate, args.baseline, args.candidate_model, args.baseline_model)
        serialized = json.dumps(report, indent=2, allow_nan=False) + '\n'
        if args.output:
            with args.output.open('x', encoding='utf-8') as stream:
                stream.write(serialized)
        else:
            print(serialized, end='')
    except (ValueError, OSError) as error:
        parser.exit(2, f'Evaluation rejected: {error}\n')


if __name__ == '__main__':
    main()
