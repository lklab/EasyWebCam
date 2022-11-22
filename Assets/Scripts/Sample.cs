using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
    [SerializeField] private WebCamController _webCamController;

    private void Start()
    {
        _webCamController.Initialize((WebCamController.Error error) =>
        {
            if (error == WebCamController.Error.Success)
                _webCamController.StartWebCam();
        });
    }
}
