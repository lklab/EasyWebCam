using UnityEngine;

namespace LKWebCam
{
    public class ComputeShaderCaptureWorker : ICaptureWorker
    {
        public static bool IsSupported(ComputeShader computeShader)
        {
            int kernelIndex = computeShader.FindKernel("TopLeft");
            return computeShader.IsSupported(kernelIndex);
        }

        private WebCamTexture mInputTexture;
        private ComputeShader mComputeShader;
        private bool mIsBusy = false;

        public bool IsBusy { get { return mIsBusy; } }

        public ComputeShaderCaptureWorker(WebCamTexture texture, ComputeShader computeShader)
        {
            mInputTexture = texture;
            mComputeShader = computeShader;
        }

        public CaptureResult<Texture2D> Capture(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            RenderTexture capturedTexture = CaptureInternal(null, rotationAngle, flipHorizontally, clip, viewportAspect);

            Texture2D texture = new Texture2D(capturedTexture.width, capturedTexture.height);
            RenderTexture activeRenderTexture = RenderTexture.active;
            RenderTexture.active = capturedTexture;
            texture.ReadPixels(new Rect(0, 0, capturedTexture.width, capturedTexture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = activeRenderTexture;

            return new CaptureResult<Texture2D>(texture);
        }
        
        public CaptureResult<RenderTexture> Capture(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            texture = CaptureInternal(texture, rotationAngle, flipHorizontally, clip, viewportAspect);
            return new CaptureResult<RenderTexture>(texture);
        }

        public System.Func<CaptureResult<Texture2D>> CaptureAsync(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            CaptureResult<Texture2D> result = Capture(rotationAngle, flipHorizontally, clip, viewportAspect);
            return delegate { return result; };
        }
        
        public System.Func<CaptureResult<RenderTexture>> CaptureAsync(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            CaptureResult<RenderTexture> result = Capture(texture, rotationAngle, flipHorizontally, clip, viewportAspect);
            return delegate { return result; };
        }

        private int GetKernelIndex(ComputeShader computeShader, int rotationStep, bool flipHorizontally)
        {
            if (!flipHorizontally)
            {
                switch (rotationStep)
                {
                    case 0: return mComputeShader.FindKernel("TopLeft");
                    case 1: return mComputeShader.FindKernel("RightTop");
                    case 2: return mComputeShader.FindKernel("BottomRight");
                    case 3: return mComputeShader.FindKernel("LeftBottom");
                }
            }
            else
            {
                switch (rotationStep)
                {
                    case 0: return mComputeShader.FindKernel("TopRight");
                    case 1: return mComputeShader.FindKernel("LeftTop");
                    case 2: return mComputeShader.FindKernel("BottomLeft");
                    case 3: return mComputeShader.FindKernel("RightBottom");
                }
            }

            return mComputeShader.FindKernel("TopLeft");
        }

        private RenderTexture CaptureInternal(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            int rotationStep = Utils.GetRotationStep(rotationAngle);
            Vector2Int capturedTextureSize = Utils.GetCapturedTextureSize(mInputTexture, rotationStep);

            if (texture == null)
                texture = new RenderTexture(capturedTextureSize.x, capturedTextureSize.y, 0);
            texture.enableRandomWrite = true;

            int kernelIndex = GetKernelIndex(mComputeShader, rotationStep, flipHorizontally);
            mComputeShader.SetTexture(kernelIndex, "_CapturedTexture", texture);
            mComputeShader.SetTexture(kernelIndex, "_WebCamTexture", mInputTexture);
            mComputeShader.SetVector("_Rect", new Vector4(0.0f, 0.0f, mInputTexture.width, mInputTexture.height));

            mComputeShader.Dispatch(kernelIndex, texture.width / 8, texture.height / 8, 1);

            return texture;
        }
    }
}
