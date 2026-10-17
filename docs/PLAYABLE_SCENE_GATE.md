# Playable scene gate — chemistry lab

**Entry point:** In Unity Editor use **Cells > Create Chemistry Lab Scene** and then press Play. This creates and saves `Assets/Scenes/ChemistryLab.unity`, with a microscope camera, ChemistryDemoBootstrap and ChemistryDemoView, and registers that scene as a build scene.

**CLI (licensed Unity Editor installed):**
```text
Unity -batchmode -quit -projectPath <repo-path> -executeMethod Cells.EditorTools.CellsSceneBuilder.CreateBatchScene -logFile -
```
No Editor runtime is present in the current chat container. The code is committed but not yet Editor-tested.

### Expected demonstration
- Two static placeholder organisms: Secretor and Grazer.
- Independently diffusing green nutrient and violet toxin concentration overlays.
- Tracked nutrient packets with seeded stochastic motion and a local grazer feeding site.
- Inspect selectable organisms for their current energy and membrane integrity.
- Press F to follow, mouse wheel to zoom, Escape to clear selection.

### Scientifically critical caveats
- This is a fixed-position **chemistry and inspection test fixture**, not a fully moving/evolving ecological sandbox.
- Field chemical samples and independently tracked packets are different substance pools.
- Toxicity and membrane integrity are scalar toy models and **not calibrated biological predictions**.
- The old v0.3 swimmer simulation remains separate; it must be merged after the chemistry fixture compiles.
- No Unity compile, Play Mode test, screenshot, or gameplay MP4 was produced for this gate.

### Acceptance checklist
1. Unity Editor imports all packages and compiles Cells.Runtime and Cells.EditorTests.
2. Generate ChemistryLab from the Cells menu, save the scene, enter Play Mode.
3. Both organisms render; toxin concentration spreads; nutrient packets move.
4. Selecting Grazer updates energy and integrity without exceptions.
5. Unity Test Runner completes EditMode tests.
6. Capture the scene with the Unity renderer; preserve evidence of commit SHA, Unity version and seed.
