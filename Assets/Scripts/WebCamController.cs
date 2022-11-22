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

    [Header("Display settings")]
    [SerializeField] private bool _flipHorizontally = true;

    /// <summary>
    /// Is WebCam supported
    /// </summary>
    public bool IsSupported { get { return mSupported; } }

    /// <summary>
    /// Is WebCam playing
    /// </summary>
    public bool IsPlaying { get { return mIsPlaying; } }

    private WebCamTexture mWebCamTexture;
    private bool mSupported = false;
    private bool mIsPlaying = false;

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
        /* disable raw image */
        _rawImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (mIsPlaying)
        {
            /* check webcam and orientation */
            if (mWebCamProperties.videoRotationAngle != mWebCamTexture.videoRotationAngle ||
                mWebCamProperties.width != mWebCamTexture.width ||
                mWebCamProperties.height != mWebCamTexture.height ||
                mWebCamProperties.videoVerticallyMirrored != mWebCamTexture.videoVerticallyMirrored ||
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
    /// Check and reset device's WebCam permission
    /// </summary>
    /// <param name="callback">Callback to receive initialization result</param>
    public void Initialize(Action<Error> callback)
    {
        if (mSupported)
        {
            callback?.Invoke(Error.Success);
            return;
        }

        if (mAcquireWebCamPermissionCoroutine != null)
        {
            Debug.LogWarning("WebCamController: Already initializing.");
            callback?.Invoke(Error.Busy);
            return;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogError("WebCamController: GameObject is disabled.");
            callback?.Invoke(Error.Disabled);
            return;
        }

        mAcquireWebCamPermissionCoroutine = StartCoroutine(AcquireWebCamPermission(error =>
        {
            if (error == Error.Success)
                error = InitializeWebCamTexture();

            switch (error)
            {
                case Error.NotSupported:
                    Debug.LogError("WebCamController: WebCam is not supported.");
                    break;

                case Error.Permission:
                    Debug.LogError("WebCamController: Failed to get WebCam permission.");
                    break;
            }

            mAcquireWebCamPermissionCoroutine = null;
            callback?.Invoke(error);
        }));
    }

    /// <summary>
    /// Start WebCam
    /// </summary>
    public void StartWebCam()
    {
        if (!mSupported || mIsPlaying)
            return;

        mWebCamTexture.Play();
        _rawImage.gameObject.SetActive(true);

        if (_flipHorizontally)
            _viewport.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
        else
            _viewport.localScale = Vector3.one;

        mIsPlaying = true;
    }

    /// <summary>
    /// Stop WebCam
    /// </summary>
    public void StopWebCam()
    {
        if (!mIsPlaying)
            return;

        mWebCamTexture.Stop();
        _rawImage.gameObject.SetActive(false);

        mIsPlaying = false;
    }

    public void Resizing()
    {
        if (!mIsPlaying)
            return;

        /* setup params */
        float rotationAngle = mWebCamTexture.videoRotationAngle;
        int rotationStep = Mathf.RoundToInt(rotationAngle / 90.0f);
        float scale = 1.0f;
        float aspectRatio = (float)mWebCamTexture.width / mWebCamTexture.height;

        /* rotation */
        float angle = rotationStep * 90.0f;
        _rawImage.transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);

        /* size */
        _aspectRatioFitter.aspectRatio = aspectRatio;

        /* scale */
        if ((rotationStep % 2) != 0)
        {
            float viewportRatio = _viewport.rect.width / _viewport.rect.height;
            scale = Mathf.Max(1.0f / aspectRatio, viewportRatio);
        }

        /* flip */
        if (mWebCamTexture.videoVerticallyMirrored)
            _rawImage.transform.localScale = new Vector3(scale, -scale, scale);
        else
            _rawImage.transform.localScale = new Vector3(scale, scale, scale);

        /* save webcam properties */
        mWebCamProperties.videoRotationAngle = mWebCamTexture.videoRotationAngle;
        mWebCamProperties.width = mWebCamTexture.width;
        mWebCamProperties.height = mWebCamTexture.height;
        mWebCamProperties.videoVerticallyMirrored = mWebCamTexture.videoVerticallyMirrored;
        mCurrentOrientation = Screen.orientation;
    }

    /// <summary>
    /// Taking a photo
    /// </summary>
    /// <param name="flipVertically">Vertically flip the photo</param>
    /// <param name="rotationAngle">Angle to rotate the photo</param>
    /// <param name="flipHorizontally">Horizontally flip the photo</param>
    /// <returns>Taken photo. Must Destroy() when no longer use it.</returns>
    public Texture2D Capture(bool flipVertically, float rotationAngle, bool flipHorizontally)
    {
        if (!mIsPlaying)
            return null;

        int rotationStep = Mathf.RoundToInt(rotationAngle / 90.0f);
        if (rotationStep >= 4)
            rotationStep = rotationStep % 4;
        else if (rotationStep < 0)
            rotationStep += -((rotationStep + 1) / 4 - 1) * 4;

        if (flipVertically)
        {
            flipHorizontally = !flipHorizontally;
            flipVertically = false;

            switch (rotationStep)
            {
                case 0: rotationStep = 2; break;
                case 1: rotationStep = 1; break;
                case 2: rotationStep = 0; break;
                case 3: rotationStep = 3; break;
            }
        }

        int width = 0;
        int height = 0;

        switch (rotationStep)
        {
            case 0:
            case 2:
                width = mWebCamTexture.width;
                height = mWebCamTexture.height;
                break;

            case 1:
            case 3:
                width = mWebCamTexture.height;
                height = mWebCamTexture.width;
                break;
        }

        Texture2D captureTexture = new Texture2D(width, height);
        Color[] webCamPixels = mWebCamTexture.GetPixels();

        if (!flipHorizontally)
        {
            switch (rotationStep)
            {
                case 0:
                    captureTexture.SetPixels(webCamPixels);
                    break;

                case 1:
                    for (int j = 0; j < height; j++)
                    {
                        int x = height - 1 - j;
                        for (int i = 0; i < width; i++)
                            captureTexture.SetPixel(i, j, webCamPixels[x + i * height]);
                    }
                    break;

                case 2:
                    for (int i = 0; i < width; i++)
                    {
                        int x = width - 1 - i;
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[x + (height - 1 - j) * width]);
                    }
                    break;

                case 3:
                    for (int i = 0; i < width; i++)
                    {
                        int y = width - 1 - i;
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[j + y * height]);
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
                        int x = width - 1 - i;
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[x + j * width]);
                    }
                    break;

                case 1:
                    for (int i = 0; i < width; i++)
                    {
                        int y = width - 1 - i;
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[(height - 1 - j) + y * height]);
                    }
                    break;

                case 2:
                    for (int j = 0; j < height; j++)
                    {
                        int y = height - 1 - j;
                        for (int i = 0; i < width; i++)
                            captureTexture.SetPixel(i, j, webCamPixels[i + y * width]);
                    }
                    break;

                case 3:
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                            captureTexture.SetPixel(i, j, webCamPixels[j + i * height]);
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
        return Capture(mWebCamProperties.videoVerticallyMirrored, mWebCamProperties.videoRotationAngle, _flipHorizontally);
    }

    private Error InitializeWebCamTexture()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        WebCamDevice device = default;
        mSupported = false;
        mIsPlaying = false;

        if (devices != null)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (_useFrontFacing == devices[i].isFrontFacing)
                {
                    device = devices[i];
                    mSupported = true;
                    break;
                }
            }
        }

        if (mSupported)
        {
            mWebCamTexture = new WebCamTexture(device.name, _webCamResolution.x, _webCamResolution.y, _webCamFPS);
            _rawImage.texture = mWebCamTexture;
            _rawImage.gameObject.SetActive(false);

            return Error.Success;
        }
        else
            return Error.NotSupported;
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
}
