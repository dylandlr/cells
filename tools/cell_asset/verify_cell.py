"""Validate actual exported FBXs in Blender, including skinning and morph targets."""
import bpy, json
from pathlib import Path
import numpy as np
root=Path.cwd()
out=root/'Assets'/'CellHero'
manifest=json.loads((out/'Documentation'/'manifest.json').read_text())
assert manifest['lods'][0]['triangles']>manifest['lods'][1]['triangles']>manifest['lods'][2]['triangles']>0
checks=[]
for lod in range(3):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    f=out/'Models'/('CellHero_LOD%d.fbx'%lod)
    assert f.exists() and f.stat().st_size>10000
    bpy.ops.import_scene.fbx(filepath=str(f))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    arms=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
    assert len(meshes)==7, ('renderer count',lod,len(meshes))
    assert len(arms)==2
    assert sum(len(a.data.bones) for a in arms)==24
    total=0
    for o in meshes:
        assert len(o.data.vertices)>0 and len(o.data.uv_layers)>0, o.name
        assert len(o.data.materials)>0, o.name
        coords=np.array([v.co[:] for v in o.data.vertices])
        assert np.isfinite(coords).all(),o.name
        o.data.calc_loop_triangles()
        total+=len(o.data.loop_triangles)
        if o.name.startswith('Flagellum'):
            assert any(m.type=='ARMATURE' for m in o.modifiers),o.name
            assert len(o.vertex_groups)==12,o.name
            for v in o.data.vertices:
                assert abs(sum(g.weight for g in v.groups)-1)<.002,(o.name,v.index)
    membrane=next(o for o in meshes if o.name.startswith('Membrane_'))
    assert membrane.data.shape_keys is not None
    names=[b.name for b in membrane.data.shape_keys.key_blocks]
    assert any(n.endswith('MembraneDrift')for n in names),names
    assert any(n.endswith('OsmoticSwell')for n in names),names
    dims=list(membrane.dimensions)
    assert min(dims)>2 and max(dims)<4.3,('body size',dims)
    checks.append({'lod':lod,'triangles_after_fbx_import':total,'mesh_renderers':len(meshes),'bones':24,'normalized_skin_weights':True,'morphs_preserved':True,'finite_geometry':True})
for name in ['Cell_SurfaceTint.png','Cell_DetailNormal.png','Cell_Mask.png']:
    im=bpy.data.images.load(str(out/'Textures'/name),check_existing=False)
    assert tuple(im.size)==(4096,4096)
report={'result':'passed','checks':checks,'texture_dimensions_verified':True,'validation_scope':'Blender FBX import, topology, skin weights, morphs, UV presence, scale, PBR map dimensions. Unity Editor compilation and Play Mode are not yet verified.'}
(out/'Documentation'/'validation.json').write_text(json.dumps(report,indent=2))
print('VALIDATION_PASSED',json.dumps(report),flush=True)
