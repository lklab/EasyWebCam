using System;
using System.Collections;
using UnityEngine;

namespace EasyWebCam
{
    public class SinglethreadCaptureWorker : ICaptureWorker
    {
        private WebCamTexture mInputTexture;
        private bool mIsBusy = false;

        public bool IsBusy { get { return mIsBusy; } }

        public SinglethreadCaptureWorker(WebCamTexture texture)
        {
            mInputTexture = texture;
        }

        public CaptureInfo Capture(int rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info)
        {
            CaptureResult result = RunInternal(mInputTexture, rotationAngle, flipHorizontally, clip, viewportAspect);
            return GetCaptureInfoFromResult(result, info);
        }

        public IEnumerator CaptureAsync(int rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info, Action<CaptureInfo> onCompleted)
        {
            onCompleted?.Invoke(Capture(rotationAngle, flipHorizontally, clip, viewportAspect, info));
            yield break;
        }

        private class CaptureResult
        {
            public Color32[] buffer;
            public Vector2Int size;
        }

        private CaptureInfo GetCaptureInfoFromResult(CaptureResult result, CaptureInfo info)
        {
            Texture2D capturedTexture;
            int width = result.size.x;
            int height = result.size.y;

            if (info == null || info.Width != width || info.Height != height)
            {
                info?.Destroy();
                info = new CaptureInfo(width, height, Format.Default);
            }

            info.GetTexture2DRaw(out capturedTexture);

            capturedTexture.SetPixels32(result.buffer);
            capturedTexture.Apply();
            info.NotifyTexture2DIsUpdated();

            return info;
        }

        private CaptureResult RunInternal(WebCamTexture inputTexture,
            int rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            TextureOrientation orientation = Utils.GetTextureOrientation(mInputTexture.videoVerticallyMirrored, rotationAngle, flipHorizontally);
            rotationAngle = orientation.rotationAngle;
            flipHorizontally = orientation.flipHorizontally;

            int rotationStep = Utils.GetRotationStep(rotationAngle);

            Vector2Int clippingOffset;
            if (clip)
                clippingOffset = Utils.GetClippingOffset(inputTexture, rotationStep, viewportAspect);
            else
                clippingOffset = Vector2Int.zero;

            Vector2Int capturedTextureSize = Utils.GetCapturedTextureSize(inputTexture, rotationStep, clippingOffset);

            int textureWidth = inputTexture.width;
            int textureHeight = inputTexture.height;
            int width = capturedTextureSize.x;
            int height = capturedTextureSize.y;
            int widthOffset = clippingOffset.x;
            int heightOffset = clippingOffset.y;

            Color32[] inputBuffer = inputTexture.GetPixels32();
            Color32[] outputBuffer = new Color32[width * height];

            Vector2Int outputTextureSize = new Vector2Int(width, height);

            if (!flipHorizontally)
            {
                switch (rotationStep)
                {
                    case 0:
                        {
                            for (int j = 0; j < height; j++)
                            {
                                int jb = j * width;
                                int c = widthOffset + (j + heightOffset) * textureWidth;
                                Array.Copy(inputBuffer, c, outputBuffer, jb, width);
                            }
                        }
                        break;

                    case 1:
                        {
                            int[] iw = new int[width];
                            for (int i = 0; i < width; i++)
                                iw[i] = i * textureWidth;

                            for (int j = 0; j < height; j++)
                            {
                                int jb = j * width;
                                int c = (textureWidth - 1 - (j + widthOffset)) + heightOffset * textureWidth;
                                for (int i = 0; i < width; i++)
                                    outputBuffer[i + jb] = inputBuffer[c + iw[i]];
                            }
                        }
                        break;

                    case 2:
                        {
                            for (int j = 0; j < height; j++)
                            {
                                int jb = j * width;
                                int c = textureWidth - 1 - widthOffset + (textureHeight - 1 - j - heightOffset) * textureWidth;
                                for (int i = 0; i < width; i++)
                                    outputBuffer[i + jb] = inputBuffer[c - i];
                            }
                        }
                        break;

                    case 3:
                        {
                            int[] iw = new int[width];
                            for (int i = 0; i < width; i++)
                                iw[i] = i * textureWidth;

                            for (int j = 0; j < height; j++)
                            {
                                int jb = j * width;
                                int c = j + widthOffset + (textureHeight - 1 - heightOffset) * textureWidth;
                                for (int i = 0; i < width; i++)
                                    outputBuffer[i + jb] = inputBuffer[c - iw[i]];
                            }
                        }
                        break;
                }
            }
            else
            {
                switch (rotationStep)
                {
                    case 0:
                        {
                            for (int j = 0; j < height; j++)
                            {
                                int jb = j * width;
                                int c = textureWidth - 1 - widthOffset + (j + heightOffset) * textureWidth;
                                for (int i = 0; i < width; i++)
                                    outputBuffer[i + jb] = inputBuffer[c - i];
                            }
                        }
                        break;

                    case 1:
                        {
                            int[] iw = new int[width];
                            for (int i = 0; i < width; i++)
                                iw[i] = i * textureWidth;

                            for (int j = 0; j < height; j++)
                            {
                                int jb = j * width;
                                int c = textureWidth - 1 - j - widthOffset + (textureHeight - 1 - heightOffset) * textureWidth;
                                for (int i = 0; i < width; i++)
                                    outputBuffer[i + jb] = inputBuffer[c - iw[i]];
                            }
                        }
                        break;

                    case 2:
                        {
                            for (int j = 0; j < height; j++)
                            {
                                int jb = j * width;
                                int c = widthOffset + (textureHeight - 1 - (j + heightOffset)) * textureWidth;
                                Array.Copy(inputBuffer, c, outputBuffer, jb, width);
                            }
                        }
                        break;

                    case 3:
                        {
                            int[] iw = new int[width];
                            for (int i = 0; i < width; i++)
                                iw[i] = i * textureWidth;

                            for (int j = 0; j < height; j++)
                            {
                                int jb = j * width;
                                int c = j + widthOffset + heightOffset * textureWidth;
                                for (int i = 0; i < width; i++)
                                    outputBuffer[i + jb] = inputBuffer[c + iw[i]];
                            }
                        }
                        break;
                }
            }

            return new CaptureResult()
            {
                buffer = outputBuffer,
                size = outputTextureSize,
            };
        }
    }
}
