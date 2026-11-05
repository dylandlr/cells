import argparse
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('controller',ROOT/'tools/run_llm_experiment001.py')
c=importlib.util.module_from_spec(spec);spec.loader.exec_module(c)

def response(code=6):
    return {'status':'completed','model':'test-fixture-not-a-real-model',
            'output':[{'content':[{'type':'output_text','text':json.dumps({'code':code,'note':'fixture'})}]}],
            'usage':{'input_tokens':10,'output_tokens':5}}

class ControllerTests(unittest.TestCase):
    def args(self,output,rounds=4):
        return argparse.Namespace(output=str(output),model='test-fixture',seed=1200000,delivery='on',
            rounds=rounds,max_calls=rounds*4,output_tokens=256,timeout=10,
            dotnet=os.environ.get('CELLS_DOTNET','dotnet'),
            dll=str(ROOT/'Tests/Headless/bin/Release/net8.0/Cells.HeadlessChecks.dll'))

    def test_strict_response(self):
        for code in [True,7,-1,1.5,'2']:
            with self.assertRaises(ValueError): c.parse_action(response(code))
        with self.assertRaises(ValueError):c.parse_action({'status':'incomplete'})
        self.assertEqual(c.parse_action(response(2))['code'],2)

    def test_request_is_whitelisted_copy(self):
        obs=dict(agent=0,step=0,ownPatch=0,energy=5,localCue=[1,-1,-1],receivedSignals=[0,0,0],hiddenYield=[4,3,2])
        body=c.payload('test',obs,[],256)
        self.assertNotIn('hiddenYield',body['input'])
        self.assertNotIn('previous_response_id',body)
        self.assertFalse(body['store'])
        obs['localCue'][0]=99
        self.assertEqual(json.loads(body['input'])['observation']['localCue'][0],1)

    def test_full_episode_isolated_histories_and_usage(self):
        calls=[]
        def fake(body,key,timeout):
            local=json.loads(body['input']);o=local['observation'];calls.append(o)
            self.assertEqual(set(local),{'observation','history'})
            self.assertEqual(len(local['history']),min(o['step'],8))
            for previous in local['history']:
                self.assertEqual(previous['observation']['agent'],o['agent'])
                self.assertLess(previous['observation']['step'],o['step'])
                self.assertEqual(previous['code'],o['agent']+3)
            return response(o['agent']+3)
        with tempfile.TemporaryDirectory() as td:
            result=c.run(self.args(Path(td)/'run',80),'fixture-key',fake)
            self.assertEqual(result['status'],'completed')
            self.assertEqual(result['calls_attempted'],320)
            self.assertEqual(result['input_tokens'],3200)
            self.assertEqual(result['output_tokens'],1600)
            returns=json.loads((Path(td)/'run/summary.json').read_text())['returns']
            for actual,expected in zip(returns,[-32,-32,-32,8]):self.assertAlmostEqual(actual,expected)
            self.assertNotIn('fixture-key',(Path(td)/'run/events.jsonl').read_text())

    def test_partial_pilot_not_reported_as_episode(self):
        with tempfile.TemporaryDirectory() as td:
            out=Path(td)/'run'
            result=c.run(self.args(out),'fixture-key',lambda *a:response())
            self.assertEqual(result['status'],'pilot_truncated')
            self.assertEqual(result['completed_rounds'],4)
            self.assertEqual(result['calls_attempted'],16)
            self.assertFalse((out/'summary.json').exists())
            with self.assertRaises(FileExistsError):c.run(self.args(out),'fixture-key',lambda *a:response())

    def test_failure_before_batch_does_not_advance(self):
        count=0
        def fail(body,key,timeout):
            nonlocal count
            count+=1
            if count==3:raise RuntimeError('test outage')
            return response()
        with tempfile.TemporaryDirectory() as td:
            out=Path(td)/'run'
            with self.assertRaises(RuntimeError):c.run(self.args(out),'fixture-key',fail)
            result=json.loads((out/'manifest.json').read_text())
            self.assertEqual(result['status'],'failed')
            self.assertEqual(result['completed_rounds'],0)
            self.assertEqual(result['calls_attempted'],3)
            self.assertNotIn('submitted_batch',(out/'events.jsonl').read_text())

    def test_insufficient_budget_rejected_before_start(self):
        with tempfile.TemporaryDirectory() as td:
            args=self.args(Path(td)/'run');args.max_calls=15
            with self.assertRaises(ValueError):c.run(args,'fixture-key',lambda *a:response())
            self.assertFalse(Path(args.output).exists())

if __name__=='__main__':unittest.main()
