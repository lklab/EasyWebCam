using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyWebCam
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

        public CaptureInfo Capture(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info)
        {
            return CaptureInternal(rotationAngle, flipHorizontally, clip, viewportAspect, info);
        }

        public IEnumerator CaptureAsync(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info, Action<CaptureInfo> onCompleted)
        {
            onCompleted?.Invoke(CaptureInternal(rotationAngle, flipHorizontally, clip, viewportAspect, info));
            yield break;
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

        private CaptureInfo CaptureInternal(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info)
        {
            int rotationStep = Utils.GetRotationStep(rotationAngle);

            Vector2Int clippingOffset;
            if (clip)
                clippingOffset = Utils.GetClippingOffset(mInputTexture, rotationStep, viewportAspect);
            else
                clippingOffset = Vector2Int.zero;

            Vector2Int capturedTextureSize = Utils.GetCapturedTextureSize(mInputTexture, rotationStep, clippingOffset);
            int width = capturedTextureSize.x;
            int height = capturedTextureSize.y;

            RenderTexture capturedTexture;
            if (info == null || info.Width != width || info.Height != height || info.Format != Format.Half)
            {
                info?.Destroy();
                info = new CaptureInfo(width, height, Format.Half);
            }

            info.GetRenderTextureRaw(out capturedTexture);

            int kernelIndex = GetKernelIndex(mComputeShader, rotationStep, flipHorizontally);
            mComputeShader.SetTexture(kernelIndex, "_CapturedTexture", capturedTexture);
            mComputeShader.SetTexture(kernelIndex, "_WebCamTexture", mInputTexture);
            mComputeShader.SetVector("_Rect", new Vector4(clippingOffset.x, clippingOffset.y, mInputTexture.width, mInputTexture.height));

            mComputeShader.Dispatch(kernelIndex, capturedTexture.width / 8, capturedTexture.height / 8, 1);

            info.NotifyRenderTextureIsUpdated();
            return info;
        }
    }
}
