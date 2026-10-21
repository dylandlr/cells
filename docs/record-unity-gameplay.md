# Recording a genuine cells Unity gameplay demo

Unity 6.x is required. This repository's branch must compile and run first;
no footage is claimed to have been captured by committing this script.

1. Open the cells demo scene in Unity.
2. Add the `DemoFrameCapture` component to the bootstrap GameObject (or
   another active object in the scene).
3. Enable `Capture On Start`. Set 30 frames/second and 20 seconds.
4. Optionally enable `ChemistryDemoBootstrap.enableExperimentalDivision`
   **before** entering Play Mode, to include the opt-in constriction/one-birth
   experiment. The demo may not naturally reach division within 20 seconds.
5. Start Play Mode and wait for the completion line in the Console.
6. Locate the logged output directory under `Application.persistentDataPath`.
7. With ffmpeg installed, run in the saved-frame directory:

```bash
ffmpeg -framerate 30 -i frame_%06d.png -c:v libx264 -pix_fmt yuv420p -crf 18 cells-unity-gameplay.mp4
```

This captures **actual Unity Game View pixels**, not procedurally fabricated
video. Capture resolution matches the game view. Frame capture can have high
disk, VRAM and CPU costs; avoid long runs until it is profiled.

Scientific caveats: The proposed division guide is still an illustrative
overlay; membrane constriction is an effective numerical actuation rather
than full cytokinesis; the one-birth experimental spawn does not complete
membrane scission. Gameplay footage should identify those distinctions.

Suggested shot list: membrane motion under flow, nutrient uptake, toxin
injury, gene-regulated flagellar activity, and opt-in experimental
constriction, followed by a daughter spawn if the growth threshold is met.
