import copy
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from evaluate_motor_logs import read_episodes, compare, MARKER


def episode(seed=631):
    return dict(schema='cells.ml-episode.v2', seed=seed, ending='time_limit', alive=True,
                recordedSteps=1500, elapsedSeconds=30., fixedDeltaTime=.02,
                horizonSeconds=30., motorPenalty=.1, nutrientAbsorbed=2.,
                nutrientDigested=1., motorEnergy=.5, pathLength=3., reward=.95,
                behaviorType='HeuristicOnly', evaluationLabel='constant-075', heuristicActivation=.75)


class EvaluationTests(unittest.TestCase):
    def load(self, rows):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'Editor.log'
            path.write_text('Unrelated Unity log\n' + ''.join(MARKER + json.dumps(r) + '\n' for r in rows))
            return read_episodes(path)

    def test_valid_pair_and_single_sample_uncertainty(self):
        baseline = self.load([episode()])
        row = episode()
        row.update(nutrientDigested=1.5, reward=1.45)
        result = compare(self.load([row]), baseline)
        self.assertAlmostEqual(result['pairedCandidateMinusBaseline']['reward']['meanDelta'], .5)
        self.assertIsNone(result['pairedCandidateMinusBaseline']['reward']['sampleStdDev'])

    def test_death_reward(self):
        row = episode()
        row.update(ending='death', alive=False, reward=-.05)
        self.assertEqual(len(self.load([row])), 1)

    def test_rejects_corruption(self):
        for changes in [dict(reward=99), dict(motorEnergy=float('nan')),
                        dict(alive=False), dict(recordedSteps=12),
                        dict(behaviorType='Default'), dict(schema='cells.ml-episode.v1')]:
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                row = episode()
                row.update(changes)
                self.load([row])

    def test_empty_and_duplicate_rejected(self):
        for rows in ([], [episode(), episode()]):
            with self.assertRaises(ValueError):
                self.load(rows)

    def test_unmatched_seeds_rejected(self):
        with self.assertRaises(ValueError):
            compare(self.load([episode(1)]), self.load([episode(2)]))

    def test_mismatched_reward_settings_rejected(self):
        row = episode()
        row.update(motorPenalty=.2, reward=.9)
        with self.assertRaises(ValueError):
            compare(self.load([episode()]), self.load([row]))

    def test_mixed_policy_run_rejected(self):
        row = episode(632)
        row['evaluationLabel'] = 'different-policy'
        with self.assertRaises(ValueError):
            self.load([episode(), row])


if __name__ == '__main__':
    unittest.main()
