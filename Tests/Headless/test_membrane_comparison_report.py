import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

SCRIPT=pathlib.Path(__file__).resolve().parents[2]/"tools/capture/analyze_membrane_comparison.py"

def metrics(area=3.0, valid=True):
    return {"valid":valid,"neckRatio":1.25,"absoluteArea":area,
            "perimeter":6.5,"normalizedAreaDrift":0.0}

class MembraneComparisonReportTests(unittest.TestCase):
    def evaluate(self,report):
        with tempfile.TemporaryDirectory() as temp:
            source=pathlib.Path(temp)/"input.json"
            source.write_text(json.dumps(report))
            result=subprocess.run([sys.executable,str(SCRIPT),str(source),temp],
                                  capture_output=True,text=True)
            files={p.name:p.read_text() for p in pathlib.Path(temp).glob("membrane-comparison*")}
            return result,files

    def test_equal_baseline_exports_csv(self):
        samples=[{"step":i,"time":i*.02,"legacy":metrics(),
                  "boundary":metrics()} for i in range(2)]
        result,files=self.evaluate({"status":"completed","samples":samples})
        self.assertEqual(0,result.returncode,result.stderr)
        self.assertIn("membrane-comparison.csv",files)
        self.assertIn("membrane-comparison-summary.md",files)

    def test_unequal_initial_geometry_rejected(self):
        samples=[{"step":0,"time":0,"legacy":metrics(),
                  "boundary":metrics(4.0)}]
        result,_=self.evaluate({"status":"completed","samples":samples})
        self.assertNotEqual(0,result.returncode)

    def test_invalid_completed_trace_rejected(self):
        samples=[{"step":0,"time":0,"legacy":metrics(),
                  "boundary":metrics(valid=False)}]
        result,_=self.evaluate({"status":"completed","samples":samples})
        self.assertNotEqual(0,result.returncode)

if __name__=="__main__":
    unittest.main()
