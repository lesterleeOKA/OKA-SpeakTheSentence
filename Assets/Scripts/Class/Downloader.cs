using System;

[Serializable]
public class Downloader
{
    public LoadMethod loadMethod = LoadMethod.UnityWebRequest;
    public bool useGCCollect = true;
    public int maxRetryCount = 3;
    public float retryDelaySeconds = 1f;
}

public enum LoadMethod
{
    www = 0,
    UnityWebRequest = 1,
    Resources = 3,
    //API = 2,
}
