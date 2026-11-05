import csv
import importlib.util
from pathlib import Path
import tempfile
import unittest

path = Path(__file__).resolve().parents[2] / 'tools/run_experiment001.py'
spec = importlib.util.spec_from_file_location('runner', path)
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class EvidenceChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.protocol = {'first_seed': 10, 'seeds': 2}
        self.data = {
            'episodes.csv': [dict(seed=s, delivery=d, collective_return=32, signals=0, invalid_actions=0)
                             for s in (10,11) for d in ('off','on')],
            'agents.csv': [dict(seed=s, delivery=d, seat=i, realized_return=8)
                           for s in (10,11) for d in ('off','on') for i in range(4)],
            'steps.csv': [dict(seed=s, delivery=d, step=t, seat=i, action=6, reward=.1, received_signals=0)
                          for s in (10,11) for d in ('off','on') for t in range(80) for i in range(4)],
            'paired.csv': [dict(seed=s, off_return=32, on_return=32, on_minus_off=0) for s in (10,11)],
            'summary.csv': [dict(contrast='delivery_on_minus_off')]
        }

    def check(self):
        for name, rows in self.data.items():
            with (self.root/name).open('w', newline='') as f:
                writer=csv.DictWriter(f,fieldnames=list(rows[0]))
                writer.writeheader(); writer.writerows(rows)
        return runner.verify(self.root,self.protocol)

    def test_complete_trace(self):
        self.assertEqual(self.check()['action_records'],1280)

    def test_duplicate_coordinate(self):
        self.data['steps.csv'][-1]=self.data['steps.csv'][0].copy()
        with self.assertRaises(ValueError): self.check()

    def test_delivery_leak(self):
        self.data['steps.csv'][0]['received_signals']=1
        with self.assertRaises(ValueError): self.check()

    def test_forged_utility(self):
        self.data['agents.csv'][0]['realized_return']=9
        with self.assertRaises(ValueError): self.check()

    def test_unpaid_signal(self):
        self.data['steps.csv'][0]['action']=3
        with self.assertRaises(ValueError): self.check()


if __name__ == '__main__': unittest.main()
