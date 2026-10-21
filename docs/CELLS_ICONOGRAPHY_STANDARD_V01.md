# cells Scientific Iconography Standard v0.1

**Scope:** Headless commons action viewer, experiment traces, Unity microscope overlays, research figures. **Status:** descriptive interface vocabulary, not scientific model validation.

## Rules

1. **Observed vs intended vs realized must never share a glyph.** An observed invitation is not a successful delivery to other agents, and an attempted restoration is not a completed restoration.
2. **Glyph + text always together** in recorded event logs. Color is redundant, never the only carrier of meaning.
3. **Only render events backed by trace fields.** Unavailable facts are shown as unknown—not inferred. A symbol can only be animated when a model-reported event exists.
4. **Role badges annotate controller provenance.** `[S]` scripted, `[L]` learned. These are not species or biological characteristics.
5. **No invented physical geometry.** Commons patch cards depict categorical locations, not continuous hydrodynamics.
6. **Stable semantics across media.** Same glyphs in slides, CSV event viewer, and eventual Unity inspector. Text alternatives must be accessible.

## Canonical glyph registry

| Class | Glyph | Event / state | Evidence required | Restrictions |
|---|---|---|---|---|
| entity | ● | living agent | agent index and observed alive status | Don't imply genotype |
| entity | ◎ | selected agent | UI selection | Viewer-only |
| entity | ▤ | resource patch | patch index, stock observation | Not a spatial position |
| entity | ◫ | substrate reserve | measured reserve | Don't conflate with patch stock |
| state | ! | low / critical condition | explicit threshold and value | Show numerical reading |
| communication | ⚑ | signal **sent** | emitted signal action | Never means delivered |
| communication | ⌁ | invitation **heard** | receiver observation `Heard[p]>0` | Never infer other receivers heard |
| communication | ⌖ | advertised target | decoded observed patch index | Target, not agent position |
| action | ↘ | harvest action | selected action code 0–2 | A harvest may yield zero units |
| action | ♻ | restoration **attempt** | selected action code 3–5 | Not a success indicator |
| action | ◇ | rest / no-op | selected action code 9 | May include dead-agent default |
| action | → | movement | genuine movement event | Not currently in the commons trace |
| outcome | ✓ | successful joint restoration | `SuccessfulRepairs>0` | Aggregate when patch not logged |
| outcome | × | death | alive-to-dead transition | Do not use for failed attempts |
| outcome | ↑ | numeric increase | measured positive delta | Label metric and units |
| outcome | ↓ | numeric decrease | measured negative delta | Label metric and units |
| state | ∅ | scarcity | observed stock at configured threshold | Never presume absolute zero |
| provenance | [S] | scripted policy | replay manifest / known exporter mode | Controller type only |
| provenance | [L] | trained policy | replay manifest / known exporter mode | Does not imply autonomous communication |
| future | ⊕ | reproduction | recorded child with parent | Not implemented by commons |
| future | Δ | mutation | actual genotype before/after | Not implemented by commons |
| future | ⋈ | coordinated agents | verified group interaction | Do not infer merely from invitations |

## Palette

| Function | Suggested foreground | Background |
|---|---|---|
| neutral entity / structural | #DCEBE8 | #0E1419 |
| scripted actor | #E6C278 | #0E1419 |
| learned actor | #98ECBf | #0E1419 |
| messages / receipt | #C4ABF1 | #0E1419 |
| restorative actions | #8ED6BD | #0E1419 |
| resource depletion | #E8BD7F | #0E1419 |
| terminal failures / death | #F18D87 | #0E1419 |

Use text annotations so the same information is preserved in grayscale.

## Event grammar

`T{tick} A{seat}[S|L] {glyph} {verb} P{patch} · measurement/result`

Examples are **format examples, not observations from a real run**:

- `T12 A0[S] ⚑ signal P1 · sent (delivery unknown)`
- `T13 A2[L] ⌁ heard P1 · LocalStock=1`
- `T13 A2[L] ♻ restore attempt P1 · reward=-0.60`
- `T13 WORLD ✓ joint restoration · count=1 (patch not recorded)`
- `T14 A1[L] ↘ harvest P0 · reward=+1.70`

## Supported vs unsupported today

The learned trace currently records receiver observation, action, reward, resulting energy, net stock changes, death, and aggregate successful restoration count. A communication observation row records a *single* recipient's heard signal. The sender action row records **sent**, not delivered. Aggregate success does **not** identify the restored patch. No movement, reproduction, mutation, individual Q-value comparison, or continuous physics event is available in this trace format.

## Integration checklist

- [x] Trace-specific view separates observed invitation from sent signal.
- [x] Display scripted vs learned controller provenance only for an identified learned-replay export.
- [x] Show attempt vs success as separate glyphs.
- [x] Display text alongside symbols and avoid using a synthetic physics scene.
- [ ] Expose message delivery and sender Q state in an explicit versioned JSON trace.
- [ ] Record per-patch successful restoration (currently aggregate only).
- [ ] Browser automation and contrast/accessibility audit.
- [ ] Unity overlay based on actual physics and physiology events.
