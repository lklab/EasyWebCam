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
    [SerializeField] private Image _captureImage;
    [SerializeField] private AspectRatioFitter _captureAspect;
    [SerializeField] private Button _closeCaptureButton;

    private Sprite mCapturedImage = null;

    private void Awake()
    {
        _captureUiObject.SetActive(false);

        _captureButton.onClick.AddListener(delegate
        {
            if (mCapturedImage != null)
            {
                Destroy(mCapturedImage.texture);
                Destroy(mCapturedImage);
            }

            Texture2D texture = _webCamController.Capture();
            if (texture == null)
                return;

            mCapturedImage = Sprite.Create(
                texture,
                new Rect(0.0f, 0.0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100.0f);

            _captureImage.sprite = mCapturedImage;
            _captureAspect.aspectRatio = (float)texture.width / texture.height;

            _captureUiObject.SetActive(true);
        });

        _changeButton.onClick.AddListener(delegate
        {
            _webCamController.StopWebCam();
            _webCamController.StartWebCam(!_webCamController.IsFrontFacing,
                _webCamController.Resolution,
                _webCamController.FPS);
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
        if (mCapturedImage != null)
        {
            Destroy(mCapturedImage.texture);
            Destroy(mCapturedImage);
            mCapturedImage = null;
        }
    }
}
