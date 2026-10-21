import json
from pathlib import Path
import sys
import tempfile
import unittest
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from evaluate_motor_logs import evaluation_report, file_identity, MARKER
from test_evaluate_motor_logs import episode

class ProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        self.baseline = self.root / 'baseline.log'
        self.candidate = self.root / 'candidate.log'
        self.baseline.write_text(MARKER + json.dumps(episode()))
        row = episode()
        row.update(behaviorType='InferenceOnly', evaluationLabel='test-model')
        self.candidate.write_text(MARKER + json.dumps(row))
        self.model = self.root / 'fixture.onnx'
        self.model.write_bytes(b'SYNTHETIC CHECKSUM FIXTURE - NOT A MODEL')

    def test_checksums_record_exact_supplied_files(self):
        report = evaluation_report(self.candidate, self.baseline, self.model)
        self.assertEqual(report['sources']['candidate']['model'], file_identity(self.model))
        self.assertEqual(report['sources']['baseline']['log'], file_identity(self.baseline))
        self.assertIsNone(report['sources']['baseline']['model'])
        self.assertIn('do not prove', report['provenanceLimit'])

    def test_inference_requires_model(self):
        with self.assertRaises(ValueError):
            evaluation_report(self.candidate, self.baseline)

    def test_heuristic_rejects_model(self):
        with self.assertRaises(ValueError):
            evaluation_report(self.baseline, self.baseline, self.model)

    def test_changed_file_changes_hash(self):
        before = file_identity(self.model)['sha256']
        self.model.write_bytes(b'different synthetic fixture')
        self.assertNotEqual(before, file_identity(self.model)['sha256'])
