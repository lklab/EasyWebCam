using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using EasyWebCam;

public class EasyWebCamSample : MonoBehaviour
{
    [Header("WebCam UI")]
    [SerializeField] private WebCam _webCam;
    [SerializeField] private Button _captureButton;
    [SerializeField] private Button _changeButton;

    [Header("Capture UI")]
    [SerializeField] private GameObject _captureUiObject;
    [SerializeField] private RawImage _captureImage;
    [SerializeField] private AspectRatioFitter _captureAspect;
    [SerializeField] private Button _closeCaptureButton;

    [Header("Common UI")]
    [SerializeField] private Text _permissionText;

    private CaptureInfo mCaptureInfo = null;
    private Vector2 mViewportSize = Vector2.zero;

    private void Awake()
    {
        _captureUiObject.SetActive(false);

        _captureButton.onClick.AddListener(delegate
        {
            if (_webCam.IsCaptureBusy())
                return;

            _webCam.CaptureAsync((CaptureInfo info) =>
            {
                if (info.State != CaptureState.Success)
                {
                    mCaptureInfo?.Destroy();
                    mCaptureInfo = null;
                    return;
                }

                mCaptureInfo = info;

                Texture2D texture = info.GetTexture2D();
                _captureImage.texture = texture;
                _captureAspect.aspectRatio = (float)texture.width / texture.height;

                _captureUiObject.SetActive(true);

            }, mCaptureInfo);
        });

        _changeButton.onClick.AddListener(delegate
        {
            _webCam.StopWebCam();
            WebCam.Error error = _webCam.StartWebCam(!_webCam.IsFrontFacing);

            if (error == WebCam.Error.NotSupported)
                StartAnyWebCam();

            DestroyCapturedTexture();
        });

        _closeCaptureButton.onClick.AddListener(delegate
        {
            _captureUiObject.SetActive(false);
        });
    }

    private IEnumerator Start()
    {
        _permissionText.text = "Requesting camera permission...";

        yield return null;

        _webCam.RequestPermission((WebCam.Error error) =>
        {
            _permissionText.text = $"Permission response: {error}";

            if (error == WebCam.Error.Success)
                error = _webCam.StartWebCam();

            if (error == WebCam.Error.NotSupported)
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
        _webCam.StartWebCam(
            deviceIndex: 0,
            resolution: _webCam.Resolution,
            fps: _webCam.FPS);
    }
}
