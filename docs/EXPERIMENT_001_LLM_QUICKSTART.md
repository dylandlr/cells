# OpenAI agents: development pilot

## Texas A&M course gateway

Configured from the supplied CSCE 631-D Fall 2026 API guide. Use this command for the
university service; the public OpenAI commands below are a separate provider option.

```powershell
git pull --ff-only origin feat/substance-ontology
dotnet build Tests/Headless/Cells.HeadlessChecks.csproj -c Release
python tools/run_llm_experiment001.py artifacts/tamu-pilot-001 --provider tamu --model protected.gpt-5-mini --prompt-key
```

The terminal prompts invisibly for the course API key and your personal CF_Authorization
cookie. Copy the course key from the course guide locally; it is intentionally not included
in this repository. Obtain your cookie after signing into https://chat.tamu.ai with TAMU
SSO/Duo: browser DevTools → Application (Firefox: Storage) → Cookies → chat.tamu.ai →
CF_Authorization → Value. Paste only that value, or its CF_Authorization=value pair,
into the hidden local prompt. Never paste either credential into chat or a shared log.
The guide states the cookie expires in approximately 24 hours and the per-student quota
is $5/day. A fresh sign-in/cookie may be needed after HTTP 401/403 or a sign-in redirect.

For preconfigured environments, omit --prompt-key and supply TAMU_API_KEY and
TAMU_CF_AUTHORIZATION locally. These are not loaded automatically from env files.
The simulator child process does not inherit these credential variables.

TAMU requests use https://chat.tamu.ai/api/chat/completions, streaming SSE,
temperature 0.7 and max_tokens 256 by default. The initial adapter deliberately supports
only protected.gpt-5-mini; the guide's Claude thinking models require different settings.
Do not pass --reasoning-effort in TAMU mode. Unsupported structured-output options are
not sent: JSON is requested in the prompt and strictly validated locally. Invalid JSON,
refusals, truncation, missing stream completion and transport errors abort without fallback.

TAMU response logs contain reconstructed answer text, returned model/id, finish status
and usage when supplied. They are normalized records, not byte-for-byte raw SSE archives.
Reasoning content is ignored. Missing usage remains explicitly flagged. The guide does
not promise that every model provides usage, so recorded zero is not proof of zero cost.
Redirects are rejected rather than forwarding session credentials to another host.
The four-round pilot makes at most 16 requests and reports pilot_truncated on success.
No live authenticated TAMU run has yet been verified.

## Public OpenAI alternative

The controller runs four OpenAI-controlled agents against the existing C# Experiment 001.
It needs Python 3.11+, .NET 8 SDK, an OpenAI API key and access to a Responses-compatible
model supporting structured outputs. Python uses its standard library; no pip install is required.
Unity is not required. This integration has passed offline tests with fixture responses;
no live OpenAI run has been performed by the authoring assistant.

## Windows PowerShell

From the cells repository:

```powershell
git pull --ff-only origin feat/substance-ontology
dotnet build Tests/Headless/Cells.HeadlessChecks.csproj -c Release
python tools/run_llm_experiment001.py artifacts/llm-pilot-001 --model gpt-5.4-mini --reasoning-effort none --prompt-key
```

The terminal asks for your existing key with hidden input. Paste it there, not into chat
or a command argument. It stays in process memory and is not written to disk. If an
OPENAI_API_KEY environment variable already exists, omit --prompt-key. Env files are
ignored by Git but are not automatically loaded by this script. Use a regular terminal
that supports Python getpass. Never share credentials or commit them.

The example disables reasoning for the small connectivity pilot; this is not a claim
about the best research configuration. The example model is documented to support structured outputs; account access is still
required. --model accepts an alternative compatible model. Use a pinned snapshot for the
final study and record the returned model identifier in the response logs.

Default pilot: 4 rounds, 16 total requests, at most 256 output tokens per request,
60-second request timeout and zero retries. Each agent receives at most eight local
history transitions. These bounds limit requests/output; they are not a dollar spending
cap. Input tokens are also billed. Actual usage is recorded; dollar cost is not estimated.

A successful short pilot ends with status `pilot_truncated`, not `completed`: the game
itself still has an 80-round horizon. No terminal utility summary is manufactured.
For a full episode, after the pilot works:

```powershell
python tools/run_llm_experiment001.py artifacts/llm-full-on-001 --model gpt-5.4-mini --reasoning-effort none --rounds 80 --max-calls 320 --delivery on --prompt-key
python tools/run_llm_experiment001.py artifacts/llm-full-off-001 --model gpt-5.4-mini --reasoning-effort none --rounds 80 --max-calls 320 --delivery off --prompt-key
```

Each output directory must be new. Two full episodes can make 640 requests. Both examples
use development seed 1200000; they are pilots, not fresh held-out evaluation. Matching
world seeds does not make model sampling deterministic or couple its randomness across arms.
Do not tune on these worlds and subsequently label them unseen tests.

## Isolation, actions and audit

The controller sends the same fixed rules to all four agents. Each request includes only
that seat's current observation and its previous local observations/actions/rewards.
It sends no world seed, other agents' private observations, current choices or shared
conversation state. Requests are sequential, but the environment is paused until all
four valid choices arrive. No earlier seat response enters a later seat's prompt.

OpenAI Responses uses strict JSON schema for `{code, note}`; the controller also validates
it locally. Incomplete responses, refusals, invalid actions, API failures and timeouts abort
the run. There is no repair retry or scripted fallback. Valid calls made before a later
failure may still be billable. Failed runs retain partial logs and are not counted as
completed episodes. The model's note is a short action explanation, not evidence of its
internal reasoning or motivation. No rationale is used as a reward.

Files:

- `manifest.json`: status, settings, prompt/hash, source and executable hashes, call count,
  completed rounds and reported token usage. Missing response usage is counted explicitly.
- `events.jsonl`: exact model request bodies, raw response bodies (including response/model
  identifiers and usage), submitted actions and simulator observations/transitions.
- `summary.json`: only for a completed 80-round episode; existing game utility/counters.
- `simulator.stderr.log`: simulator diagnostics.

Request credentials are never included in these records. API errors record safe HTTP status
rather than raw error bodies. `store: false` is sent to OpenAI; this is not a claim of zero
provider retention under every account policy. The controller has no browser/tools/code
execution available to model agents. Actions remain bounded game integers.

Inspect manifest status before using outputs in a report. Stop a run with Ctrl+C; it will
be marked failed rather than completed. Logs are development artifacts under ignored
`artifacts/`. Preserve chosen completed runs with their exact source revision when packaging
research evidence. Replay simulator dynamics with recorded actions; live model responses
need not repeat exactly even with a pinned model.

## Verification

```powershell
python -m unittest discover -s Tests/Headless -p test_llm_controller.py
```

Set CELLS_DOTNET to an absolute runtime path if needed. Offline tests exercise a full episode,
local history isolation, utility accounting, usage totals, failure before world advancement,
strict parsing, pilot truncation, output protection and the request budget. Fixture outputs
are never reported as LLM findings.

API contract references:
- https://developers.openai.com/api/reference/resources/responses/methods/create
- https://developers.openai.com/api/docs/guides/structured-outputs?api-mode=responses
- https://developers.openai.com/api/docs/models/gpt-5.4-mini
