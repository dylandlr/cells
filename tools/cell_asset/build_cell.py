"""Deterministic hero cell asset builder. Run in Blender 4.3.2, not system Python."""
import bpy, math, json, os, random
import numpy as np
from pathlib import Path
from mathutils import Vector
random.seed(1846)
ROOT=Path.cwd()
OUT=ROOT/'Assets'/'CellHero'
SOURCE=ROOT/'art'/'cell-hero'
PREVIEW=SOURCE/'Previews'
for p in [OUT/'Models',OUT/'Textures',OUT/'Documentation',SOURCE,PREVIEW]:
    p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.cycles.use_adaptive_sampling=True
scene.cycles.adaptive_threshold=.06
scene.cycles.max_bounces=8
scene.cycles.transparent_max_bounces=16
scene.render.fps=24
scene.frame_start=1
scene.frame_end=96
scene.view_settings.view_transform='AgX'
scene.world.color=(.065,.085,.1)
parts=[]
shells=[]
interior=[]
rig_objects=[]

# Neutral tileable maps are shared across all pigments. The mask is HDRP RGBA:
# metallic / microdetail occlusion / unused detail mask / smoothness.
N=4096
x=np.linspace(0,math.tau,N,endpoint=False,dtype=np.float32)
U,V=np.meshgrid(x,x)
h=(.32*np.sin(U*37+np.sin(V*19)*1.4)+.26*np.cos(V*43+np.cos(U*13))+
   .12*np.sin(U*107+V*83)+.08*np.cos(U*283-V*191)+.03*np.sin(U*701+V*503))
h=(h-h.min())/(h.max()-h.min())
def image(name,pix,color='Non-Color'):
    im=bpy.data.images.new(name,width=N,height=N,alpha=True)
    im.colorspace_settings.name=color
    im.pixels.foreach_set(np.ascontiguousarray(pix,dtype=np.float32).ravel())
    im.filepath_raw=str(OUT/'Textures'/name)
    im.file_format='PNG'
    im.save()
    return im
a=np.ones((N,N,4),dtype=np.float32)
a[:,:,:3]=(.84+.15*h)[:,:,None]
detail=image('Cell_SurfaceTint.png',a,'sRGB')
dy=(np.roll(h,-1,0)-np.roll(h,1,0))*1.25
dx=(np.roll(h,-1,1)-np.roll(h,1,1))*1.25
normal=np.stack((-dx,-dy,np.ones_like(h)),axis=2)
normal/=np.linalg.norm(normal,axis=2)[:,:,None]
a[:,:,:3]=normal*.5+.5
normal_image=image('Cell_DetailNormal.png',a)
a[:,:,0]=0; a[:,:,1]=.93+.07*h; a[:,:,2]=0; a[:,:,3]=.48+.34*h
mask_image=image('Cell_Mask.png',a)
del a,U,V,h,dx,dy,normal

material_specs=[]
def material(name,rgb,rough=.35,alpha=1,metal=0,membrane=False):
    m=bpy.data.materials.new(name)
    m.use_nodes=True
    n=m.node_tree.nodes
    l=m.node_tree.links
    bs=n.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*rgb,1)
    bs.inputs['Roughness'].default_value=rough
    bs.inputs['Metallic'].default_value=metal
    bs.inputs['Alpha'].default_value=alpha
    bs.inputs['IOR'].default_value=1.37
    bs.inputs['Subsurface Weight'].default_value=.06 if alpha==1 else 0
    bs.inputs['Subsurface Radius'].default_value=(.15,.08,.04)
    t=n.new('ShaderNodeTexImage');t.image=detail
    mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
    mix.inputs[2].default_value=(*rgb,1);l.new(t.outputs['Color'],mix.inputs[1]);l.new(mix.outputs[0],bs.inputs['Base Color'])
    norm=n.new('ShaderNodeTexImage');norm.image=normal_image
    nm=n.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.33
    l.new(norm.outputs['Color'],nm.inputs['Color']);l.new(nm.outputs['Normal'],bs.inputs['Normal'])
    if membrane:
        fres=n.new('ShaderNodeFresnel');fres.inputs['IOR'].default_value=1.37
        rem=n.new('ShaderNodeMapRange');rem.inputs['To Min'].default_value=.025;rem.inputs['To Max'].default_value=.54
        l.new(fres.outputs[0],rem.inputs['Value']);l.new(rem.outputs[0],bs.inputs['Alpha'])
        bs.inputs['Coat Weight'].default_value=.25
        bs.inputs['Coat Roughness'].default_value=.12
    m.diffuse_color=(*rgb,alpha)
    if alpha<1:m.surface_render_method='DITHERED'
    material_specs.append({'name':name,'color':list(rgb),'alpha':alpha,'roughness':rough,'metallic':metal,'membrane':membrane})
    return m
membrane=material('Cell_Membrane',(.055,.4,.355),.23,.18,membrane=True)
cytoplasm=material('Cell_Cytoplasm',(.095,.32,.28),.55,.025)
nuclear=material('Cell_NucleusEnvelope',(.33,.18,.43),.29,.22)
chromatin=material('Cell_Chromatin',(.24,.11,.32),.47)
nucleolus=material('Cell_Nucleolus',(.11,.027,.16),.29)
er_mat=material('Cell_ER',(.62,.54,.35),.34)
golgi_mat=material('Cell_Golgi',(.72,.33,.10),.31)
mito_shell=material('Cell_MitochondriaEnvelope',(.53,.2,.115),.24,.31)
mito_fold=material('Cell_Cristae',(.72,.31,.15),.3)
vesicle_shell=material('Cell_VesicleEnvelope',(.16,.46,.37),.23,.27)
lipid=material('Cell_LipidDroplet',(.65,.47,.115),.26)
ribosome_mat=material('Cell_Ribosome',(.23,.11,.31),.38)
filament=material('Cell_Cytoskeleton',(.3,.45,.39),.42)
flag_mat=material('Cell_Flagellum',(.09,.38,.34),.26)
protein=material('Cell_MembraneProtein',(.1,.36,.35),.36)

def finish(o,name,mat,category):
    o.name=name
    o.data.materials.append(mat)
    for p in o.data.polygons:p.use_smooth=True
    bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    parts.append(o)
    category.append(o)
    return o
def sphere(name,loc,scale,mat,category=interior,segments=32,rings=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=loc)
    o=bpy.context.object;o.scale=scale
    return finish(o,name,mat,category)
def tube(name,points,r,mat,category=interior,sides=8,elliptic=1,radii=None):
    # Swept tube with tangent-continuous frames, explicit UV0, and closed ends.
    pts=[Vector(p) for p in points]
    verts=[];faces=[];uv=[]
    prev=Vector((0,0,1))
    for i,p in enumerate(pts):
        tan=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized()
        side=tan.cross(prev)
        if side.length<.01:side=tan.cross(Vector((0,1,0)))
        side.normalize();up=side.cross(tan).normalized();prev=up
        rr=radii[i] if radii is not None else r
        for j in range(sides):
            a=j*math.tau/sides
            v=p+side*(math.cos(a)*rr)+up*(math.sin(a)*rr*elliptic)
            verts.append(tuple(v))
    for i in range(len(pts)-1):
        for j in range(sides):faces.append((i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j))
    faces.extend([tuple(reversed(range(sides))),tuple(range((len(pts)-1)*sides,len(pts)*sides))])
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new(name,me);scene.collection.objects.link(o)
    lay=me.uv_layers.new(name='UV0')
    for p in me.polygons:
        for li in p.loop_indices:
            vi=me.loops[li].vertex_index
            lay.data[li].uv=((vi%sides)/sides,(vi//sides)/(len(pts)-1))
    return finish(o,name,mat,category)
def radial_points(center,radius,count,start=0,end=math.tau,zam=.0):
    return [(center[0]+radius*math.cos(a),center[1]+radius*math.sin(a),center[2]+zam*math.sin(3*a))
            for a in np.linspace(start,end,count)]
def triangulate(o):
    bpy.context.view_layer.objects.active=o
    mod=o.modifiers.new('Stable export triangles','TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=mod.name)
def drift_shapes(o):
    # Shapes are visual controls; physiology belongs to the simulation.
    o.shape_key_add(name='Basis')
    k=o.shape_key_add(name='MembraneDrift')
    for v in k.data:
        p=v.co.copy();f=.026*math.sin(p.x*2.7+p.y*1.9+p.z*3.2)
        v.co=p+Vector((p.x*.017+f,p.y*.012-f*.3,p.z*.019+f*.25))
    k=o.shape_key_add(name='OsmoticSwell')
    for v in k.data:v.co*=1.10

print('BUILDING CELL',flush=True)
body=sphere('Membrane',(0,0,0),(1.42,1.99,1.18),membrane,shells,96,64)
for v in body.data.vertices:
    p=v.co
    fac=1+.016*math.sin(p.y*5+p.x*3)*math.cos(p.z*4)+.008*math.sin(p.x*13+p.y*7)
    p.x*=fac*(1-.07*max(p.y,0))
    p.z*=fac
triangulate(body);drift_shapes(body)
cyto=sphere('Cytoplasm',(0,0,0),(1.385,1.94,1.15),cytoplasm,shells,64,40)

center=Vector((-.32,-.30,.05))
ns=sphere('NucleusEnvelope',center,(.65,.72,.59),nuclear,shells,64,40)
sphere('Nucleolus',center+Vector((-.08,.08,.02)),(.19,.21,.18),nucleolus,segments=40,rings=24)
for i in range(28):
    seed=np.array([random.uniform(-.5,.5) for _ in range(3)])
    coords=[]
    for j in range(22):
        t=j/21
        coords.append(tuple(center+Vector((.47*math.sin(t*5+i*1.7)*math.cos(i),.53*math.cos(t*4+i*.83)*math.sin(i+.7),.40*math.sin(t*6+i*.37)))))
    tube('Chromatin_%02d'%i,coords,.009,chromatin,sides=6)
# Nuclear pores on the envelope, with a slight surface offset.
for i in range(64):
    zz=1-2*(i+.5)/64;a=i*2.399963
    r=math.sqrt(1-zz*zz)
    pos=center+Vector((.657*r*math.cos(a),.728*r*math.sin(a),.598*zz))
    sphere('NuclearPore_%02d'%i,pos,(.017,.017,.017),er_mat,segments=12,rings=8)

# Sheet-like ER cisternae curl around the nucleus.
for i in range(9):
    rr=.78+i*.039
    coords=radial_points((center.x,center.y,.0+(i-4)*.075),rr,58,-.55,2.37,zam=.024)
    tube('ER_Cisterna_%02d'%i,coords,.048,er_mat,sides=10,elliptic=.43)
    for j in range(0,len(coords),8):
        p=Vector(coords[j])+Vector((0,0,.027))
        sphere('ER_BoundRibosome',p,(.018,.018,.018),ribosome_mat,segments=10,rings=6)

# Golgi: flattened, slightly irregular crescents with budding vesicles.
for i in range(7):
    coords=[]
    for j in range(42):
        t=j/41
        a=-1.22+t*2.44
        coords.append((.64+.36*math.cos(a),-.02+.47*math.sin(a),-.23+i*.084+.026*math.sin(t*4+i)))
    tube('Golgi_Cisterna_%02d'%i,coords,.051,golgi_mat,sides=10,elliptic=.37)
for i in range(11):
    sphere('Golgi_BuddingVesicle',(.80+random.uniform(-.15,.16),.45+random.uniform(-.1,.2),-.15+random.random()*.55),(.039,)*3,lipid,segments=16,rings=10)

# Independent mitochondrial shells and genuinely modeled internal cristae.
mito_positions=[(-.78,-1.05,.3),(.64,-1.14,.16),(.60,.98,.25),(-.73,.88,.1),(.96,.32,-.12),(-.88,-.1,-.35),(.1,1.39,-.25),(.25,-.98,-.53)]
for i,pos in enumerate(mito_positions):
    rot=Vector((random.uniform(-.3,.3),random.uniform(-.3,.3),random.uniform(-1,1)))
    sh=sphere('Mitochondrion_%02d'%i,pos,(.35,.16,.17),mito_shell,shells,40,24)
    sh.rotation_euler=rot
    # Transform local folds into the same mitochondrion frame.
    R=sh.rotation_euler.to_matrix()
    for j in range(8):
        xx=-.245+j*.07
        cr=[]
        for k in range(18):
            a=-.92*math.pi+k/17*1.84*math.pi
            p=Vector((xx+.012*math.sin(a*2),.115*math.cos(a),.122*math.sin(a)))
            cr.append(tuple(Vector(pos)+R@p))
        tube('Mitochondrion_%02d_Crista_%02d'%(i,j),cr,.011,mito_fold,sides=6)

# Vacuoles, lipid stores and dispersed vesicles, with a clear cytoplasmic margin.
for i,(pos,rad) in enumerate([((.25,.8,-.3),.23),((-.52,1.1,-.35),.17),((.55,-.66,-.10),.14),((-.25,-1.28,.22),.16)]):
    sphere('Vacuole_%02d'%i,pos,(rad,rad*.9,rad),vesicle_shell,shells,32,20)
for i in range(36):
    while True:
        p=Vector((random.uniform(-1.03,1.03),random.uniform(-1.5,1.5),random.uniform(-.72,.72)))
        if (p.x/1.15)**2+(p.y/1.75)**2+(p.z/.92)**2<.86 and (p-center).length>.79:break
    r=random.uniform(.027,.077)
    sphere('LipidDroplet_%02d'%i,p,(r,r*.95,r),lipid,segments=20,rings=12)
for i in range(65):
    p=Vector((random.uniform(-.95,.95),random.uniform(-1.45,1.45),random.uniform(-.62,.62)))
    if (p-center).length<.75:continue
    sphere('FreeRibosome_%02d'%i,p,(.013,)*3,ribosome_mat,segments=8,rings=6)
# Filament bundles have useful topology and remain within the cell envelope.
for i in range(17):
    a=random.uniform(0,math.tau)
    cr=[]
    for j in range(26):
        t=j/25
        cr.append((.98*math.cos(a)*math.sin(t*math.pi),-1.55+t*3.05,.61*math.sin(a)*math.sin(t*math.pi)+.06*math.sin(t*8+i)))
    tube('CytoskeletalBundle_%02d'%i,cr,.009,filament,sides=6)
# Membrane proteins distributed by a Fibonacci sphere, not a tiled dot decal.
for i in range(82):
    z=1-2*(i+.5)/82;a=i*2.399963;r=math.sqrt(1-z*z)
    p=Vector((1.426*r*math.cos(a),1.997*r*math.sin(a),1.187*z))
    sphere('MembraneProtein_%02d'%i,p,(.026,.032,.028),protein,segments=12,rings=8)

# Two smooth tapered appendages, each with a 12-bone deformation chain.
def flagellum(letter,sign,length):
    pts=[]
    rings=105
    for j in range(rings):
        t=j/(rings-1)
        pts.append((sign*(.14+.60*math.sin(t*3.2)+.42*t),1.76+length*t,.09+sign*.16*math.sin(t*5)))
    rads=[.031*(1-j/(rings-1))+.004 for j in range(rings)]
    mesh=tube('Flagellum'+letter,pts,.035,flag_mat,[],sides=12,radii=rads)
    parts.remove(mesh)
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0))
    arm=bpy.context.object;arm.name='FlagellumRig'+letter
    for b in list(arm.data.edit_bones):arm.data.edit_bones.remove(b)
    for k in range(12):
        b=arm.data.edit_bones.new('Flagellum%s_%02d'%(letter,k))
        b.head=pts[round(k/12*(rings-1))]
        b.tail=pts[round((k+1)/12*(rings-1))]
        if k:b.parent=arm.data.edit_bones.get('Flagellum%s_%02d'%(letter,k-1));b.use_connect=True
    bpy.ops.object.mode_set(mode='OBJECT')
    for k in range(12):mesh.vertex_groups.new(name='Flagellum%s_%02d'%(letter,k))
    for j in range(rings):
        t=j/(rings-1)*12
        k=min(int(t),11);f=t-int(t)
        for vi in range(j*12,(j+1)*12):
            mesh.vertex_groups[k].add([vi],1-f if k<11 else 1,'REPLACE')
            if k<11:mesh.vertex_groups[k+1].add([vi],f,'REPLACE')
    mod=mesh.modifiers.new('Flagellar skeleton','ARMATURE');mod.object=arm
    mesh.parent=arm
    for b in arm.pose.bones:
        b.rotation_mode='XYZ'
        k=int(b.name[-2:])
        for frame in range(1,97,4):
            phase=(frame-1)/24*math.tau*.35-k*.55+(0 if letter=='A' else 1.2)
            b.rotation_euler=(.025*math.sin(phase),0,.04*math.sin(phase))
            b.keyframe_insert(data_path='rotation_euler',frame=frame)
    rig_objects.extend([arm,mesh])
    return arm,mesh
rigA,flagA=flagellum('A',1,3.25)
rigB,flagB=flagellum('B',-1,2.65)
scene.frame_set(1)
print('EDITABLE_PARTS',len(parts),flush=True)
source=bpy.data.collections.new('Cell | Editable anatomy')
scene.collection.children.link(source)
for o in parts+rig_objects:
    for c in list(o.users_collection):c.objects.unlink(o)
    source.objects.link(o)
source.hide_render=True
source.hide_viewport=True

def duplicate(o,coll):
    cp=o.copy();cp.data=o.data.copy();coll.objects.link(cp);return cp
def join(objs,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    bpy.ops.object.join()
    o=bpy.context.object;o.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    return o
def smooth_decimate(o,ratio):
    bpy.context.view_layer.objects.active=o
    if ratio<1:
        d=o.modifiers.new('LOD reduction','DECIMATE');d.ratio=ratio;d.use_collapse_triangulate=True
        bpy.ops.object.modifier_apply(modifier=d.name)
    triangulate(o)
def count(o):
    o.data.calc_loop_triangles()
    return len(o.data.loop_triangles)
lod_objects=[]
manifest={'asset':'Cell Hero / flagellated eukaryote','version':'1.0.0','biological_scale':'1 Unity unit = 10 micrometers','pivot':'cell body center','editable_anatomy_parts':len(parts),'maps':{'resolution':[4096,4096],'normal_convention':'OpenGL tangent space','mask_channels':'R metallic; G microdetail occlusion; B unused; A smoothness'},'lods':[],'editor_validation':'Not run: Unity Editor requires a connected licensed environment.'}
for idx,ratio in enumerate([1,.44,.14]):
    coll=bpy.data.collections.new('Cell | LOD%d'%idx);scene.collection.children.link(coll)
    objects=[]
    for src in [body,cyto,ns]:
        cp=duplicate(src,coll)
        cp.name=src.name+'_LOD%d'%idx
        if cp.data.shape_keys:
            for k in reversed(range(len(cp.data.shape_keys.key_blocks))):cp.shape_key_remove(cp.data.shape_keys.key_blocks[k])
        smooth_decimate(cp,ratio)
        if src==body:drift_shapes(cp)
        objects.append(cp)
    solid=join([duplicate(o,coll)for o in interior],'Organelles_LOD%d'%idx)
    smooth_decimate(solid,ratio);objects.append(solid)
    thin=join([duplicate(o,coll)for o in shells if o not in [body,cyto,ns]],'OrganelleShells_LOD%d'%idx)
    smooth_decimate(thin,ratio);objects.append(thin)
    for arm,mesh in [(rigA,flagA),(rigB,flagB)]:
        ac=duplicate(arm,coll);ac.name=arm.name+'_LOD%d'%idx
        mc=duplicate(mesh,coll);mc.name=mesh.name+'_LOD%d'%idx;mc.parent=ac
        mc.modifiers.get('Flagellar skeleton').object=ac
        objects.extend([ac,mc])
    # Unique lightmap UVs for static meshes; transparent and skinned parts use UV0.
    for o in objects:
        if o.type!='MESH':continue
        if len(o.data.uv_layers)==0:o.data.uv_layers.new(name='UV0')
        if not o.data.shape_keys and not any(m.type=='ARMATURE'for m in o.modifiers):
            o.data.uv_layers.new(name='UV1_Lightmap');o.data.uv_layers.active_index=1
            bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
            bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
            bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.015,scale_to_bounds=True)
            bpy.ops.object.mode_set(mode='OBJECT');o.data.uv_layers.active_index=0
    # Export reference skeletal rest pose. Unity's component supplies slow idle motion.
    for o in objects:
        if o.type=='ARMATURE':
            o.animation_data_clear()
            for b in o.pose.bones:b.rotation_euler=(0,0,0)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/('CellHero_LOD%d.fbx'%idx)),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,use_mesh_modifiers=False,add_leaf_bones=False,mesh_smooth_type='FACE',use_tspace=True,bake_anim=False,path_mode='STRIP')
    row={'level':idx,'triangles':sum(count(o)for o in objects if o.type=='MESH'),'vertices':sum(len(o.data.vertices)for o in objects if o.type=='MESH'),'mesh_renderers':sum(o.type=='MESH'for o in objects),'material_slots':sum(len(o.data.materials)for o in objects if o.type=='MESH'),'bones':sum(len(o.data.bones)for o in objects if o.type=='ARMATURE'),'blend_shapes':['MembraneDrift','OsmoticSwell']}
    manifest['lods'].append(row);lod_objects.append((coll,objects))
    coll.hide_render=idx>0;coll.hide_viewport=idx>0
print('LOD_STATS',json.dumps(manifest['lods']),flush=True)
(OUT/'Documentation'/'manifest.json').write_text(json.dumps(manifest,indent=2))
(OUT/'Documentation'/'materials.json').write_text(json.dumps({'materials':material_specs},indent=2))
# GLB uses the exact LOD0 geometry and materials for a portable viewer.
bpy.ops.object.select_all(action='DESELECT')
for o in lod_objects[0][1]:o.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(SOURCE/'CellHero_Preview.glb'),export_format='GLB',use_selection=True,export_yup=True,export_texcoords=True,export_normals=True,export_tangents=True,export_animations=False)
# Animated Blender preview bones retain the source rest animation on LOD0.
for src in [rigA,rigB]:
    dst=next(o for o in lod_objects[0][1]if o.name==src.name+'_LOD0')
    dst.animation_data_create();dst.animation_data.action=src.animation_data.action.copy()
# Studio rendering is separate from all exported geometry.
studio=bpy.data.collections.new('Studio | Preview only');scene.collection.children.link(studio)
def studio_move(o):
    for c in list(o.users_collection):c.objects.unlink(o)
    studio.objects.link(o)
def area(name,pos,power,color,size):
    bpy.ops.object.light_add(type='AREA',location=pos)
    o=bpy.context.object;o.name=name;o.data.energy=power;o.data.color=color;o.data.shape='DISK';o.data.size=size
    o.rotation_euler=(Vector((0,.9,0))-o.location).to_track_quat('-Z','Y').to_euler();studio_move(o)
area('Neutral softbox',(-3,-4,7),1700,(.78,.91,1),5)
area('Warm rim',(4,3,5),1550,(1,.72,.44),4)
area('Blue fill',(-4,5,2),1050,(.36,.71,1),4)
bpy.ops.object.camera_add(location=(5,-6,9))
cam=bpy.context.object;cam.name='Microscope preview camera';studio_move(cam);scene.camera=cam
cam.data.type='ORTHO';cam.data.ortho_scale=8.7
target=Vector((0,1.05,0))
cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
# A world background keeps the asset legible without inserting a mesh ground.
scene.render.film_transparent=False
scene.render.resolution_x=1536;scene.render.resolution_y=1536;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.image_settings.color_mode='RGBA'
for im in [detail,normal_image,mask_image]:
    im.filepath='//'+os.path.relpath(str(OUT/'Textures'/im.name),SOURCE)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'CellHero_Master.blend'),compress=True)
for name,pos,aim,scale in [
    ('CellHero_Hero.png',(3,-5,9),(0,1.10,0),8.5),
    ('CellHero_Detail.png',(3,-4,7),(0,-.25,0),4.45),
    ('CellHero_Reverse.png',(-5,4,7),(0,1.10,0),8.5)]:
    cam.location=pos;cam.rotation_euler=(Vector(aim)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
    scene.frame_set(1);scene.cycles.samples=32;scene.render.filepath=str(PREVIEW/name)
    print('RENDER',name,flush=True);bpy.ops.render.render(write_still=True)
# An inspection render hides only the outer membrane/cytoplasm; it preserves anatomy.
for o in lod_objects[0][1]:
    if o.name.startswith(('Membrane_','Cytoplasm_')):o.hide_render=True
cam.location=(3,-4,7);cam.rotation_euler=(Vector((0,-.1,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=4.5
scene.render.filepath=str(PREVIEW/'CellHero_Inspection.png');bpy.ops.render.render(write_still=True)
for o in lod_objects[0][1]:o.hide_render=False
scene.render.resolution_x=640;scene.render.resolution_y=640;scene.cycles.samples=10
cam.data.ortho_scale=8.5
frames=PREVIEW/'turntable';frames.mkdir(exist_ok=True)
for f in range(48):
    angle=f/48*math.tau
    cam.location=(math.sin(angle)*6,1.05-math.cos(angle)*6,8)
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.frame_set(1+f*2)
    scene.render.filepath=str(frames/('%04d.png'%f))
    bpy.ops.render.render(write_still=True)
print('CELL_BUILD_COMPLETE',flush=True)
