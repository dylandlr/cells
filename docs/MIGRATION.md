# Legacy archive migration

The earlier conversation describes a **Causal Microscope v0.3** ZIP. It is not attached
to the accessible chat, present under the synced project sources, or present in the
inspected Downloads, Documents, Desktop, OneDrive, PROJECTS, and starter Unity project
locations. No archive migration was performed in this pass. The standalone chemistry
demo is built from the code already on `feat/substance-ontology`.

When the archive becomes available:

1. Inventory archive paths and preserve provenance/version information.
2. Import useful source/assets into a separate legacy area, preserving GUIDs and checking conflicts.
3. Compare transport, physiology, sensor and rendering responsibilities with the working demo.
4. Integrate incrementally against the existing EditMode and PlayMode tests.
5. Record new runtime evidence separately from older target-footage renders.

Do not overwrite the working project settings or reintroduce duplicate chemistry
systems without verifying their units, material accounting and update ownership.
