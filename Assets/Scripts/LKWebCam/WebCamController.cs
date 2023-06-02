using System;
using System.Collections;
using UnityEngine;

#if !UNITY_EDITOR
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#endif

namespace LKWebCam
{
    public class WebCamController : MonoBehaviour
    {
        public enum Error { Success, Disabled, NotSupported, Permission, Busy }

        public enum CaptureMode { Multithread, ComputeShader }

        [Header("UI")]
        [SerializeField] private Viewport _viewport;

        [Header("WebCam settings")]
        [SerializeField] private Vector2Int _webCamResolution = new Vector2Int(1280, 720);
        [SerializeField] private int _webCamFPS = 60;
        [SerializeField] private bool _useFrontFacing = true;
        [SerializeField] private bool _autoResizeViewport = true;

        [Header("Capture settings")]
        [SerializeField] private CaptureMode _captureMode = CaptureMode.Multithread;
        [SerializeField] private int _captureThreadCount = 4;
        [SerializeField] private ComputeShader _captureComputeShader;

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

        /// <summary>
        /// Current WebCam texture
        /// </summary>
        public WebCamTexture Texture { get; private set; } = null;

        private Coroutine mAcquireWebCamPermissionCoroutine = null;

        private ICaptureWorker mCaptureWorker;

        private void Awake()
        {
            Initialize();
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

            /* clear viewport */
            _viewport.ClearWebCamTexture();
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

            /* make WebCam texture */
            Texture = new WebCamTexture(device.name, resolution.x, resolution.y, fps);
            Texture.Play();

            /* setup viewport */
            _viewport.SetWebCamTexture(Texture);

            if (_autoResizeViewport)
                _viewport.SetAutoResizingEnabled(true);

            if (flipHorizontally)
                _viewport.RectTr.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
            else
                _viewport.RectTr.localScale = Vector3.one;
            
            /* setup capture worker */
            switch (_captureMode)
            {
                case CaptureMode.Multithread:
                    mCaptureWorker = new MultithreadCaptureWorker(Texture, _captureThreadCount);
                    break;

                case CaptureMode.ComputeShader:
                    if (ComputeShaderCaptureWorker.IsSupported(_captureComputeShader))
                        mCaptureWorker = new ComputeShaderCaptureWorker(_captureComputeShader);
                    else
                        mCaptureWorker = new MultithreadCaptureWorker(Texture, _captureThreadCount);
                    break;
            }

            /* store variables */
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
            _viewport.ClearWebCamTexture();

            IsPlaying = false;
        }

        /// <summary>
        /// Resize UI
        /// </summary>
        public void Resize()
        {
            if (!IsPlaying)
                return;

            _viewport.Resize();
        }

        /// <summary>
        /// Taking a photo
        /// </summary>
        /// <param name="rotationAngle">Angle to rotate the photo</param>
        /// <param name="flipHorizontally">Horizontally flip the photo</param>
        /// <param name="clip">Whether to clip only the part visible in the viewport</param>
        /// <returns>Taken photo. Must Destroy() when no longer use it.</returns>
        public CaptureResult<Texture2D> Capture(float rotationAngle, bool flipHorizontally, bool clip)
        {
            if (!IsPlaying)
                return CaptureResult<Texture2D>.Fail;

            return mCaptureWorker.Capture(rotationAngle, flipHorizontally, clip, _viewport.AspectRatio);
        }

        /// <summary>
        /// Taking a photo
        /// </summary>
        /// <returns>Taken photo. Must Destroy() when no longer use it.</returns>
        public CaptureResult<Texture2D> Capture()
        {
            return Capture(_viewport.WebCamProperties.videoRotationAngle, FlipHorizontally, true);
        }

        public CaptureResult<RenderTexture> Capture(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip)
        {
            if (!IsPlaying)
                return CaptureResult<RenderTexture>.Fail;

            return mCaptureWorker.Capture(texture, rotationAngle, flipHorizontally, clip, _viewport.AspectRatio);
        }

        public CaptureResult<RenderTexture> Capture(RenderTexture texture)
        {
            return Capture(texture, _viewport.WebCamProperties.videoRotationAngle, FlipHorizontally, true);
        }

        /// <summary>
        /// Taking a photo (async)
        /// </summary>
        /// <param name="rotationAngle">Angle to rotate the photo</param>
        /// <param name="flipHorizontally">Horizontally flip the photo</param>
        /// <param name="clip">Whether to clip only the part visible in the viewport</param>
        /// <param name="callback">Callback to get a photo. Must Destroy() when no longer use the photo.</param>
        public void CaptureAsync(float rotationAngle, bool flipHorizontally, bool clip, System.Action<CaptureResult<Texture2D>> callback)
        {
            if (!IsPlaying)
            {
                callback?.Invoke(CaptureResult<Texture2D>.Fail);
                return;
            }

            System.Func<CaptureResult<Texture2D>> waitFunc = mCaptureWorker.CaptureAsync(rotationAngle, flipHorizontally, clip, _viewport.AspectRatio);
            StartCoroutine(WaitCaptureCoroutine(waitFunc, callback));
        }

        /// <summary>
        /// Taking a photo (async)
        /// </summary>
        /// <param name="callback">Callback to get a photo. Must Destroy() when no longer use the photo.</param>
        public void CaptureAsync(System.Action<CaptureResult<Texture2D>> callback)
        {
            CaptureAsync(_viewport.WebCamProperties.videoRotationAngle, FlipHorizontally, true, callback);
        }

        public void CaptureAsync(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, System.Action<CaptureResult<RenderTexture>> callback)
        {
            if (!IsPlaying)
            {
                callback?.Invoke(CaptureResult<RenderTexture>.Fail);
                return;
            }

            System.Func<CaptureResult<RenderTexture>> waitFunc = mCaptureWorker.CaptureAsync(texture, rotationAngle, flipHorizontally, clip, _viewport.AspectRatio);
            StartCoroutine(WaitCaptureCoroutine(waitFunc, callback));
        }

        public void CaptureAsync(RenderTexture texture, System.Action<CaptureResult<RenderTexture>> callback)
        {
            CaptureAsync(texture, _viewport.WebCamProperties.videoRotationAngle, FlipHorizontally, true, callback);
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
            
            PermissionCallbacks permissionCallbacks = new PermissionCallbacks();
            permissionCallbacks.PermissionDenied += (string msg) => callback?.Invoke(Error.Permission);
            permissionCallbacks.PermissionDeniedAndDontAskAgain += (string msg) => callback?.Invoke(Error.Permission);
            permissionCallbacks.PermissionGranted += (string msg) => callback?.Invoke(Error.Success);

            Permission.RequestUserPermission(Permission.Camera, permissionCallbacks);
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

        private IEnumerator WaitCaptureCoroutine<T>(System.Func<CaptureResult<T>> waitFunc, System.Action<CaptureResult<T>> callback) where T : Texture
        {
            CaptureResult<T> result = waitFunc();

            while (result.state == CaptureState.Working)
            {
                yield return null;
                result = waitFunc();
            }

            callback?.Invoke(result);
        }
    }
}
