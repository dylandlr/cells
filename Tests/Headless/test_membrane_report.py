import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

SCRIPT = pathlib.Path(__file__).resolve().parents[2] / "tools/capture/analyze_membrane_assay.py"


def sample(time, ratio=1.1, valid=True, narrowed=False):
    return {
        "time": time, "actuation": time, "equatorWidth": ratio,
        "shoulderWidth": 1.0, "ratio": ratio, "area": 3.0,
        "maxStress": 0.1, "valid": valid, "narrowed": narrowed
    }


class AnalyzeMembraneAssayTests(unittest.TestCase):
    def run_report(self, report):
        with tempfile.TemporaryDirectory() as tmp:
            path = pathlib.Path(tmp) / "report.json"
            path.write_text(json.dumps(report))
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(path), tmp],
                capture_output=True, text=True
            )
            outputs = {p.name: p.read_text() for p in pathlib.Path(tmp).glob("membrane-*")}
            return result, outputs

    def test_valid_report_produces_csv_and_summary(self):
        r, files = self.run_report({
            "status": "not-narrowed", "invalidSamples": 0, "narrowedSamples": 0,
            "samples": [sample(0), sample(1, 1.02)]
        })
        self.assertEqual(0, r.returncode, r.stderr)
        self.assertIn("membrane-trace.csv", files)
        self.assertIn("not-narrowed", files["membrane-summary.md"])

    def test_inconsistent_result_is_rejected(self):
        r, _ = self.run_report({
            "status": "narrowed", "invalidSamples": 0, "narrowedSamples": 0,
            "samples": [sample(0), sample(1)]
        })
        self.assertNotEqual(0, r.returncode)

    def test_invalid_geometry_is_not_mistaken_for_success(self):
        r, files = self.run_report({
            "status": "invalid-geometry", "invalidSamples": 1, "narrowedSamples": 0,
            "samples": [sample(0), sample(1, valid=False)]
        })
        self.assertNotEqual(0, r.returncode)
        self.assertIn("invalid-geometry", files["membrane-summary.md"])


if __name__ == "__main__":
    unittest.main()
