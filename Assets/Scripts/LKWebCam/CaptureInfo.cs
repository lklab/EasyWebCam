using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LKWebCam
{
    /// <summary>
    /// Represents the state of a capture operation.
    /// </summary>
    public enum CaptureState { Success, NotPlaying, Busy, Destroyed }

    /// <summary>
    /// Stores information about a captured photo.
    /// </summary>
    public class CaptureInfo
    {
        /// <summary>
        /// Gets the current state of the capture.
        /// </summary>
        public CaptureState State { get; private set; }

        /// <summary>
        /// Predefined CaptureInfo instance for a busy state.
        /// </summary>
        public static CaptureInfo Busy { get; private set; } = new CaptureInfo(CaptureState.Busy);

        /// <summary>
        /// Predefined CaptureInfo instance for a not playing state.
        /// </summary>
        public static CaptureInfo NotPlaying { get; private set; } = new CaptureInfo(CaptureState.NotPlaying);

        private Texture2D mTexture2D;
        private RenderTexture mRenderTexture;

        /// <summary>
        /// Initializes a new instance of the CaptureInfo class with the specified state.
        /// </summary>
        /// <param name="state">The state of the capture.</param>
        public CaptureInfo(CaptureState state)
        {
            State = state;
            mTexture2D = null;
            mRenderTexture = null;
        }

        /// <summary>
        /// Initializes a new instance of the CaptureInfo class with a Texture2D.
        /// </summary>
        /// <param name="texture">The Texture2D to initialize the CaptureInfo with.</param>
        public CaptureInfo(Texture2D texture)
        {
            State = CaptureState.Success;
            mTexture2D = texture;
            mRenderTexture = null;
        }

        /// <summary>
        /// Initializes a new instance of the CaptureInfo class with a RenderTexture.
        /// </summary>
        /// <param name="texture">The RenderTexture to initialize the CaptureInfo with.</param>
        public CaptureInfo(RenderTexture texture)
        {
            State = CaptureState.Success;
            mTexture2D = null;
            mRenderTexture = texture;
            mRenderTexture.enableRandomWrite = true;
        }

        /// <summary>
        /// Gets the Texture2D of the captured photo.
        /// </summary>
        /// <returns>The captured Texture2D.</returns>
        public Texture2D GetTexture2D()
        {
            if (State != CaptureState.Success)
                return null;

            if (mTexture2D == null)
            {
                mTexture2D = new Texture2D(mRenderTexture.width, mRenderTexture.height);
                NotifyRenderTextureIsUpdated();
            }

            return mTexture2D;
        }

        /// <summary>
        /// Gets the RenderTexture of the captured photo.
        /// </summary>
        /// <returns>The captured RenderTexture.</returns>
        public RenderTexture GetRenderTexture()
        {
            if (State != CaptureState.Success)
                return null;

            if (mRenderTexture == null)
            {
                mRenderTexture = new RenderTexture(mTexture2D.width, mTexture2D.height, 0);
                mRenderTexture.enableRandomWrite = true;
                NotifyTexture2DIsUpdated();
            }

            return mRenderTexture;
        }

        /// <summary>
        /// Gets the size of the captured texture.
        /// </summary>
        /// <returns>The size of the captured texture.</returns>
        public Vector2Int GetTextureSize()
        {
            if (State != CaptureState.Success)
                return Vector2Int.zero;

            if (mTexture2D != null)
                return new Vector2Int(mTexture2D.width, mTexture2D.height);
            else if (mRenderTexture != null)
                return new Vector2Int(mRenderTexture.width, mRenderTexture.height);

            return Vector2Int.zero;
        }

        /// <summary>
        /// Updates the RenderTexture with changes from the Texture2D.
        /// </summary>
        public void NotifyTexture2DIsUpdated()
        {
            if (mTexture2D != null && mRenderTexture != null)
                Graphics.Blit(mTexture2D, mRenderTexture);
        }

        /// <summary>
        /// Updates the Texture2D with changes from the RenderTexture.
        /// </summary>
        public void NotifyRenderTextureIsUpdated()
        {
            if (mTexture2D != null && mRenderTexture != null)
            {
                RenderTexture activeRenderTexture = RenderTexture.active;
                RenderTexture.active = mRenderTexture;
                mTexture2D.ReadPixels(new Rect(0, 0, mRenderTexture.width, mRenderTexture.height), 0, 0);
                mTexture2D.Apply();
                RenderTexture.active = activeRenderTexture;
            }
        }

        /// <summary>
        /// Releases memory by destroying stored textures.
        /// </summary>
        public void Destroy()
        {
            if (mTexture2D != null)
                Object.Destroy(mTexture2D);

            if (mRenderTexture != null)
                Object.Destroy(mRenderTexture);

            State = CaptureState.Destroyed;
        }
    }
}
