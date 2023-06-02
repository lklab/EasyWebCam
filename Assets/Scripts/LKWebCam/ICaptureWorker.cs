using UnityEngine;

namespace LKWebCam
{
    public enum CaptureState { Working, Success, Fail, Busy }

    public struct CaptureResult<T> where T : Texture
    {
        public CaptureState state;

        public T texture;

        public CaptureResult(CaptureState state)
        {
            this.state = state;
            this.texture = null;
        }

        public CaptureResult(T texture)
        {
            this.state = CaptureState.Success;
            this.texture = texture;
        }

        public static CaptureResult<T> Fail { get; private set; } = new CaptureResult<T>
        {
            state = CaptureState.Fail,
            texture = null,
        };

        public static CaptureResult<T> Busy { get; private set; } = new CaptureResult<T>
        {
            state = CaptureState.Busy,
            texture = null,
        };
    }

    public interface ICaptureWorker
    {
        public bool IsBusy { get; }

        public CaptureResult<Texture2D> Capture(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect);
        
        public CaptureResult<RenderTexture> Capture(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect);

        public System.Func<CaptureResult<Texture2D>> CaptureAsync(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect);
        
        public System.Func<CaptureResult<RenderTexture>> CaptureAsync(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect);
    }
}
