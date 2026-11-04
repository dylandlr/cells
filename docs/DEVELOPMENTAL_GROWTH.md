# Paid cellular development, first integration

Inspired by Earth: Rogue Origins OpenMorphTissue at e958a40:
local inherited growth rules and neighboring material influence. This adaptation
uses the existing 32 membrane regions of ONE biological cell, not a tissue grid.
It does not port the ERO provisional energy replenishment or candidate fate labels.

The focused BuildPlayableDemo enables developmental growth. Founders receive equal
8-unit energy and .5-unit internal nutrient reserves, with different directional
allocation biases (.15 and .9). Other physiological traits still differ; this is
not a controlled fitness comparison. Existing scenes/assays default to the old mode.

CellBiomassGrowth transfers nutrient to structural mass and pays energy first.
CellDevelopment records the spatial allocation of exactly that transferred mass.
Its records are allocation history, not additional stored matter. Proposals read
previous neighboring deposits, then all receive their fraction of paid growth.
Zero bias is an isotropic control. Bias and axis copy through OrganismGenome;
mutation of these new traits is not implemented yet.

SoftCellBody2D uses these deposits to reshape its rest boundary, normalizing polygon
area to the existing biomass-derived area target. The live mesh reads the actual
physical nodes. This changes shape constraints and contact geometry; the current
scalar hydrodynamic drag approximation does not yet resolve shape-dependent drag.

Development is disabled when experimental division is enabled: partitioning the
spatial allocation history and preserving daughter geometry requires a separate
contract. This release does not add compartments, differentiated functions,
multicellular growth, local chemical regulatory fields, or evolutionary selection.

Validation: structural verifier and whitespace checks passed. Four Unity EditMode
tests cover paid allocation, starvation/death, isotropic growth/inherited traits,
and resulting physical boundary/area. Unity execution and visual review are pending;
this is not a verified playable build. Run tools/Build-PlayableDemo.ps1 -Launch on
the activated Windows Editor, then inspect slow growth and toxin/repair behavior.
