using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using EasyWebCam;

public class Test : MonoBehaviour
{
    [Header("WebCam UI")]
    [SerializeField] private WebCam _webCam;
    [SerializeField] private Button _captureButton;
    [SerializeField] private Button _changeButton;

    [Header("Capture UI")]
    [SerializeField] private GameObject _captureUiObject;
    [SerializeField] private RawImage[] _captureImages;
    [SerializeField] private AspectRatioFitter[] _captureAspects;
    [SerializeField] private Button _closeCaptureButton;

    [Header("Textures")]
    [SerializeField] private RawImage _webCamTexture;
    [SerializeField] private RawImage _copiedTexture;

    private CaptureOption[] mCaptureOptions = new CaptureOption[]
    {
        new CaptureOption(  0, false),
        new CaptureOption( 90, false),
        new CaptureOption(180, false),
        new CaptureOption(270, false),
        new CaptureOption(  0, true),
        new CaptureOption( 90, true),
        new CaptureOption(180, true),
        new CaptureOption(270, true),
    };

    private CaptureInfo[] mCurrentCaptureInfos = null;

    private void Awake()
    {
        _captureUiObject.SetActive(false);

        _captureButton.onClick.AddListener(delegate
        {
            if (_webCam.IsCaptureBusy())
                return;

            DestroyCapturedTextures();

            mCurrentCaptureInfos = new CaptureInfo[mCaptureOptions.Length];

            for (int i = 0; i < 8; i++)
            {
                CaptureOption o = mCaptureOptions[i];
                CaptureInfo info = _webCam.Capture(o.rotationAngle, o.flipHorizontally, false);

                if (info.State == CaptureState.Success)
                {
                    mCurrentCaptureInfos[i] = info;
                    Texture2D texture = info.GetTexture2D();
                    _captureImages[i].texture = texture;
                    _captureAspects[i].aspectRatio = (float)texture.width / texture.height;
                }
                else
                {
                    mCurrentCaptureInfos[i] = null;
                    _captureImages[i].texture = null;
                }
            }

            _captureUiObject.SetActive(true);
        });

        _changeButton.onClick.AddListener(delegate
        {
            _webCam.StartWebCam(!_webCam.IsFrontFacing,
                _webCam.Resolution,
                _webCam.FPS);
            _webCamTexture.texture = _webCam.Texture;

            DestroyCapturedTextures();
        });

        _closeCaptureButton.onClick.AddListener(delegate
        {
            _captureUiObject.SetActive(false);
        });
    }

    private Texture2D captureTexture = null;

    private void Update()
    {
        if (_webCam.IsPlaying && _webCam.Texture != null)
        {
            if (captureTexture == null || captureTexture.width != _webCam.Texture.width || captureTexture.height != _webCam.Texture.height)
            {
                if (captureTexture != null)
                    Destroy(captureTexture);

                captureTexture = new Texture2D(_webCam.Texture.width, _webCam.Texture.height);
                _copiedTexture.texture = captureTexture;
            }

            Color32[] colors = _webCam.Texture.GetPixels32();
            captureTexture.SetPixels32(colors);
            captureTexture.Apply();
        }
    }

    private void Start()
    {
        _webCam.Initialize();
        _webCam.RequestPermission((WebCam.Result result) =>
        {
            if (result == WebCam.Result.Success)
                _webCam.StartWebCam();
            _webCamTexture.texture = _webCam.Texture;
        });
    }

    private void OnDestroy()
    {
        DestroyCapturedTextures();
    }

    private void DestroyCapturedTextures()
    {
        if (mCurrentCaptureInfos != null)
        {
            for (int i = 0; i < mCurrentCaptureInfos.Length; i++)
                mCurrentCaptureInfos[i]?.Destroy();
            mCurrentCaptureInfos = null;
        }
    }

    private struct CaptureOption
    {
        public int rotationAngle;
        public bool flipHorizontally;

        public CaptureOption(int rotationAngle, bool flipHorizontally)
        {
            this.rotationAngle = rotationAngle;
            this.flipHorizontally = flipHorizontally;
        }
    }
}
