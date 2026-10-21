# Reproduce the microscope camera recording

The capture template runs the actual Unity PlayMode simulation and saves 600 camera
frames at 1920x1080 / 30 fps. It deliberately lives outside Assets so ordinary test
runs do not record a video. It starts with no challenge toxin, adds 2 mass at 4 s,
then 3 mass reagent at 10 s at the grazer's right membrane. All simulation systems
remain active. The culture deforms locally under contact and injury, has no beating, and starts with chemical false
color hidden. The camera is fixed and excludes the interactive IMGUI panel.

1. Copy `VideoCaptureTests.cs.txt` to
   `Assets/CellsDemo/Tests/PlayMode/VideoCaptureTests.cs` (do not overwrite an existing file).
2. Run Unity CLI from the project root:
   `unity test . --mode PlayMode --filter Cells.Tests.VideoCaptureTests.RecordLiveSimulation --output TestResults/video.xml --timeout 900 -- -logFile Logs/video.log`
3. Remove the temporary `.cs` and its Unity-generated `.meta` after Unity exits.
4. Run `python tools/capture/encode_video.py --ffmpeg ABSOLUTE_PATH_TO_FFMPEG`.

The MP4, PNG frames, measured-state CSV, captions, and provenance are written under
ignored `Artifacts/`. The encoder requires Python's standard library and ffmpeg
with libx264/libass; it does not download anything. Captions are added after capture.
Cell interior granules are illustrative texture warped by deformation, not simulated organelles.
Chemical display interpolation does not alter the finite-volume chemistry grid.
