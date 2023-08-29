using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyWebCam
{
    public interface ICaptureWorker
    {
        public bool IsBusy { get; }
        
        public CaptureInfo Capture(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info);
        
        public IEnumerator CaptureAsync(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info, Action<CaptureInfo> onCompleted);
    }
}
