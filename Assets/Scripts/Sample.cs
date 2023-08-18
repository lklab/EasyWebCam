using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LKWebCam;

public class Sample : MonoBehaviour
{
    [Header("WebCam UI")]
    [SerializeField] private WebCamController _webCamController;
    [SerializeField] private Button _captureButton;
    [SerializeField] private Button _changeButton;

    [Header("Capture UI")]
    [SerializeField] private GameObject _captureUiObject;
    [SerializeField] private RawImage _captureImage;
    [SerializeField] private AspectRatioFitter _captureAspect;
    [SerializeField] private Button _closeCaptureButton;

    private CaptureInfo mCaptureInfo = null;
    private Vector2 mViewportSize = Vector2.zero;

    private void Awake()
    {
        _captureUiObject.SetActive(false);

        _captureButton.onClick.AddListener(delegate
        {
            if (_webCamController.IsCaptureBusy())
                return;

            _webCamController.CaptureAsync((CaptureInfo info) =>
            {
                if (info.State != CaptureState.Success)
                {
                    mCaptureInfo?.Destroy();
                    mCaptureInfo = null;
                    return;
                }

                mCaptureInfo = info;

                RenderTexture texture = info.GetRenderTexture();
                _captureImage.texture = texture;
                _captureAspect.aspectRatio = (float)texture.width / texture.height;

                _captureUiObject.SetActive(true);

            }, mCaptureInfo);
        });

        _changeButton.onClick.AddListener(delegate
        {
            _webCamController.StopWebCam();
            WebCamController.Error error = _webCamController.StartWebCam(!_webCamController.IsFrontFacing);

            if (error == WebCamController.Error.NotSupported)
                StartAnyWebCam();

            DestroyCapturedTexture();
        });

        _closeCaptureButton.onClick.AddListener(delegate
        {
            _captureUiObject.SetActive(false);
        });
    }

    private void Start()
    {
        _webCamController.Initialize();
        _webCamController.RequestPermission((WebCamController.Error error) =>
        {
            if (error == WebCamController.Error.Success)
                error = _webCamController.StartWebCam();

            if (error == WebCamController.Error.NotSupported)
                StartAnyWebCam();
        });
    }

    private void OnDestroy()
    {
        DestroyCapturedTexture();
    }

    private void DestroyCapturedTexture()
    {
        if (mCaptureInfo != null)
        {
            _captureImage.texture = null;
            mCaptureInfo.Destroy();
            mCaptureInfo = null;
        }
    }

    private void StartAnyWebCam()
    {
        _webCamController.StartWebCam(
            deviceIndex: 0,
            resolution: _webCamController.Resolution,
            fps: _webCamController.FPS);
    }
}
