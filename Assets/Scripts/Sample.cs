using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Sample : MonoBehaviour
{
    [SerializeField] private WebCamController _webCamController;
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _text;

    private void Awake()
    {
        _button.onClick.AddListener(delegate
        {
            _webCamController.StopWebCam();
            _webCamController.StartWebCam(!_webCamController.IsFrontFacing,
                _webCamController.Resolution,
                _webCamController.FPS);
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

    private void Update()
    {
        if (_webCamController.Texture == null)
            _text.text = "null";
        else
        {
            _text.text = "videoVerticallyMirrored=" + _webCamController.Texture.videoVerticallyMirrored.ToString() +
                "\nvideoRotationAngle=" + _webCamController.Texture.videoRotationAngle.ToString();
        }
    }
}
