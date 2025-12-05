using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class VideoEventHandler : MonoBehaviour
{
    [Header("Video Player Reference (auto-assigned if empty)")]
    public VideoPlayer videoPlayer;

    [Header("Event fired when the video finishes playing")]
    public UnityEvent OnVideoFinished;

    private bool eventTriggered = false;

    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying || eventTriggered)
            return;

        // Check if the video reached the last frame
        if (videoPlayer.frame >= (long)videoPlayer.frameCount - 1)
        {
            eventTriggered = true;
            OnVideoFinished?.Invoke();
        }
    }
}
