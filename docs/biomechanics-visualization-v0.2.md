# Microscope biomechanics visual milestone v0.2

## Added runtime visuals
- `CellMechanicsOverlay2D` creates line segments anchored to actual
  `SoftCellBody2D.Node` positions. Blue vectors use measured local node
  velocity and orange/red outward traces use `StressAt`. These are
  diagnostic *velocity and strain indicators*, **not calibrated force
  measurements**. No synthetic impulses, fluid mass or chemical resources
  are introduced.
- `CellDivisionPreview2D` follows opposed real membrane nodes and draws
  a proposed division plane. A separate track starts at the measured
  `InternalCompartments2D.Position(0)` and indicates a hypothetical
  nucleoid segregation direction. **The line does not move the nucleus or
  pinch the cell**. It is not a division solver.
- When `enableExperimentalDivision` is set before `Build`, demo
  organisms with soft meshes receive both diagnostic components.
  `ExperimentalDivisionDemo` exposes the division guide based on
  actual biomass fraction approaching the valid division threshold.
- Play Mode smoke test authored for line construction and position
  count. Unity Editor execution and rendering **not verified**.

## Known limitations
The experiment still directly spawns a prepared daughter instead of
executing a mechanical cytokinesis model. No molecular force arrows,
membrane scission, chromosome segregation, or conservation of all
intracellular module inventories have been implemented. The overlays
are deliberately invisible in standard mode to avoid suggesting more
physical sophistication than the solver provides.

## Next visual/physical milestone
1. Add a material-conserving split of the authoritative membrane mesh;
   have constriction forces influence the actual shape solver.
2. Move and partition internal compartments, coupled to real
   replicated pseudo-genetic material quantities and cell geometry.
3. Add transport event visuals at anchored membrane pores, preserving
   correct event timestamps and mass-flow semantics.
4. Render flagellar hydrodynamic disturbances from measured fluid-grid
   velocity rather than from invented forces.
5. Run Play Mode smoke, profiler stress tests, and close visual inspection
   before enabling continuous evolutionary population reproduction.

Source discipline: User-supplied *Substrate: Emergence* artifacts are
gameplay/visual references only, not editable Unity source. Current
visualization is an independent approximation, not proof of cellular
viability or calibrated biophysics.
