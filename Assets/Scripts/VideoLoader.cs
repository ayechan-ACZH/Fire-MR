using UnityEngine;
using UnityEngine.Video;
using System.IO;

public class VideoLoader : MonoBehaviour
{
    public string videoName;
    void Start()
    {
        VideoPlayer vp = GetComponent<VideoPlayer>();

        string path = Path.Combine(Application.streamingAssetsPath, videoName);
        vp.url = path;

        vp.Prepare();
        vp.Play();
    }
}
