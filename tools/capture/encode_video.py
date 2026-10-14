"""Caption real Unity camera frames using measured state; requires an ffmpeg executable."""
import argparse
import csv
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--ffmpeg', required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parents[2] / 'Artifacts'
rows = list(csv.DictReader((root / 'deforming-metrics.csv').open()))
assert len(rows) == 600, 'Expected the complete 20-second, 30-fps recording.'

def stamp(t):
    return f'{int(t)//3600}:{int(t)//60%60:02}:{t%60:05.2f}'

header = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 1080
WrapStyle: 2
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Text,Segoe UI,26,&H00EEE8E0,&H00FFFFFF,&H00120C07,&H90000000,0,0,0,0,100,100,0,0,1,1,0,7,0,0,0,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
events = []
def label(start, end, x, y, text, size=26, color='EEE8E0', alignment=7):
    tags = rf'{{\pos({x},{y})\an{alignment}\fs{size}\c&H{color}&}}'
    events.append(f'Dialogue: 0,{stamp(start)},{stamp(end)},Text,,0,0,0,,{tags}{text}\n')

label(0, 20, 40, 24, 'CELL CULTURE  /  Phase-contrast-inspired Unity view', 25)
label(0, 20, 1880, 26, 'NO BEATING  /  CHEMISTRY ACTIVE', 21, 'EEE8E0', 9)
for i in range(0, len(rows), 15):
    r = rows[i]
    t = i / 30
    end = t + .5
    phase = 'Nutrient uptake' if t < 4 else 'Local toxin challenge' if t < 10 else 'Neutralizing reagent added'
    label(t, end, 40, 1029, f'{t:04.1f} s   |   {phase}', 24)
    state = 'intact' if float(r['weakest']) > .99 else 'injured' if r['alive'] == 'True' else 'ruptured'
    label(t, end, 1880, 1029, f"Target: {state}   |   Leaked nutrient mass: {float(r['leaked']):.3f}", 23, 'EEE8E0', 9)

(root / 'microscope-captions.ass').write_text(header + ''.join(events), encoding='utf-8')
subprocess.run([args.ffmpeg, '-y', '-hide_banner', '-loglevel', 'warning', '-framerate', '30',
    '-i', 'deforming-frames/frame-%04d.png', '-vf', 'drawbox=x=0:y=0:w=iw:h=70:color=black@0.65:t=fill,drawbox=x=0:y=1010:w=iw:h=70:color=black@0.65:t=fill,ass=microscope-captions.ass',
    '-c:v', 'libx264', '-crf', '18', '-preset', 'medium', '-pix_fmt', 'yuv420p', '-movflags', '+faststart',
    'cells-deforming-culture.mp4'], cwd=root, check=True)
subprocess.run([args.ffmpeg, '-hide_banner', '-i', str(root / 'cells-deforming-culture.mp4'), '-f', 'null', '-'], check=True)
