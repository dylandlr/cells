"""Integration checks against the compiled, existing C# environment."""
import json
import os
from pathlib import Path
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]
DLL = ROOT / 'Tests/Headless/bin/Release/net8.0/Cells.HeadlessChecks.dll'
DOTNET = os.environ.get('CELLS_DOTNET', 'dotnet')


def batch(step, codes=None, reverse=False):
    codes = codes or [6] * 4
    entries = [{'agent': i, 'code': codes[i]} for i in range(4)]
    return json.dumps({'step': step, 'actions': entries[::-1] if reverse else entries})


def run(lines, delivery='on'):
    result = subprocess.run([DOTNET, str(DLL), '--strategic-session', '1200000', delivery],
                            input='\n'.join(lines) + '\n', text=True, capture_output=True, check=True)
    return [json.loads(line) for line in result.stdout.splitlines()]


class SessionTests(unittest.TestCase):
    def test_complete_rest_episode(self):
        events = run([batch(step) for step in range(80)])
        summary = events[-1]['result']
        self.assertEqual(summary['steps'], 80)
        self.assertEqual(summary['invalidActions'], 0)
        self.assertEqual(len([e for e in events if e['type'] == 'transition']), 80)
        for value in summary['returns']:
            self.assertAlmostEqual(value, 8)
        observation = events[1]['observations'][0]
        self.assertEqual(set(observation), {'agent', 'step', 'ownPatch', 'energy', 'localCue', 'receivedSignals'})
        self.assertEqual(sum(v == -1 for v in observation['localCue']), 2)

    def test_invalid_batches_are_atomic(self):
        bad = ['{', '{}', 'null', '[]', batch(1), batch(0, [7, 6, 6, 6]),
               '{"step":0,"step":0,"actions":[]}',
               json.dumps({'step': 0, 'actions': [{'agent': 0, 'code': 6}] * 4}),
               json.dumps({'step': 0, 'actions': [{'agent': i, 'code': 6} for i in range(3)]})]
        events = run(bad + [batch(0)])
        errors = [e for e in events if e['type'] == 'error']
        self.assertEqual(len(errors), len(bad))
        self.assertTrue(all(e['step'] == 0 for e in errors))
        transition = next(e for e in events if e['type'] == 'transition')
        self.assertEqual(transition['rewards'], [0.1] * 4)
        self.assertEqual(events[-1], {'type': 'aborted', 'step': 1, 'reason': 'input_closed'})
        self.assertFalse(any(e['type'] == 'summary' for e in events))

    def test_replay_and_batch_order(self):
        codes = lambda step: [(step + seat) % 7 for seat in range(4)]
        original = [batch(s, codes(s)) for s in range(80)]
        first = run(original)
        self.assertEqual(first, run(original))
        self.assertEqual(first, run([batch(s, codes(s), True) for s in range(80)]))
        replay = [batch(e['step'], [a['code'] for a in e['actions']])
                  for e in first if e['type'] == 'transition']
        self.assertEqual(first, run(replay))

    def test_delivery_ablation_preserves_costs(self):
        actions = [batch(0, [3, 4, 5, 6]), batch(1)]
        on, off = run(actions), run(actions, 'off')
        for events in (on, off):
            transition = next(e for e in events if e['type'] == 'transition')
            self.assertEqual(transition['rewards'], [-0.4, -0.4, -0.4, 0.1])
        for events, expected in ((on, [1, 1, 1]), (off, [0, 0, 0])):
            decision = next(e for e in events if e['type'] == 'decision' and e['step'] == 1)
            self.assertTrue(all(o['receivedSignals'] == expected for o in decision['observations']))


if __name__ == '__main__':
    unittest.main()
