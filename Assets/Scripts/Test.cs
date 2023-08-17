using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LKWebCam;

public class Test : MonoBehaviour
{
    [Header("WebCam UI")]
    [SerializeField] private WebCamController _webCamController;
    [SerializeField] private Button _captureButton;
    [SerializeField] private Button _changeButton;

    [Header("Capture UI")]
    [SerializeField] private GameObject _captureUiObject;
    [SerializeField] private RawImage[] _captureImages;
    [SerializeField] private AspectRatioFitter[] _captureAspects;
    [SerializeField] private Button _closeCaptureButton;

    private CaptureOption[] mCaptureOptions = new CaptureOption[]
    {
        new CaptureOption(  0.0f, false),
        new CaptureOption( 90.0f, false),
        new CaptureOption(180.0f, false),
        new CaptureOption(270.0f, false),
        new CaptureOption(  0.0f, true),
        new CaptureOption( 90.0f, true),
        new CaptureOption(180.0f, true),
        new CaptureOption(270.0f, true),
    };

    private Texture2D[] mCurrentTextures = null;

    private void Awake()
    {
        _captureUiObject.SetActive(false);

        _captureButton.onClick.AddListener(delegate
        {
            if (_webCamController.IsCaptureBusy())
                return;

            DestroyCapturedTextures();

            mCurrentTextures = new Texture2D[8];

            for (int i = 0; i < 8; i++)
            {
                CaptureOption o = mCaptureOptions[i];
                CaptureResult<Texture2D> result = _webCamController.Capture(o.rotationAngle, o.flipHorizontally, false);

                if (result.state == CaptureState.Success)
                {
                    mCurrentTextures[i] = result.texture;
                    _captureImages[i].texture = result.texture;
                    _captureAspects[i].aspectRatio = (float)result.texture.width / result.texture.height;
                }
                else
                {
                    mCurrentTextures[i] = null;
                    _captureImages[i].texture = null;
                }
            }

            _captureUiObject.SetActive(true);
        });

        _changeButton.onClick.AddListener(delegate
        {
            _webCamController.StopWebCam();
            _webCamController.StartWebCam(!_webCamController.IsFrontFacing,
                _webCamController.Resolution,
                _webCamController.FPS);

            DestroyCapturedTextures();
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
        DestroyCapturedTextures();
    }

    private void DestroyCapturedTextures()
    {
        if (mCurrentTextures != null)
        {
            for (int i = 0; i < mCurrentTextures.Length; i++)
                Destroy(mCurrentTextures[i]);
            mCurrentTextures = null;
        }
    }

    private struct CaptureOption
    {
        public float rotationAngle;
        public bool flipHorizontally;

        public CaptureOption(float rotationAngle, bool flipHorizontally)
        {
            this.rotationAngle = rotationAngle;
            this.flipHorizontally = flipHorizontally;
        }
    }
}
