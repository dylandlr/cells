# Cell Hero — 3D Unity asset

A stylized, anatomy-rich flagellated eukaryotic cell, made from actual meshes. This is a generalized illustrative organism, not a reconstruction of one named species.

## Import

1. Import CellHero-3D.unitypackage, or copy Assets/CellHero into your Unity project.
2. Allow Unity to finish importing and compiling. The setup script creates the materials and prefab on first import.
3. If needed, run Tools → Cells → 3D Cell → Build Hero Prefab.
4. Run Tools → Cells → 3D Cell → Open Preview Scene and enter Play Mode.

For this repository, switch to feat/cell-hero-3d and open the project in your installed Unity 6 editor. The game code comes from feat/substance-ontology; this branch adds a separate hero asset and preview.

## Contents

- Three FBX LODs with UVs, material slots, two 12-bone flagellar rigs, and membrane blend shapes.
- 4K tileable surface tint, tangent normal and packed mask maps.
- A custom Fresnel membrane shader, plus pipeline Lit materials for the organelles.
- A component for slow flagellar waves, membrane drift, osmotic swelling and inspection visibility.
- A Unity Editor prefab and preview-scene builder.
- An editable Blender 4.3.2 source file, portable GLB, rendered stills and a rotating video.
- A deterministic source generator, FBX validation and package checksums.

The Blender file contains individually named anatomy on a hidden editable collection. Enable “Cell | Editable anatomy” and hide the LOD collections to edit the source without duplicate geometry.

## Scale and physiology

The model origin is the cell body's center. One Unity unit represents 10 micrometers; the cell body is about 40 micrometers long. This is a visualization scale, not a calibrated dynamics model.

MembraneDrift is a small visual deformation. OsmoticSwell reaches a 10% geometric expansion at full weight. CellHeroAnimation does not calculate metabolism, osmotic pressure, propulsion, collision forces or damage. A simulation can drive its public controls with measured state. Flagellar animation is visual and does not propel the cell.

Inspection mode hides the outer membrane and cytoplasm so organelles remain visible. It is an augmented microscope view.

## Materials and performance

The asset includes nucleus/envelope/nucleolus, chromatin, nuclear pores, rough ER, Golgi stacks and buds, mitochondria with modeled cristae, vacuoles, lipid droplets, ribosomes, cytoskeletal bundles and surface proteins.

The membrane shader approximates translucent membrane optics with Fresnel opacity and microdetail normals. It does not simulate volumetric scattering or refraction. Neutral shared textures provide reusable microdetail; this is not a unique high-poly-to-low-poly bake.

The packed mask is R: metallic (zero), G: microdetail occlusion, B: unused, A: smoothness. The alpha is suitable for Standard/URP metallic-smoothness sampling; HDRP uses the full mask. No light or shadow is baked into the base tint.

LOD counts and material slots are reported in Documentation/manifest.json. Transparency overdraw and skinned appendages need profiling at your intended population count. A close-up hero model is not the default mesh for thousands of simultaneous cells. Shadows are disabled on the prefab to keep the translucent interior legible. Lower LODs preserve the rig and membrane controls; LOD culling occurs below 5.5% screen height.

Built-in is the current repository's pipeline. The setup script has Standard, URP and HDRP material paths; in-engine appearance and compilation have not yet been verified in a connected licensed Unity Editor. The custom membrane shader is an art-directed forward pass and should be checked in the selected pipeline.

## Evidence

Documentation/validation.json records validation performed on the exported FBXs, including normalized skin weights, finite geometry, morph preservation, scale, UVs and map dimensions. Blender previews are renders of the delivered 3D meshes; they are not Unity gameplay captures.

The Blender/asset build workflow requires no Unity license. Existing Unity Edit Mode/Play Mode CI remains a separate check and needs its configured licensing.
