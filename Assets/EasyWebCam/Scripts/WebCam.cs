using System;
using System.Collections;
using UnityEngine;

#if !UNITY_EDITOR
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#endif

namespace EasyWebCam
{
    public class WebCam : MonoBehaviour
    {
        public enum Error { Success, Disabled, NotSupported, Permission, Busy }

        public enum CaptureMode { Multithread, ComputeShader }

        [Header("UI")]
        [SerializeField] private Viewport _viewport;

        [Header("WebCam settings")]
        [SerializeField] private Vector2Int _webCamResolution = new Vector2Int(1920, 1080);
        [SerializeField] private int _webCamFPS = 60;
        [SerializeField] private bool _useFrontFacing = true;
        [SerializeField] private bool _autoResizeViewport = true;

        [Header("Capture settings")]
        [SerializeField] private CaptureMode _captureMode = CaptureMode.ComputeShader;
        [SerializeField] private int _captureThreadCount = 4;
        [SerializeField] private ComputeShader _captureComputeShader;

        /// <summary>
        /// Indicates whether initialization has been completed.
        /// </summary>
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Indicates whether camera permission has been granted.
        /// </summary>
        public bool IsPermitted { get; private set; } = false;

        /// <summary>
        /// Indicates if the WebCam is currently active.
        /// </summary>
        public bool IsPlaying { get; private set; } = false;

        /// <summary>
        /// Resolution of the WebCam.
        /// </summary>
        public Vector2Int Resolution { get { return _webCamResolution; } }

        /// <summary>
        /// Frames per second (FPS) of the WebCam.
        /// </summary>
        public int FPS { get { return _webCamFPS; } }

        /// <summary>
        /// Indicates if the WebCam is front-facing.
        /// </summary>
        public bool IsFrontFacing { get { return _useFrontFacing; } }

        /// <summary>
        /// Horizontally flip the WebCam.
        /// </summary>
        public bool FlipHorizontally { get; private set; }

        /// <summary>
        /// The current WebCam texture.
        /// </summary>
        public WebCamTexture Texture { get; private set; } = null;

        public Viewport Viewport { get { return _viewport; } }

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
        /// Initialize the WebCamController.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;

            /* Check camera permission */
            IsPermitted = CheckPermission();

            /* Clear the viewport */
            _viewport.ClearWebCamTexture();
        }

        /// <summary>
        /// Request permission to use the WebCam.
        /// </summary>
        /// <param name="callback">Callback to receive the permission result.</param>
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
        /// Start the WebCam with default settings.
        /// </summary>
        /// <returns>Error.Success if the WebCam started successfully,
        /// Error.Busy if the WebCam has already started,
        /// Error.NotSupported if no WebCam is available.</returns>
        public Error StartWebCam()
        {
            if (!IsPermitted)
                return Error.Permission;

            if (IsPlaying)
                return Error.Busy;

            return StartWebCam(_useFrontFacing, _webCamResolution, _webCamFPS);
        }

        /// <summary>
        /// Start the WebCam with custom settings.
        /// </summary>
        /// <param name="useFrontFacing">Whether to use the front-facing WebCam.</param>
        /// <returns>Error.Success if the WebCam started successfully,
        /// Error.Busy if the WebCam has already started,
        /// Error.NotSupported if no WebCam is available.</returns>
        public Error StartWebCam(bool useFrontFacing)
        {
            if (!IsPermitted)
                return Error.Permission;

            if (IsPlaying)
                return Error.Busy;

            return StartWebCam(useFrontFacing, _webCamResolution, _webCamFPS);
        }

        /// <summary>
        /// Start the WebCam with custom settings.
        /// </summary>
        /// <param name="useFrontFacing">Whether to use the front-facing WebCam.</param>
        /// <param name="resolution">WebCam resolution.</param>
        /// <param name="fps">WebCam FPS.</param>
        /// <returns>Error.Success if the WebCam started successfully,
        /// Error.Busy if the WebCam has already started,
        /// Error.NotSupported if no WebCam is available.</returns>
        public Error StartWebCam(bool useFrontFacing, Vector2Int resolution, int fps)
        {
            if (!IsPermitted)
                return Error.Permission;

            if (IsPlaying)
                return Error.Busy;

            return StartWebCam(useFrontFacing, resolution, fps, useFrontFacing);
        }

        /// <summary>
        /// Start the WebCam with custom settings and horizontal flip option.
        /// </summary>
        /// <param name="useFrontFacing">Whether to use the front-facing WebCam.</param>
        /// <param name="resolution">WebCam resolution.</param>
        /// <param name="fps">WebCam FPS.</param>
        /// <param name="flipHorizontally">Whether to horizontally flip the WebCam.</param>
        /// <returns>Error.Success if the WebCam started successfully,
        /// Error.Busy if the WebCam has already started,
        /// Error.NotSupported if no WebCam is available.</returns>
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
        /// Start the WebCam with a specified device index and custom settings.
        /// </summary>
        /// <param name="deviceIndex">Index of the WebCamTexture.devices array.</param>
        /// <param name="resolution">WebCam resolution.</param>
        /// <param name="fps">WebCam FPS.</param>
        /// <returns>Error.Success if the WebCam started successfully,
        /// Error.Busy if the WebCam has already started,
        /// Error.NotSupported if no WebCam is available.</returns>
        public Error StartWebCam(int deviceIndex, Vector2Int resolution, int fps)
        {
            if (!IsPermitted)
                return Error.Permission;

            if (IsPlaying)
                return Error.Busy;

            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices == null || deviceIndex < 0 || deviceIndex >= devices.Length)
                return Error.NotSupported;

            return StartWebCam(devices[deviceIndex], resolution, fps, devices[deviceIndex].isFrontFacing);
        }

        /// <summary>
        /// Start the WebCam with a specified device index and custom settings.
        /// </summary>
        /// <param name="deviceIndex">Index of the WebCamTexture.devices array.</param>
        /// <param name="resolution">WebCam resolution.</param>
        /// <param name="fps">WebCam FPS.</param>
        /// <param name="flipHorizontally">Whether to horizontally flip the WebCam.</param>
        /// <returns>Error.Success if the WebCam started successfully,
        /// Error.Busy if the WebCam has already started,
        /// Error.NotSupported if no WebCam is available.</returns>
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
        /// Start the WebCam with a specified device and custom settings.
        /// </summary>
        /// <param name="device">The WebCam device to use.</param>
        /// <param name="resolution">WebCam resolution.</param>
        /// <param name="fps">WebCam FPS.</param>
        /// <param name="flipHorizontally">Whether to horizontally flip the WebCam.</param>
        /// <returns>Error.Success if the WebCam started successfully,
        /// Error.Busy if the WebCam has already started,
        /// Error.NotSupported if no WebCam is available.</returns>
        public Error StartWebCam(WebCamDevice device, Vector2Int resolution, int fps, bool flipHorizontally)
        {
            if (!IsPermitted)
                return Error.Permission;

            if (IsPlaying)
                return Error.Busy;

            // Create the WebCam texture
            Texture = new WebCamTexture(device.name, resolution.x, resolution.y, fps);
            Texture.wrapMode = TextureWrapMode.Clamp;
            Texture.Play();

            // Set up the viewport
            _viewport.SetWebCamTexture(Texture);

            if (_autoResizeViewport)
                _viewport.SetAutoResizingEnabled(true);

            if (flipHorizontally)
                _viewport.RectTr.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
            else
                _viewport.RectTr.localScale = Vector3.one;

            // Set up the capture worker
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                mCaptureWorker = new SinglethreadCaptureWorker(Texture);
            }
            else
            {
                try
                {
                    switch (_captureMode)
                    {
                        case CaptureMode.Multithread:
                            mCaptureWorker = new MultithreadCaptureWorker(Texture, _captureThreadCount);
                            break;

                        case CaptureMode.ComputeShader:
                            if (ComputeShaderCaptureWorker.IsSupported(_captureComputeShader))
                                mCaptureWorker = new ComputeShaderCaptureWorker(Texture, _captureComputeShader);
                            else
                                mCaptureWorker = new MultithreadCaptureWorker(Texture, _captureThreadCount);
                            break;
                    }
                }
                catch
                {
                    mCaptureWorker = new SinglethreadCaptureWorker(Texture);
                }
            }

            // Store variables
            _webCamResolution = resolution;
            _webCamFPS = fps;
            _useFrontFacing = device.isFrontFacing;
            FlipHorizontally = flipHorizontally;
            IsPlaying = true;

            return Error.Success;
        }

        /// <summary>
        /// Stop the WebCam.
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
        /// Resize the UI to fit the WebCam view.
        /// </summary>
        public void Resize()
        {
            if (!IsPlaying)
                return;

            _viewport.Resize();
        }

        /// <summary>
        /// Capture a photo using the device's camera.
        /// </summary>
        /// <param name="rotationAngle">Angle by which the photo should be rotated.</param>
        /// <param name="flipHorizontally">Whether to horizontally flip the photo.</param>
        /// <param name="clip">Whether to clip only the visible part in the viewport.</param>
        /// <param name="info">CaptureInfo to reuse the texture object.</param>
        /// <returns>Information about the captured photo. Remember to call Destroy() when no longer using it.</returns>
        public CaptureInfo Capture(float rotationAngle, bool flipHorizontally, bool clip, CaptureInfo info = null)
        {
            if (!IsPlaying)
                return CaptureInfo.NotPlaying;

            return mCaptureWorker.Capture(rotationAngle, flipHorizontally, clip, _viewport.AspectRatio, info);
        }

        /// <summary>
        /// Capture a photo using the device's camera.
        /// </summary>
        /// <param name="info">CaptureInfo to reuse the texture object.</param>
        /// <returns>Information about the captured photo. Remember to call Destroy() when no longer using it.</returns>
        public CaptureInfo Capture(CaptureInfo info = null)
        {
            return Capture(_viewport.WebCamProperties.videoRotationAngle, FlipHorizontally, true, info);
        }

        /// <summary>
        /// Capture a photo using the device's camera asynchronously.
        /// </summary>
        /// <param name="rotationAngle">Angle by which the photo should be rotated.</param>
        /// <param name="flipHorizontally">Whether to horizontally flip the photo.</param>
        /// <param name="clip">Whether to clip only the visible part in the viewport.</param>
        /// <param name="onComplete">Callback to receive the captured photo info. Remember to call Destroy() when no longer using the photo.</param>
        /// <param name="info">CaptureInfo to reuse the texture object.</param>
        public void CaptureAsync(float rotationAngle, bool flipHorizontally, bool clip, Action<CaptureInfo> onComplete, CaptureInfo info = null)
        {
            if (!IsPlaying)
            {
                onComplete?.Invoke(CaptureInfo.NotPlaying);
                return;
            }

            StartCoroutine(mCaptureWorker.CaptureAsync(rotationAngle, flipHorizontally, clip, _viewport.AspectRatio, info, onComplete));
        }

        /// <summary>
        /// Capture a photo using the device's camera asynchronously.
        /// </summary>
        /// <param name="onComplete">Callback to receive the captured photo info. Remember to call Destroy() when no longer using the photo.</param>
        /// <param name="info">CaptureInfo to reuse the texture object.</param>
        public void CaptureAsync(Action<CaptureInfo> onComplete, CaptureInfo info = null)
        {
            CaptureAsync(_viewport.WebCamProperties.videoRotationAngle, FlipHorizontally, true, onComplete, info);
        }

        /// <summary>
        /// Check if a photo capture is in progress.
        /// </summary>
        /// <returns>True if a capture is in progress, otherwise false.</returns>
        public bool IsCaptureBusy()
        {
            if (!IsPlaying)
                return false;
            
            return mCaptureWorker.IsBusy;
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
#else
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
#else
            return Application.HasUserAuthorization(UserAuthorization.WebCam);
#endif
#else
            return true;
#endif
        }
    }
}
