#!/usr/bin/env python3
"""Bounded OpenAI controller for the existing Experiment 001 process."""
import argparse
import copy
import datetime
import getpass
import hashlib
import json
import os
from pathlib import Path
import queue
import subprocess
import sys
import threading
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
PROMPT = '''You control one agent in a four-agent, three-patch resource game lasting 80 rounds.
Maximize your own accumulated utility. There is no cooperation bonus.
All agents act simultaneously; other agents' current actions are unavailable.
Observation: agent is your seat, step is the zero-based round, ownPatch is your location,
energy is your balance, localCue is -1 for unknown patches, 0 for a poor own patch,
1 for a rich own patch. ReceivedSignals counts public messages for each patch from
last round. A message names a patch, not a verified quality report. Messages may be blocked.
Actions: code 0,1,2 forages at that patch. Its yield is split equally among agents
foraging there that round, then each pays 0.15 plus 0.20 if changing patch.
Code 3,4,5 signals patch 0,1,2 respectively and costs 0.40 instead of foraging.
Code 6 rests and gains 0.10. Sending costs apply even if messages are blocked.
Patch yields are integers 1..4 with independent stochastic drift. Rich means yield >=3.
Energy starts at 5; it is an accounting balance, without death or action restrictions.
You receive only your own current observation and up to eight past local transitions.
Return a JSON object with code (integer 0..6) and note (one short action explanation,
not private chain-of-thought).'''
SCHEMA = {'type': 'object', 'properties': {'code': {'type': 'integer', 'enum': list(range(7))},
          'note': {'type': 'string'}}, 'required': ['code', 'note'], 'additionalProperties': False}
OBS_FIELDS = {'agent', 'step', 'ownPatch', 'energy', 'localCue', 'receivedSignals'}


def payload(model, observation, history, output_tokens):
    # Construct a fresh whitelist, never serialize the controller event or global state.
    local = {k: copy.deepcopy(observation[k]) for k in sorted(OBS_FIELDS)}
    return {'model': model, 'store': False, 'instructions': PROMPT,
            'input': json.dumps({'observation': local, 'history': copy.deepcopy(history[-8:])}),
            'max_output_tokens': output_tokens,
            'text': {'format': {'type': 'json_schema', 'name': 'cell_action',
                                'strict': True, 'schema': SCHEMA}}}


def request_openai(body, key, timeout):
    req = urllib.request.Request('https://api.openai.com/v1/responses',
          data=json.dumps(body).encode(), headers={'Authorization': 'Bearer ' + key,
          'Content-Type': 'application/json'}, method='POST')
    try:
        with urllib.request.urlopen(req, timeout=timeout) as response:
            return json.load(response)
    except urllib.error.HTTPError as exc:
        # Do not echo request headers, credentials or arbitrary server error bodies.
        raise RuntimeError('OpenAI HTTP status ' + str(exc.code)) from None
    except (urllib.error.URLError, TimeoutError):
        raise RuntimeError('OpenAI network request failed or timed out') from None


def parse_action(response):
    if response.get('status') != 'completed':
        raise ValueError('Model response incomplete')
    texts = []
    for item in response.get('output', []):
        for content in item.get('content', []):
            if content.get('type') == 'refusal':
                raise ValueError('Model refused')
            if content.get('type') == 'output_text': texts.append(content['text'])
    def unique(pairs):
        result = {}
        for k, v in pairs:
            if k in result: raise ValueError('Duplicate response field')
            result[k] = v
        return result
    action = json.loads(''.join(texts), object_pairs_hook=unique)
    if not isinstance(action, dict) or set(action) != {'code', 'note'}:
        raise ValueError('Invalid action fields')
    if type(action['code']) is not int or not 0 <= action['code'] <= 6:
        raise ValueError('Invalid action code')
    if not isinstance(action['note'], str) or len(action['note']) > 1000:
        raise ValueError('Invalid action note')
    return action


def run(args, key, call=request_openai):
    if not 1 <= args.rounds <= 80 or args.max_calls < args.rounds * 4:
        raise ValueError('Need 1..80 rounds and max-calls >= four times rounds')
    if not 64 <= args.output_tokens <= 4096 or not 1 <= args.timeout <= 300:
        raise ValueError('Output tokens must be 64..4096; timeout 1..300 seconds')
    dll = Path(args.dll).resolve()
    if not dll.is_file(): raise ValueError('Build the headless project first')
    out = Path(args.output).resolve()
    out.mkdir(parents=True, exist_ok=False)
    def write(name, obj):
        (out/name).write_text(json.dumps(obj, indent=2) + '\n', encoding='utf-8')
    def git(*values):
        result = subprocess.run(['git', *values], cwd=ROOT, capture_output=True, text=True)
        return result.stdout.strip() if result.returncode == 0 else 'unavailable'
    manifest = {'schema': 'cells.llm-pilot.v1', 'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'status': 'started', 'provider': getattr(args, 'provider', 'openai'), 'model_requested': args.model, 'seed': args.seed,
        'delivery': args.delivery, 'rounds_requested': args.rounds, 'game_horizon': 80,
        'max_calls': args.max_calls, 'max_output_tokens_per_call': args.output_tokens,
        'timeout_seconds': args.timeout, 'retry_policy': 'none; abort on any failure',
        'reasoning_effort': getattr(args, 'reasoning_effort', None),
        'history_window': 8, 'prompt': PROMPT, 'prompt_sha256': hashlib.sha256(PROMPT.encode()).hexdigest(),
        'source_commit': git('rev-parse', 'HEAD'), 'dirty_worktree': bool(git('status', '--porcelain')),
        'controller_sha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        'dll_sha256': hashlib.sha256(dll.read_bytes()).hexdigest(),
        'calls_attempted': 0, 'completed_rounds': 0, 'input_tokens': 0, 'output_tokens': 0,
        'usage_missing_responses': 0, 'dollar_cost': 'not estimated'}
    write('manifest.json', manifest)
    process = None
    try:
        with (out/'events.jsonl').open('w', encoding='utf-8') as log, (out/'simulator.stderr.log').open('w') as err:
            def event(kind, **fields):
                log.write(json.dumps({'type': kind, **fields})+'\n'); log.flush()
            process = subprocess.Popen([args.dotnet, str(dll), '--strategic-session', str(args.seed), args.delivery],
                stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=err, text=True, encoding='utf-8',
                env={k:v for k,v in os.environ.items() if k not in ('OPENAI_API_KEY', 'TAMU_API_KEY', 'TAMU_CF_AUTHORIZATION')})
            inbox = queue.Queue()
            def reader():
                for line in process.stdout: inbox.put(line)
                inbox.put(None)
            threading.Thread(target=reader, daemon=True).start()
            histories = [[] for _ in range(4)]
            pending = None
            while True:
                try: line = inbox.get(timeout=args.timeout)
                except queue.Empty: raise RuntimeError('Simulator read timed out') from None
                if line is None: raise RuntimeError('Simulator exited before expected result')
                current = json.loads(line)
                event('environment', data=current)
                if current['type'] == 'decision':
                    if current['step'] >= args.rounds:
                        manifest['status'] = 'pilot_truncated'
                        break
                    observations = current['observations']
                    if sorted(o['agent'] for o in observations) != list(range(4)):
                        raise ValueError('Invalid observation seats')
                    actions = []
                    for obs in sorted(observations, key=lambda x:x['agent']):
                        seat = obs['agent']
                        if obs['step'] != current['step']: raise ValueError('Stale observation')
                        if manifest['calls_attempted'] >= args.max_calls: raise ValueError('Call cap reached')
                        body = payload(args.model, obs, histories[seat], args.output_tokens)
                        if getattr(args, 'reasoning_effort', None) is not None:
                            body['reasoning'] = {'effort': args.reasoning_effort}
                        if getattr(args, 'provider', 'openai') == 'tamu':
                            from tamu_llm_transport import chat_payload
                            body = chat_payload(body)
                        manifest['calls_attempted'] += 1
                        event('model_request', step=current['step'], seat=seat, body=body)
                        write('manifest.json', manifest)
                        response = call(body, key, args.timeout)
                        event('model_response', step=current['step'], seat=seat, body=response)
                        usage = response.get('usage') or {}
                        if not usage: manifest['usage_missing_responses'] += 1
                        manifest['input_tokens'] += usage.get('input_tokens', 0)
                        manifest['output_tokens'] += usage.get('output_tokens', 0)
                        action = parse_action(response)
                        actions.append({'agent': seat, 'code': action['code']})
                    pending = {'step': current['step'], 'actions': actions}
                    event('submitted_batch', **pending)
                    process.stdin.write(json.dumps(pending)+'\n'); process.stdin.flush()
                elif current['type'] == 'transition':
                    if pending is None or current['step'] != pending['step']:
                        raise ValueError('Unexpected transition')
                    for seat in range(4):
                        histories[seat].append({'observation': current['observations'][seat],
                            'code': current['actions'][seat]['code'], 'reward': current['rewards'][seat]})
                    manifest['completed_rounds'] += 1
                    pending = None
                    write('manifest.json', manifest)
                elif current['type'] == 'summary':
                    if manifest['completed_rounds'] != 80: raise ValueError('Premature summary')
                    write('summary.json', current['result'])
                    manifest['status'] = 'completed'
                    break
                elif current['type'] not in ('session',):
                    raise RuntimeError('Unexpected simulator event')
    except BaseException as exc:
        manifest['status'] = 'failed'
        manifest['error_type'] = type(exc).__name__
        if isinstance(exc, RuntimeError): manifest['error_message'] = str(exc)
        raise
    finally:
        if process:
            if process.stdin and not process.stdin.closed: process.stdin.close()
            try: process.wait(timeout=5)
            except subprocess.TimeoutExpired: process.kill(); process.wait()
            if process.stdout: process.stdout.close()
        write('manifest.json', manifest)
    return manifest


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('output', help='New output directory; never overwritten')
    p.add_argument('--provider', choices=['openai','tamu'], default='openai')
    p.add_argument('--model', required=True, help='Responses-compatible model with structured outputs; pin a snapshot for evaluation')
    p.add_argument('--reasoning-effort', choices=['none','low','medium','high'], default=None)
    p.add_argument('--seed', type=int, default=1200000)
    p.add_argument('--delivery', choices=['on','off'], default='on')
    p.add_argument('--rounds', type=int, default=4)
    p.add_argument('--max-calls', type=int, default=16)
    p.add_argument('--output-tokens', type=int, default=256)
    p.add_argument('--timeout', type=int, default=60)
    p.add_argument('--dotnet', default='dotnet')
    p.add_argument('--dll', default=str(ROOT/'Tests/Headless/bin/Release/net8.0/Cells.HeadlessChecks.dll'))
    p.add_argument('--prompt-key', action='store_true', help='Read existing key invisibly; keep only in memory')
    args = p.parse_args()
    key_env = 'TAMU_API_KEY' if args.provider == 'tamu' else 'OPENAI_API_KEY'
    key = getpass.getpass('API key (hidden): ') if args.prompt_key else os.environ.get(key_env, '')
    if not key.strip(): p.error('No API key available; use --prompt-key or '+key_env)
    call = request_openai
    if args.provider == 'tamu':
        if args.model != 'protected.gpt-5-mini':
            p.error('Initial TAMU pilot supports protected.gpt-5-mini only; other models need separate parameter validation')
        if args.reasoning_effort is not None:
            p.error('Do not set --reasoning-effort for the TAMU pilot')
        from tamu_llm_transport import normalize_cookie, request_tamu
        cookie = getpass.getpass('CF_Authorization cookie (hidden): ') if args.prompt_key else os.environ.get('TAMU_CF_AUTHORIZATION', '')
        try: cookie = normalize_cookie(cookie)
        except ValueError: p.error('Missing or invalid TAMU_CF_AUTHORIZATION; use --prompt-key')
        call = lambda body, key, timeout: request_tamu(body, key, timeout, cookie)
    try: result = run(args, key.strip(), call)
    except Exception as exc:
        print('Run failed ('+type(exc).__name__+'). Inspect local logs; no fallback actions were substituted.', file=sys.stderr)
        return 1
    print(json.dumps({k: result[k] for k in ['status','completed_rounds','calls_attempted','input_tokens','output_tokens']}))
    return 0

if __name__ == '__main__': sys.exit(main())
