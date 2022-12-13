using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if !UNITY_EDITOR
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#endif

public class WebCamController : MonoBehaviour
{
    public enum Error { Success, Disabled, NotSupported, Permission, Busy }

    [Header("UI components")]
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RawImage _rawImage;
    [SerializeField] private AspectRatioFitter _aspectRatioFitter;

    [Header("WebCam settings")]
    [SerializeField] private Vector2Int _webCamResolution = new Vector2Int(1280, 720);
    [SerializeField] private int _webCamFPS = 60;
    [SerializeField] private bool _useFrontFacing = true;

    /// <summary>
    /// Whether it has been initialized
    /// </summary>
    public bool IsInitialized { get; private set; } = false;

    /// <summary>
    /// Whether camera permission is allowed
    /// </summary>
    public bool IsPermitted { get; private set; } = false;

    /// <summary>
    /// Is WebCam playing
    /// </summary>
    public bool IsPlaying { get; private set; } = false;

    /// <summary>
    /// WebCam resolution
    /// </summary>
    public Vector2Int Resolution { get { return _webCamResolution; } }

    /// <summary>
    /// WebCam FPS
    /// </summary>
    public int FPS { get { return _webCamFPS; } }

    /// <summary>
    /// Is WebCam front facing
    /// </summary>
    public bool IsFrontFacing { get { return _useFrontFacing; } }

    /// <summary>
    /// Horizontally flip the WebCam
    /// </summary>
    public bool FlipHorizontally { get; private set; }

    public WebCamTexture Texture { get; private set; } = null;

    private Coroutine mAcquireWebCamPermissionCoroutine = null;

    private WebCamProperties mWebCamProperties = new WebCamProperties();
    private ScreenOrientation mCurrentOrientation = ScreenOrientation.Portrait;

    private class WebCamProperties
    {
        public int videoRotationAngle = -1;
        public bool videoVerticallyMirrored = false;
        public int width = 0;
        public int height = 0;
    }

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (IsPlaying)
        {
            /* check webcam and orientation */
            if (mWebCamProperties.videoRotationAngle != Texture.videoRotationAngle ||
                mWebCamProperties.width != Texture.width ||
                mWebCamProperties.height != Texture.height ||
                mWebCamProperties.videoVerticallyMirrored != Texture.videoVerticallyMirrored ||
                mCurrentOrientation != Screen.orientation)
            {
                Resizing();
            }
        }
    }

    private void OnDestroy()
    {
        StopWebCam();
    }

    /// <summary>
    /// Initialize WebCamController
    /// </summary>
    public void Initialize()
    {
        if (IsInitialized)
            return;
        IsInitialized = true;

        /* check permission */
        IsPermitted = CheckPermission();

        /* disable raw image */
        _rawImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// Request WebCam permission
    /// </summary>
    /// <param name="callback">Callback to receive permission result</param>
    public void RequestPermission(Action<Error> callback)
    {
        if (IsPermitted)
        {
            callback?.Invoke(Error.Success);
            return;
        }

        if (mAcquireWebCamPermissionCoroutine != null)
        {
            callback?.Invoke(Error.Busy);
            return;
        }

        if (!isActiveAndEnabled)
        {
            callback?.Invoke(Error.Disabled);
            return;
        }

        mAcquireWebCamPermissionCoroutine = StartCoroutine(AcquireWebCamPermission(error =>
        {
            mAcquireWebCamPermissionCoroutine = null;

            IsPermitted = error == Error.Success;
            callback?.Invoke(error);
        }));
    }

    /// <summary>
    /// Stop requesting for WebCam permission
    /// </summary>
    public void StopRequestPermission()
    {
        if (mAcquireWebCamPermissionCoroutine != null)
        {
            StopCoroutine(mAcquireWebCamPermissionCoroutine);
            mAcquireWebCamPermissionCoroutine = null;
        }    
    }

    /// <summary>
    /// Start WebCam
    /// </summary>
    /// <returns>
    /// Error.Success when WebCam started successfully,
    /// Error.Busy when WebCam has already started,
    /// Error.NotSupported when no WebCam.
    /// </returns>
    public Error StartWebCam()
    {
        if (!IsPermitted)
            return Error.Permission;

        if (IsPlaying)
            return Error.Busy;

        return StartWebCam(_useFrontFacing, _webCamResolution, _webCamFPS);
    }

    /// <summary>
    /// Start WebCam
    /// </summary>
    /// <param name="useFrontFacing">Whether to choose the front facing WebCam</param>
    /// <param name="resolution">WebCam resolution</param>
    /// <param name="fps">WebCam FPS</param>
    /// <returns>
    /// Error.Success when WebCam started successfully,
    /// Error.Busy when WebCam has already started,
    /// Error.NotSupported when no WebCam.
    /// </returns>
    public Error StartWebCam(bool useFrontFacing, Vector2Int resolution, int fps)
    {
        if (!IsPermitted)
            return Error.Permission;

        if (IsPlaying)
            return Error.Busy;

        return StartWebCam(useFrontFacing, resolution, fps, useFrontFacing);
    }

    /// <summary>
    /// Start WebCam
    /// </summary>
    /// <param name="useFrontFacing">Whether to choose the front facing WebCam</param>
    /// <param name="resolution">WebCam resolution</param>
    /// <param name="fps">WebCam FPS</param>
    /// <param name="flipHorizontally">Horizontally flip the WebCam</param>
    /// <returns>
    /// Error.Success when WebCam started successfully,
    /// Error.Busy when WebCam has already started,
    /// Error.NotSupported when no WebCam.
    /// </returns>
    public Error StartWebCam(bool useFrontFacing, Vector2Int resolution, int fps, bool flipHorizontally)
    {
        if (!IsPermitted)
            return Error.Permission;

        if (IsPlaying)
            return Error.Busy;

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null)
            return Error.NotSupported;

        WebCamDevice device = default;
        bool found = false;

        for (int i = 0; i < devices.Length; i++)
        {
            if (useFrontFacing == devices[i].isFrontFacing)
            {
                device = devices[i];
                found = true;
                break;
            }
        }

        if (!found)
            return Error.NotSupported;

        return StartWebCam(device, resolution, fps, flipHorizontally);
    }

    /// <summary>
    /// Start WebCam
    /// </summary>
    /// <param name="deviceIndex">Index of WebCamTexture.devices</param>
    /// <param name="resolution">WebCam resolution</param>
    /// <param name="fps">WebCam FPS</param>
    /// <param name="flipHorizontally">Horizontally flip the WebCam</param>
    /// <returns>
    /// Error.Success when WebCam started successfully,
    /// Error.Busy when WebCam has already started,
    /// Error.NotSupported when no WebCam.
    /// </returns>
    public Error StartWebCam(int deviceIndex, Vector2Int resolution, int fps, bool flipHorizontally)
    {
        if (!IsPermitted)
            return Error.Permission;

        if (IsPlaying)
            return Error.Busy;

        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices == null || deviceIndex < 0 || deviceIndex >= devices.Length)
            return Error.NotSupported;

        return StartWebCam(devices[deviceIndex], resolution, fps, flipHorizontally);
    }

    /// <summary>
    /// Start WebCam
    /// </summary>
    /// <param name="device">WebCam device</param>
    /// <param name="resolution">WebCam resolution</param>
    /// <param name="fps">WebCam FPS</param>
    /// <param name="flipHorizontally">Horizontally flip the WebCam</param>
    /// <returns>
    /// Error.Success when WebCam started successfully,
    /// Error.Busy when WebCam has already started,
    /// Error.NotSupported when no WebCam.
    /// </returns>
    public Error StartWebCam(WebCamDevice device, Vector2Int resolution, int fps, bool flipHorizontally)
    {
        if (!IsPermitted)
            return Error.Permission;

        if (IsPlaying)
            return Error.Busy;

        Texture = new WebCamTexture(device.name, resolution.x, resolution.y, fps);
        _rawImage.texture = Texture;

        Texture.Play();
        _rawImage.gameObject.SetActive(true);

        if (flipHorizontally)
            _viewport.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
        else
            _viewport.localScale = Vector3.one;

        _webCamResolution = resolution;
        _webCamFPS = fps;
        _useFrontFacing = device.isFrontFacing;
        FlipHorizontally = flipHorizontally;
        IsPlaying = true;

        return Error.Success;
    }

    /// <summary>
    /// Stop WebCam
    /// </summary>
    public void StopWebCam()
    {
        if (!IsPlaying)
            return;

        Texture.Stop();
        Texture = null;
        _rawImage.gameObject.SetActive(false);

        IsPlaying = false;
    }

    /// <summary>
    /// Resize UI
    /// </summary>
    public void Resizing()
    {
        if (!IsPlaying)
            return;

        /* setup params */
        float rotationAngle = Texture.videoRotationAngle;
        int rotationStep = Mathf.RoundToInt(rotationAngle / 90.0f);
        bool isOrthogonal = (rotationStep % 2) != 0;
        float scale = 1.0f;
        float aspectRatio = (float)Texture.width / Texture.height;

        /* rotation */
        float angle = rotationStep * 90.0f;
        _rawImage.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -angle);

        /* size */
        _aspectRatioFitter.aspectRatio = aspectRatio;

        /* scale */
        if (isOrthogonal)
        {
            float viewportRatio = _viewport.rect.width / _viewport.rect.height;
            scale = Mathf.Max(1.0f / aspectRatio, viewportRatio);
        }

        /* flip */
        if (Texture.videoVerticallyMirrored)
            _rawImage.transform.localScale = new Vector3(scale, -scale, scale);
        else
            _rawImage.transform.localScale = new Vector3(scale, scale, scale);

        /* save webcam properties */
        mWebCamProperties.videoRotationAngle = Texture.videoRotationAngle;
        mWebCamProperties.width = Texture.width;
        mWebCamProperties.height = Texture.height;
        mWebCamProperties.videoVerticallyMirrored = Texture.videoVerticallyMirrored;
        mCurrentOrientation = Screen.orientation;
    }

    /// <summary>
    /// Taking a photo
    /// </summary>
    /// <param name="rotationAngle">Angle to rotate the photo</param>
    /// <param name="flipHorizontally">Horizontally flip the photo</param>
    /// <param name="clip">Whether to clip only the part visible in the viewport</param>
    /// <returns>Taken photo. Must Destroy() when no longer use it.</returns>
    public Texture2D Capture(float rotationAngle, bool flipHorizontally, bool clip)
    {
        if (!IsPlaying)
            return null;

        int rotationStep = Mathf.RoundToInt(rotationAngle / 90.0f);
        if (rotationStep >= 4)
            rotationStep = rotationStep % 4;
        else if (rotationStep < 0)
            rotationStep += -((rotationStep + 1) / 4 - 1) * 4;

        int textureWidth = Texture.width;
        int textureHeight = Texture.height;
        int width = 0;
        int height = 0;
        int widthOffset = 0;
        int heightOffset = 0;

        switch (rotationStep)
        {
            case 0:
            case 2:
                width = textureWidth;
                height = textureHeight;
                break;

            case 1:
            case 3:
                width = textureHeight;
                height = textureWidth;
                break;
        }

        if (clip)
        {
            float viewportAspect = _viewport.rect.width / _viewport.rect.height;
            float textureAspect = (float)width / height;

            if (viewportAspect > textureAspect)
            {
                int newHeight = (int)((float)width / viewportAspect);
                heightOffset = (height - newHeight) / 2;
                height = newHeight;
            }
            else
            {
                int newWidth = (int)((float)height * viewportAspect);
                widthOffset = (width - newWidth) / 2;
                width = newWidth;
            }
        }

        Texture2D captureTexture = new Texture2D(width, height);
        Color[] webCamPixels = Texture.GetPixels();

        if (!flipHorizontally)
        {
            switch (rotationStep)
            {
                case 0:
                    if (clip)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            int ybase = (j + heightOffset) * textureWidth;
                            for (int i = 0; i < width; i++)
                                captureTexture.SetPixel(i, j, webCamPixels[(i + widthOffset) + ybase]);
                        }
                    }
                    else
                        captureTexture.SetPixels(webCamPixels);
                    break;

                case 1:
                    for (int j = 0; j < height; j++)
                    {
                        int x = textureWidth - 1 - (j + heightOffset);
                        for (int i = 0; i < width; i++)
                            captureTexture.SetPixel(i, j, webCamPixels[x + (i + widthOffset) * textureWidth]);
                    }
                    break;

                case 2:
                    for (int i = 0; i < width; i++)
                    {
                        int x = textureWidth - 1 - (i + widthOffset);
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[x + (textureHeight - 1 - (j + heightOffset)) * textureWidth]);
                    }
                    break;

                case 3:
                    for (int i = 0; i < width; i++)
                    {
                        int y = textureHeight - 1 - (i + widthOffset);
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[(j + heightOffset) + y * textureWidth]);
                    }
                    break;
            }
        }
        else
        {
            switch (rotationStep)
            {
                case 0:
                    for (int i = 0; i < width; i++)
                    {
                        int x = textureWidth - 1 - (i + widthOffset);
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[x + (j + heightOffset) * textureWidth]);
                    }
                    break;

                case 1:
                    for (int i = 0; i < width; i++)
                    {
                        int y = textureHeight - 1 - (i + widthOffset);
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[(textureWidth - 1 - (j + heightOffset)) + y * textureWidth]);
                    }
                    break;

                case 2:
                    for (int j = 0; j < height; j++)
                    {
                        int y = textureHeight - 1 - (j + heightOffset);
                        for (int i = 0; i < width; i++)
                            captureTexture.SetPixel(i, j, webCamPixels[(i + widthOffset) + y * textureWidth]);
                    }
                    break;

                case 3:
                    for (int i = 0; i < width; i++)
                    {
                        int ybase = (i + widthOffset) * textureWidth;
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[(j + heightOffset) + ybase]);
                    }
                    break;
            }
        }

        captureTexture.Apply();
        return captureTexture;
    }

    /// <summary>
    /// Taking a photo
    /// </summary>
    /// <returns>Taken photo. Must Destroy() when no longer use it.</returns>
    public Texture2D Capture()
    {
        return Capture(mWebCamProperties.videoRotationAngle, FlipHorizontally, true);
    }

    private IEnumerator AcquireWebCamPermission(Action<Error> callback)
    {
#if !UNITY_EDITOR
#if UNITY_ANDROID
		if (Permission.HasUserAuthorizedPermission(Permission.Camera))
		{
			callback?.Invoke(Error.Success);
			yield break;
		}

		Permission.RequestUserPermission(Permission.Camera);

		while (!Permission.HasUserAuthorizedPermission(Permission.Camera))
			yield return null;

		callback?.Invoke(Error.Success);
#elif UNITY_IOS
		if (Application.HasUserAuthorization(UserAuthorization.WebCam))
		{
			callback?.Invoke(Error.Success);
			yield break;
		}

		yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

		if (Application.HasUserAuthorization(UserAuthorization.WebCam))
			callback?.Invoke(Error.Success);
		else
			callback?.Invoke(Error.Permission);
#endif
#else
        callback?.Invoke(Error.Success);
        yield break;
#endif
    }

    private bool CheckPermission()
    {
#if !UNITY_EDITOR
#if UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Camera);
#elif UNITY_IOS
        return Application.HasUserAuthorization(UserAuthorization.WebCam);
#endif
#else
        return true;
#endif
    }
}
