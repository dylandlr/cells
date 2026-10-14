"""Package generated assets with stable GUID metadata and a native Unity import archive."""
import hashlib, io, json, tarfile, zipfile
from pathlib import Path
root=Path.cwd();assets=root/'Assets'/'CellHero';source=root/'art'/'cell-hero';dist=root/'dist'/'cell-hero'
dist.mkdir(parents=True,exist_ok=True)
def guid(p):return hashlib.md5(('cells-hero-v1/'+p.as_posix()).encode()).hexdigest()
items=[assets]+sorted(assets.rglob('*'))
for p in items:
    if p.suffix=='.meta':continue
    rel=p.relative_to(root)
    m=Path(str(p)+'.meta')
    if not m.exists():
        text='fileFormatVersion: 2\nguid: '+guid(rel)+'\n'
        if p.is_dir():text+='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
        m.write_text(text)
archive=dist/'CellHero-3D.unitypackage'
with tarfile.open(archive,'w:gz')as tar:
    for p in [assets]+sorted(assets.rglob('*')):
        if p.suffix=='.meta':continue
        rel=p.relative_to(root);g=guid(rel)
        for suffix,data in [('pathname',rel.as_posix().encode()),('asset.meta',Path(str(p)+'.meta').read_bytes())]:
            ti=tarfile.TarInfo(g+'/'+suffix);ti.size=len(data);tar.addfile(ti,io.BytesIO(data))
        if p.is_file():tar.add(p,arcname=g+'/asset')
with zipfile.ZipFile(dist/'CellHero-3D-Asset.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6)as z:
    for base in [assets,source,root/'tools'/'cell_asset']:
        for p in sorted(base.rglob('*')):
            if not p.is_file()or p.suffix=='.blend1'or 'turntable' in p.parts or '__pycache__'in p.parts:continue
            z.write(p,p.relative_to(root))
    z.write(archive,archive.name)
with tarfile.open(archive)as t:
    names=t.getnames();assert any(n.endswith('/asset') for n in names)
checks=[]
for p in sorted(dist.iterdir()):
    if p.is_file():
        assert p.stat().st_size<95*1024*1024,('GitHub file size',p.name,p.stat().st_size)
        checks.append({'file':p.name,'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(dist/'checksums.json').write_text(json.dumps(checks,indent=2))
print('PACKAGES_READY',json.dumps(checks))
