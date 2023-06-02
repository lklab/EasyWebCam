using UnityEngine;

namespace LKWebCam
{
    public class ComputeShaderCaptureWorker : ICaptureWorker
    {
        private bool mIsBusy = false;

        public bool IsBusy { get { return mIsBusy; } }

        public static bool IsSupported(ComputeShader computeShader)
        {
            int kernelIndex = computeShader.FindKernel("TODO");
            return computeShader.IsSupported(kernelIndex);
        }

        public ComputeShaderCaptureWorker(ComputeShader computeShader)
        {

        }

        public CaptureResult<Texture2D> Capture(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            return CaptureResult<Texture2D>.Fail;
        }
        
        public CaptureResult<RenderTexture> Capture(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            return CaptureResult<RenderTexture>.Fail;
        }

        public System.Func<CaptureResult<Texture2D>> CaptureAsync(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            return null;
        }
        
        public System.Func<CaptureResult<RenderTexture>> CaptureAsync(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            return null;
        }
    }
}
