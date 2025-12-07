using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class VideoEventHandler : MonoBehaviour
{
    [Header("Video Player Reference (auto-assigned if empty)")]
    public VideoPlayer videoPlayer;

    [Header("Event fired when the video finishes playing")]
    public UnityEvent OnVideoFinished;

    [Header("Detection Method")]
    public bool useFrameDetection = true;

    private bool eventTriggered = false;
    private bool isPrepared = false;

    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void OnEnable()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogError("VideoEndEvent: No VideoPlayer found!");
            return;
        }

        // Subscribe events (must be done BEFORE preparing)
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.started += OnVideoStarted;
        if (!useFrameDetection)
            videoPlayer.loopPointReached += OnLoopPointReached;

        // Auto-prepare when enabled (Scene 2 load)
        AutoPrepare();
    }

    private void OnDisable()
    {
        if (videoPlayer == null) return;

        // Unsubscribe to prevent double calls if re-enabled
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.started -= OnVideoStarted;
        if (!useFrameDetection)
            videoPlayer.loopPointReached -= OnLoopPointReached;
    }

    // 🔥 Called automatically when the GameObject becomes active (e.g. on Scene load)
    private void AutoPrepare()
    {
        eventTriggered = false;
        isPrepared = false;
        videoPlayer.Prepare();
    }

    // 🔥 This is what you call from other scripts if you want to manually replay the video
    public void PlayVideo()
    {
        eventTriggered = false;
        isPrepared = false;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        isPrepared = true;
        eventTriggered = false;

        // Play AFTER prepare completed
        videoPlayer.Play();
    }

    private void OnVideoStarted(VideoPlayer vp)
    {
        eventTriggered = false;
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        if (!useFrameDetection)
            OnVideoFinished?.Invoke();
    }

    private void Update()
    {
        if (!useFrameDetection)
            return;

        if (!isPrepared || videoPlayer == null || eventTriggered)
            return;

        if (!videoPlayer.isPlaying)
            return;

        if (videoPlayer.frame >= (long)videoPlayer.frameCount - 1)
        {
            eventTriggered = true;
            OnVideoFinished?.Invoke();
        }
    }
}
