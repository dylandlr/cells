using System.Collections;
using System.IO;
using UnityEngine;

namespace Cells.Demo
{
    /// <summary>
    /// Opt-in capture of *actual Unity Game View frames* for demo review.
    /// Requires Unity Play Mode or a running player; never synthesizes footage.
    /// Converts to MP4 externally using ffmpeg after the frames are saved.
    /// </summary>
    public sealed class DemoFrameCapture : MonoBehaviour
    {
        public bool captureOnStart;
        [Range(1,60)] public int framesPerSecond = 30;
        [Range(1,120)] public int durationSeconds = 20;
        public string folderName = "cells-unity-demo";
        public string OutputFolder { get; private set; }
        public int FramesCaptured { get; private set; }

        IEnumerator Start()
        {
            if (!captureOnStart) yield break;
            int fps = Mathf.Clamp(framesPerSecond,1,60);
            int count = fps * Mathf.Clamp(durationSeconds,1,120);
            string safeFolder = string.IsNullOrWhiteSpace(folderName)
                ? "cells-unity-demo" : Path.GetFileName(folderName);
            OutputFolder = Path.Combine(Application.persistentDataPath,safeFolder);
            Directory.CreateDirectory(OutputFolder);
            // Explicit capture rate makes each recorded frame represent a
            // fixed simulation-time increment, regardless of rendering speed.
            int previousCaptureRate = Time.captureFramerate;
            Time.captureFramerate = fps;
            Debug.Log("cells gameplay frame capture started: " + OutputFolder);
            try
            {
                for (int frame=0; frame<count; frame++)
                {
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(
                        OutputFolder, "frame_" + frame.ToString("D6") + ".png"));
                    FramesCaptured = frame+1;
                }
            }
            finally
            {
                Time.captureFramerate = previousCaptureRate;
                Debug.Log("cells gameplay frame capture finished: " + FramesCaptured +
                    " frames in " + OutputFolder);
            }
        }
    }
}
