import json
from pathlib import Path
import sys
import unittest
sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'tools'))
from tamu_llm_transport import chat_payload, normalize_cookie, parse_stream, NoRedirect

class TamuTests(unittest.TestCase):
    def stream(self, finish='stop', done=True):
        content=json.dumps({'code':6,'note':'rest'})
        items=[{'id':'fixture','model':'protected.gpt-5-mini','choices':[{'index':0,'delta':{'content':content[:10],'reasoning_content':'do not retain'}}]},
               {'choices':[{'index':0,'delta':{'content':content[10:]},'finish_reason':finish}]},
               {'choices':[],'usage':{'prompt_tokens':20,'completion_tokens':10}}]
        return [('data: '+json.dumps(x)+'\n').encode() for x in items]+([b'data: [DONE]\n'] if done else [])
    def test_fragmented_stream_usage_and_reasoning_exclusion(self):
        r=parse_stream(self.stream())
        self.assertEqual(json.loads(r['output'][0]['content'][0]['text'])['code'],6)
        self.assertEqual(r['usage'],{'input_tokens':20,'output_tokens':10})
        self.assertNotIn('do not retain',json.dumps(r))
    def test_truncated_stream_rejected(self):
        for finish,done in [('length',True),('stop',False),('content_filter',True)]:
            with self.assertRaises(ValueError):parse_stream(self.stream(finish,done))
    def test_cookie_formats_and_header_injection(self):
        self.assertEqual(normalize_cookie('abc.def'),normalize_cookie('CF_Authorization=abc.def'))
        for value in ['', 'abc; other=x','abc\nInjected:x']:
            with self.assertRaises(ValueError):normalize_cookie(value)
    def test_gateway_payload(self):
        p=chat_payload({'model':'protected.gpt-5-mini','instructions':'rules','input':'local','max_output_tokens':256})
        self.assertTrue(p['stream']);self.assertEqual(p['max_tokens'],256)
        self.assertNotIn('text',p);self.assertNotIn('reasoning',p)
        self.assertEqual(p['messages'][1]['content'],'local')
    def test_redirect_blocked(self):
        self.assertIsNone(NoRedirect().redirect_request(None,None,302,'redirect',{},'https://other.example'))

if __name__=='__main__':unittest.main()
