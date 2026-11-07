"""TAMU course gateway transport. Credentials supplied locally, never logged."""
import json
import urllib.error
import urllib.request

ENDPOINT = 'https://chat.tamu.ai/api/chat/completions'


def chat_payload(body):
    return {'model': body['model'], 'messages': [
        {'role': 'system', 'content': body['instructions'] +
         '\nReturn only JSON: {"code": 0, "note": "short explanation"}. No markdown.'},
        {'role': 'user', 'content': body['input']}],
        'temperature': 0.7, 'max_tokens': body['max_output_tokens'], 'stream': True}


def normalize_cookie(value):
    value = value.strip()
    if value.startswith('CF_Authorization='): value = value.split('=', 1)[1]
    if not value or any(c in value for c in '\r\n; \t'):
        raise ValueError('Supply only the CF_Authorization value or its name=value pair')
    return 'CF_Authorization=' + value


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        # Expired sessions can redirect to sign-in. Do not forward credentials.
        return None


def parse_stream(lines):
    text = []
    finish = None
    usage = None
    model = None
    response_id = None
    done = False
    for raw in lines:
        line = raw.decode('utf-8').strip()
        if not line.startswith('data:'): continue
        value = line[5:].strip()
        if value == '[DONE]':
            done = True
            break
        if not value: continue
        part = json.loads(value)
        if 'error' in part: raise RuntimeError('TAMU stream reported an error')
        model = part.get('model', model)
        response_id = part.get('id', response_id)
        if part.get('usage'): usage = part['usage']
        for choice in part.get('choices', []):
            if choice.get('index', 0) != 0: raise ValueError('Unexpected multiple choices')
            delta = choice.get('delta') or {}
            if delta.get('refusal'): raise ValueError('TAMU model refused')
            content = delta.get('content')
            if content is not None:
                if not isinstance(content, str): raise ValueError('Invalid streamed content')
                text.append(content)
            # Ignore reasoning_content; it is not an action or an explanation to log.
            if choice.get('finish_reason') is not None: finish = choice['finish_reason']
    if not done or finish != 'stop':
        raise ValueError('TAMU stream incomplete or truncated')
    normalized = {'id': response_id, 'model': model, 'status': 'completed',
        'output': [{'type':'message', 'content':[{'type':'output_text','text':''.join(text)}]}],
        'transport': {'protocol':'chat.completions.sse', 'finish_reason': finish,
                      'reasoning_content_omitted': True}}
    if usage is not None:
        normalized['usage'] = {'input_tokens':usage.get('prompt_tokens',0),
                               'output_tokens':usage.get('completion_tokens',0)}
        normalized['transport']['reported_usage'] = usage
    return normalized


def request_tamu(body, key, timeout, cookie):
    req = urllib.request.Request(ENDPOINT, data=json.dumps(body).encode(),
        headers={'Authorization':'Bearer '+key, 'Cookie':normalize_cookie(cookie),
                 'Content-Type':'application/json', 'Accept':'text/event-stream'}, method='POST')
    try:
        with urllib.request.build_opener(NoRedirect()).open(req, timeout=timeout) as response:
            return parse_stream(response)
    except urllib.error.HTTPError as exc:
        raise RuntimeError('TAMU HTTP status '+str(exc.code)+'; check local credentials/session and quota') from None
    except (urllib.error.URLError, TimeoutError):
        raise RuntimeError('TAMU request failed or timed out') from None
