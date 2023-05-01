using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("UI")]
        [SerializeField] private WebCamViewport _viewport;

        [Header("WebCam settings")]
        [SerializeField] private Vector2Int _webCamResolution = new Vector2Int(1280, 720);
        [SerializeField] private int _webCamFPS = 60;
        [SerializeField] private bool _useFrontFacing = true;
        [SerializeField] private bool _autoResizeViewport = true;

        [Header("Capture settings")]
        [SerializeField] private int _captureThreadCount = 4;

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
            Texture.Play();
            _viewport.SetWebCamTexture(Texture);

            if (_autoResizeViewport)
                _viewport.SetAutoResizingEnabled(true);

            if (flipHorizontally)
                _viewport.RectTr.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
            else
                _viewport.RectTr.localScale = Vector3.one;

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
        public Texture2D Capture(float rotationAngle, bool flipHorizontally, bool clip)
        {
            if (!IsPlaying)
                return null;

            WebCamCaptureWorker worker = GetCaptureWorker(rotationAngle, flipHorizontally, clip);
            return worker.Run();
        }

        /// <summary>
        /// Taking a photo
        /// </summary>
        /// <returns>Taken photo. Must Destroy() when no longer use it.</returns>
        public Texture2D Capture()
        {
            if (!IsPlaying)
                return null;

            WebCamCaptureWorker worker = GetCaptureWorker(_viewport.WebCamProperties.videoRotationAngle, FlipHorizontally, true);
            return worker.Run();
        }

        /// <summary>
        /// Taking a photo (async)
        /// </summary>
        /// <param name="rotationAngle">Angle to rotate the photo</param>
        /// <param name="flipHorizontally">Horizontally flip the photo</param>
        /// <param name="clip">Whether to clip only the part visible in the viewport</param>
        /// <param name="callback">Callback to get a photo. Must Destroy() when no longer use the photo.</param>
        public void CaptureAsync(float rotationAngle, bool flipHorizontally, bool clip, System.Action<Texture2D> callback)
        {
            if (!IsPlaying)
            {
                callback?.Invoke(null);
                return;
            }

            WebCamCaptureWorker worker = GetCaptureWorker(rotationAngle, flipHorizontally, clip);
            StartCoroutine(WaitCaptureCoroutine(worker.RunAsync(), callback));
        }

        /// <summary>
        /// Taking a photo (async)
        /// </summary>
        /// <param name="callback">Callback to get a photo. Must Destroy() when no longer use the photo.</param>
        public void CaptureAsync(System.Action<Texture2D> callback)
        {
            if (!IsPlaying)
            {
                callback?.Invoke(null);
                return;
            }

            WebCamCaptureWorker worker = GetCaptureWorker(_viewport.WebCamProperties.videoRotationAngle, FlipHorizontally, true);
            StartCoroutine(WaitCaptureCoroutine(worker.RunAsync(), callback));
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

        private WebCamCaptureWorker GetCaptureWorker(float rotationAngle, bool flipHorizontally, bool clip)
        {
            return new WebCamCaptureWorker(
                texture: Texture,
                rotationAngle: rotationAngle,
                flipHorizontally: flipHorizontally,
                clip: clip,
                viewportAspect: _viewport.RectTr.rect.width / _viewport.RectTr.rect.height,
                threadCount: _captureThreadCount);
        }

        private IEnumerator WaitCaptureCoroutine(System.Func<Texture2D> waitFunc, System.Action<Texture2D> callback)
        {
            Texture2D texture = null;

            while (texture == null)
            {
                yield return null;
                texture = waitFunc();
            }

            callback?.Invoke(texture);
        }
    }
}
