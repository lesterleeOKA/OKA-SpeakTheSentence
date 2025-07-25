using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.Collections.Generic;

[Serializable]
public class LoadImage : Downloader
{
    private static bool isFileOperationInProgress = false;
    private Queue<(string folderName, string fileName, Action<Texture> callback)> loadQueue = new Queue<(string, string, Action<Texture>)>();
    private bool isProcessingQueue = false;
    [SerializeField] public LoadImageMethod loadImageMethod = LoadImageMethod.StreamingAssets;
    [SerializeField] private ImageType imageType = ImageType.jpg;
    private AssetBundle assetBundle = null;
    [HideInInspector]
    public Texture[] allTextures;
    public bool useGCCollect = true;

    public string ImageExtension
    {
        get
        {
            return this.imageType switch
            {
                ImageType.none => "",
                ImageType.jpg => ".jpg",
                ImageType.png => ".png",
                _ => throw new ArgumentOutOfRangeException(nameof(imageType), imageType, "Invalid image type.")
            };
        }
    }

    public IEnumerator Load(string folderName = "", string fileName = "", Action<Texture> callback = null)
    {
        // Enqueue the loading request
        this.loadQueue.Enqueue((folderName, fileName, callback));

        if (!this.isProcessingQueue)
        {
            yield return this.ProcessLoadQueue();
        }
    }

    private IEnumerator ProcessLoadQueue()
    {
        this.isProcessingQueue = true;

        while (this.loadQueue.Count > 0)
        {
            var (folderName, fileName, callback) = this.loadQueue.Dequeue();

            while (isFileOperationInProgress)
            {
                yield return null;
            }

            isFileOperationInProgress = true;

            // Perform the actual loading based on the selected method
            switch (this.loadImageMethod)
            {
                case LoadImageMethod.StreamingAssets:
                    yield return this.LoadImageFromStreamingAssets(folderName, fileName, callback);
                    break;
                case LoadImageMethod.Resources:
                    yield return this.LoadImageFromResources(folderName, fileName, callback);
                    break;
                case LoadImageMethod.AssetsBundle:
                    yield return this.LoadImageFromAssetsBundle(fileName, callback);
                    break;
                case LoadImageMethod.Url:
                    yield return this.LoadImageFromURL(fileName, callback);
                    break;
                default:
                    yield return this.LoadImageFromStreamingAssets(folderName, fileName, callback);
                    break;
            }

            isFileOperationInProgress = false;
        }

        if (this.useGCCollect)
        {
            yield return Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
        this.isProcessingQueue = false;
    }


    private IEnumerator LoadImageFromStreamingAssets(
    string folderName = "",
    string fileName = "",
    Action<Texture> callback = null)
    {

        var imagePath = Path.Combine(Application.streamingAssetsPath, folderName + "/" + fileName + this.ImageExtension);

        switch (this.loadMethod)
        {
            case LoadMethod.www:
                WWW www = new WWW(imagePath);
                yield return www;

                if (string.IsNullOrEmpty(www.error))
                {
                    Texture2D texture = www.texture;
                    if (texture != null)
                    {
                        texture.filterMode = FilterMode.Bilinear;
                        texture.wrapMode = TextureWrapMode.Clamp;

                        callback?.Invoke(texture);
                        LogController.Instance?.debug($"Loaded Image : {fileName}");
                    }
                }
                else
                {
                    LogController.Instance?.debug($"Error loading image:{www.error}");
                    callback?.Invoke(null);
                }
                break;
            case LoadMethod.UnityWebRequest:
                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imagePath))
                {
                    request.certificateHandler = new WebRequestSkipCert();
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Texture2D texture = DownloadHandlerTexture.GetContent(request);
                        if (texture != null)
                        {
                            texture.filterMode = FilterMode.Bilinear;
                            texture.wrapMode = TextureWrapMode.Clamp;

                            callback?.Invoke(texture);
                            LogController.Instance?.debug($"Loaded Image : {fileName}");
                        }
                    }
                    else
                    {
                        LogController.Instance?.debug($"Error loading image:{request.error}");
                        callback?.Invoke(null);
                    }
                }
                break;
        }
    }

    private IEnumerator LoadImageFromResources(string folderName = "", string fileName = "", Action<Texture> callback = null)
    {
        // Load the image from the "Resources" folder
        var imagePath = folderName + "/" + fileName;
        Texture texture = Resources.Load<Texture>(imagePath);

        if (texture != null)
        {
            // Use the loaded sprite
            LogController.Instance?.debug("Image loaded successfully!");
            callback?.Invoke(texture);
        }
        else
        {
            LogController.Instance?.debug($"Failed to load image from path: {imagePath}");
            callback?.Invoke(null);
        }

        yield return null;
    }


    public IEnumerator loadImageAssetBundleFile(string fileName = "")
    {
        if (this.assetBundle == null)
        {

            string unitKey = Regex.Replace(fileName, @"-c\d+", "-c");
            var assetBundlePath = Path.Combine(Application.streamingAssetsPath, "picture." + unitKey);

#if UNITY_WEBGL && !UNITY_EDITOR
            using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(assetBundlePath))
            {
                request.certificateHandler = new WebRequestSkipCert();
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    this.assetBundle = DownloadHandlerAssetBundle.GetContent(request);
                    this.allTextures = this.assetBundle.LoadAllAssets<Texture>();
                    LogController.Instance?.debug($"downloaded AssetBundle: {this.assetBundle}");
                }
                else
                {
                    LogController.Instance?.debugError($"Failed to download AssetBundle: {request.error}");
                    yield break; // Exit if the download failed
                }
            }
#else
            var assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(assetBundlePath);
            yield return assetBundleCreateRequest;
            this.assetBundle = assetBundleCreateRequest.assetBundle;
            this.allTextures = this.assetBundle.LoadAllAssets<Texture>();
#endif
        }
    }

    private IEnumerator LoadImageFromAssetsBundle(string fileName = "", Action<Texture> callback = null)
    {
        if (this.assetBundle != null)
        {
            //Texture texture = assetBundle.LoadAsset<Texture>(fileName);
            Texture texture = Array.Find(this.allTextures, t => t.name == fileName);

            if (texture != null)
            {
                LogController.Instance?.debug(fileName + " loaded successfully!");
                callback?.Invoke(texture);
            }
            else
            {
                LogController.Instance?.debug($"Failed to load Image asset: {fileName}");
                callback?.Invoke(null);
            }

            yield return null;
        }
    }

    private IEnumerator LoadImageFromURL(string url, Action<Texture> callback = null)
    {
        LogController.Instance?.debug($"Loading Image from url : {url}");
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            www.certificateHandler = new WebRequestSkipCert();
            // Send the request and wait for a response
            yield return www.SendWebRequest();

            // Check for errors
            if (www.result != UnityWebRequest.Result.Success)
            {
                LogController.Instance?.debug($"Error loading image: {www.error}");
                callback?.Invoke(null);
            }
            else
            {
                // Get the texture and apply it to the target renderer
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                LogController.Instance?.debug("loaded api qa texture: " + texture.texelSize);
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;

                    callback?.Invoke(texture);
                    LogController.Instance?.debug($"Loaded Image from url : {url}");
                }
            }
        }
    }

}


public enum ImageType
{
    none,
    jpg,
    png
}
public enum LoadImageMethod
{
    Resources = 0,
    StreamingAssets = 1,
    AssetsBundle = 2,
    Url = 3
}