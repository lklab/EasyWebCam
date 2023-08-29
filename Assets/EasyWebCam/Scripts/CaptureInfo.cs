using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyWebCam
{
    /// <summary>
    /// Represents the state of a capture operation.
    /// </summary>
    public enum CaptureState { Success, NotPlaying, Busy, Destroyed }

    /// <summary>
    /// Represents the format of a captured photo.
    /// </summary>
    public enum Format { Default, Half }

    /// <summary>
    /// Stores information about a captured photo.
    /// </summary>
    public class CaptureInfo
    {
        /// <summary>
        /// Predefined CaptureInfo instance for a busy state.
        /// </summary>
        internal static CaptureInfo Busy { get; private set; } = new CaptureInfo(CaptureState.Busy);

        /// <summary>
        /// Predefined CaptureInfo instance for a not playing state.
        /// </summary>
        internal static CaptureInfo NotPlaying { get; private set; } = new CaptureInfo(CaptureState.NotPlaying);

        /// <summary>
        /// Gets the current state of the capture.
        /// </summary>
        public CaptureState State { get; private set; }

        /// <summary>
        /// Gets the format of the captured photo.
        /// </summary>
        public Format Format { get; private set; } = Format.Default;

        /// <summary>
        /// Gets the width of the captured photo.
        /// </summary>
        public int Width { get; private set; } = 0;

        /// <summary>
        /// Gets the height of the captured photo.
        /// </summary>
        public int Height { get; private set; } = 0;

        private Texture2D mTexture2D = null;
        private RenderTexture mRenderTexture = null;

        /// <summary>
        /// Initializes a new instance of the CaptureInfo class with the specified state.
        /// </summary>
        /// <param name="state">The state of the capture.</param>
        internal CaptureInfo(CaptureState state)
        {
            State = state;
        }

        /// <summary>
        /// Initializes a new instance of the CaptureInfo class with the specified dimensions and format.
        /// </summary>
        /// <param name="width">The width of the captured photo.</param>
        /// <param name="height">The height of the captured photo.</param>
        /// <param name="format">The format of the captured photo.</param>
        internal CaptureInfo(int width, int height, Format format)
        {
            State = CaptureState.Success;

            Width = width;
            Height = height;

            Format = format;
        }

        /// <summary>
        /// Updates the RenderTexture with changes from the Texture2D.
        /// </summary>
        internal void NotifyTexture2DIsUpdated()
        {
            if (mTexture2D != null && mRenderTexture != null)
                Graphics.Blit(mTexture2D, mRenderTexture);
        }

        /// <summary>
        /// Updates the Texture2D with changes from the RenderTexture.
        /// </summary>
        internal void NotifyRenderTextureIsUpdated()
        {
            if (mTexture2D != null && mRenderTexture != null)
            {
                RenderTexture activeRenderTexture = RenderTexture.active;
                RenderTexture.active = mRenderTexture;
                mTexture2D.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                mTexture2D.Apply();
                RenderTexture.active = activeRenderTexture;
            }
        }

        /// <summary>
        /// Gets the Texture2D of the captured photo.
        /// If the Texture2D is not yet created, this method creates it.
        /// </summary>
        /// <param name="texture">The captured Texture2D if created or reused.</param>
        /// <returns>Returns true if the Texture2D was newly created; otherwise, false.</returns>
        internal bool GetTexture2DRaw(out Texture2D texture)
        {
            bool isNew = false;
            texture = null;

            if (State != CaptureState.Success)
                return false;

            if (mTexture2D == null)
            {
                mTexture2D = CreateTexture2D();
                isNew = true;
            }

            texture = mTexture2D;
            return isNew;
        }

        /// <summary>
        /// Gets the RenderTexture of the captured photo.
        /// If the RenderTexture is not yet created, this method creates it.
        /// </summary>
        /// <param name="texture">The captured RenderTexture if created or reused.</param>
        /// <returns>Returns true if the RenderTexture was newly created; otherwise, false.</returns>
        internal bool GetRenderTextureRaw(out RenderTexture texture)
        {
            bool isNew = false;
            texture = null;

            if (State != CaptureState.Success)
                return false;

            if (mRenderTexture == null)
            {
                mRenderTexture = CreateRenderTexture();
                isNew = true;
            }

            texture = mRenderTexture;
            return isNew;
        }

        private Texture2D CreateTexture2D()
        {
            switch (Format)
            {
                case Format.Default:
                default:
                    return new Texture2D(Width, Height);

                case Format.Half:
                    return new Texture2D(Width, Height, TextureFormat.RGBAHalf, false);
            }
        }

        private RenderTexture CreateRenderTexture()
        {
            RenderTexture renderTexture;

            switch (Format)
            {
                case Format.Default:
                default:
                    renderTexture = new RenderTexture(Width, Height, 0);
                    break;

                case Format.Half:
                    renderTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBHalf);
                    break;
            }
            renderTexture.enableRandomWrite = true;

            return renderTexture;
        }

        /// <summary>
        /// Gets the Texture2D of the captured photo.
        /// </summary>
        /// <returns>The captured Texture2D.</returns>
        public Texture2D GetTexture2D()
        {
            if (GetTexture2DRaw(out Texture2D texture))
                NotifyRenderTextureIsUpdated();

            return texture;
        }

        /// <summary>
        /// Gets the RenderTexture of the captured photo.
        /// </summary>
        /// <returns>The captured RenderTexture.</returns>
        public RenderTexture GetRenderTexture()
        {
            if (GetRenderTextureRaw(out RenderTexture texture))
                NotifyTexture2DIsUpdated();

            return texture;
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
