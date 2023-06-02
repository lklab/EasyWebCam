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

    private RenderTexture mCapturedRenderTexture = null;
    private Vector2 mViewportSize = Vector2.zero;

    private void Awake()
    {
        _captureUiObject.SetActive(false);

        _captureButton.onClick.AddListener(delegate
        {
            if (mViewportSize != _webCamController.Viewport.Size)
            {
                DestroyCapturedTexture();
                mViewportSize = _webCamController.Viewport.Size;
            }

            _webCamController.CaptureAsync(mCapturedRenderTexture, (CaptureResult<RenderTexture> result) =>
            {
                if (result.state != CaptureState.Success)
                    return;

                mCapturedRenderTexture = result.texture;
                _captureImage.texture = mCapturedRenderTexture;
                _captureAspect.aspectRatio = (float)result.texture.width / result.texture.height;

                _captureUiObject.SetActive(true);
            });
        });

        _changeButton.onClick.AddListener(delegate
        {
            _webCamController.StopWebCam();
            _webCamController.StartWebCam(!_webCamController.IsFrontFacing,
                _webCamController.Resolution,
                _webCamController.FPS);

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
                _webCamController.StartWebCam();
        });
    }

    private void OnDestroy()
    {
        DestroyCapturedTexture();
    }

    private void DestroyCapturedTexture()
    {
        if (mCapturedRenderTexture != null)
        {
            _captureImage.texture = null;
            Destroy(mCapturedRenderTexture);
            mCapturedRenderTexture = null;
        }
    }
}
