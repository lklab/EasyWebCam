using System;
using System.Collections;
using UnityEngine;

namespace EasyWebCam
{
    public interface ICaptureWorker
    {
        public bool IsBusy { get; }
        
        public CaptureInfo Capture(int rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info);
        
        public IEnumerator CaptureAsync(int rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info, Action<CaptureInfo> onCompleted);
    }
}
