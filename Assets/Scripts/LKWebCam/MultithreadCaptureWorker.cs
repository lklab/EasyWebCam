using System;
using System.Threading;
using UnityEngine;

namespace LKWebCam
{
    /// <summary>
    /// Class that rotates and mirrors the captured image according to the webcam orientation.
    /// </summary>
    public class MultithreadCaptureWorker : ICaptureWorker
    {
        /* config variables */
        private WebCamTexture mInputTexture;
        private int mThreadCount;

        /* thread control variables */
        private bool mIsBusy = false;

        /* thread contexts */
        private Color32[] mInputBuffer;
        private Color32[] mOutputBuffer;
        private Vector2Int mOutputTextureSize;
        private int[] mSlice;

        /* properties */
        public bool IsBusy { get { return mIsBusy; } }

        public MultithreadCaptureWorker(WebCamTexture texture, int threadCount)
        {
            mInputTexture = texture;
            mThreadCount = threadCount;
        }

        public CaptureResult<Texture2D> Capture(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            if (mIsBusy)
                return CaptureResult<Texture2D>.Busy;

            Thread[] threads = RunInternal(rotationAngle, flipHorizontally, clip, viewportAspect);

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            Texture2D outputTexture = new Texture2D(mOutputTextureSize.x, mOutputTextureSize.y);
            outputTexture.SetPixels32(mOutputBuffer);
            outputTexture.Apply();
            return new CaptureResult<Texture2D>(outputTexture);
        }
        
        public CaptureResult<RenderTexture> Capture(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            CaptureResult<Texture2D> result = Capture(rotationAngle, flipHorizontally, clip, viewportAspect);

            if (result.state != CaptureState.Success)
                return new CaptureResult<RenderTexture>(result.state);

            if (texture == null)
                texture = new RenderTexture(mOutputTextureSize.x, mOutputTextureSize.y, 0);

            RenderTexture backup = RenderTexture.active;
            RenderTexture.active = texture;
            Graphics.Blit(result.texture, texture);
            RenderTexture.active = backup;

            return new CaptureResult<RenderTexture>(texture);
        }

        public System.Func<CaptureResult<Texture2D>> CaptureAsync(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            if (mIsBusy)
                return delegate { return CaptureResult<Texture2D>.Busy; };
            mIsBusy = true;

            Texture2D outputTexture = null;
            Thread[] threads = RunInternal(rotationAngle, flipHorizontally, clip, viewportAspect);

            return delegate
            {
                if (outputTexture != null)
                    return new CaptureResult<Texture2D>(outputTexture);

                for (int i = 0; i < threads.Length; i++)
                {
                    if (threads[i].IsAlive)
                        return CaptureResult<Texture2D>.Busy;
                }

                for (int i = 0; i < threads.Length; i++)
                    threads[i].Join();

                outputTexture = new Texture2D(mOutputTextureSize.x, mOutputTextureSize.y);
                outputTexture.SetPixels32(mOutputBuffer);
                outputTexture.Apply();

                mIsBusy = false;
                return new CaptureResult<Texture2D>(outputTexture);
            };
        }
        
        public System.Func<CaptureResult<RenderTexture>> CaptureAsync(RenderTexture texture, float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            RenderTexture outputTexture = null;

            System.Func<CaptureResult<Texture2D>> waitFunc = CaptureAsync(rotationAngle, flipHorizontally, clip, viewportAspect);
            CaptureResult<Texture2D> result = waitFunc();

            if (result.state != CaptureState.Working || result.state != CaptureState.Success)
                return delegate { return new CaptureResult<RenderTexture>(result.state); };
            
            return delegate
            {
                if (outputTexture != null)
                    return new CaptureResult<RenderTexture>(outputTexture);

                CaptureResult<Texture2D> result = waitFunc();

                if (result.state == CaptureState.Success)
                {
                    
                }
            };
        }

        private Thread[] RunInternal(float rotationAngle, bool flipHorizontally, bool clip, float viewportAspect)
        {
            int rotationStep = Mathf.RoundToInt(rotationAngle / 90.0f);
            if (rotationStep >= 4)
                rotationStep = rotationStep % 4;
            else if (rotationStep < 0)
                rotationStep += -((rotationStep + 1) / 4 - 1) * 4;

            int textureWidth = mInputTexture.width;
            int textureHeight = mInputTexture.height;
            int width = 0;
            int height = 0;
            int widthOffset = 0;
            int heightOffset = 0;

            switch (rotationStep)
            {
                case 0:
                case 2:
                    width = textureWidth;
                    height = textureHeight;
                    break;

                case 1:
                case 3:
                    width = textureHeight;
                    height = textureWidth;
                    break;
            }

            if (clip)
            {
                float textureAspect = (float)width / height;

                if (viewportAspect > textureAspect)
                {
                    int newHeight = (int)((float)width / viewportAspect);
                    heightOffset = (height - newHeight) / 2;
                    height = newHeight;
                }
                else
                {
                    int newWidth = (int)((float)height * viewportAspect);
                    widthOffset = (width - newWidth) / 2;
                    width = newWidth;
                }
            }

            mInputBuffer = mInputTexture.GetPixels32();
            mOutputBuffer = new Color32[width * height];
            mOutputTextureSize = new Vector2Int(width, height);

            Thread[] threads = new Thread[mThreadCount];
            mSlice = GetSlice(height, mThreadCount);

            if (!flipHorizontally)
            {
                switch (rotationStep)
                {
                    case 0:
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = mSlice[index];
                                int end = mSlice[index + 1];

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = widthOffset + (j + heightOffset) * textureWidth;
                                    Array.Copy(mInputBuffer, c, mOutputBuffer, jb, width);
                                }
                            });
                        }
                        break;

                    case 1:
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = mSlice[index];
                                int end = mSlice[index + 1];

                                int[] iw = new int[width];
                                for (int i = 0; i < width; i++)
                                    iw[i] = i * textureWidth;

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = (textureWidth - 1 - (j + heightOffset)) + widthOffset * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        mOutputBuffer[i + jb] = mInputBuffer[c + iw[i]];
                                }
                            });
                        }
                        break;

                    case 2:
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = mSlice[index];
                                int end = mSlice[index + 1];

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = textureWidth - 1 - widthOffset + (textureHeight - 1 - j - heightOffset) * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        mOutputBuffer[i + jb] = mInputBuffer[c - i];
                                }
                            });
                        }
                        break;

                    case 3:
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = mSlice[index];
                                int end = mSlice[index + 1];

                                int[] iw = new int[width];
                                for (int i = 0; i < width; i++)
                                    iw[i] = i * textureWidth;

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = j + heightOffset + (textureHeight - 1 - widthOffset) * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        mOutputBuffer[i + jb] = mInputBuffer[c - iw[i]];
                                }
                            });
                        }
                        break;
                }
            }
            else
            {
                switch (rotationStep)
                {
                    case 0:
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = mSlice[index];
                                int end = mSlice[index + 1];

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = textureWidth - 1 - widthOffset + (j + heightOffset) * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        mOutputBuffer[i + jb] = mInputBuffer[c - i];
                                }
                            });
                        }
                        break;

                    case 1:
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = mSlice[index];
                                int end = mSlice[index + 1];

                                int[] iw = new int[width];
                                for (int i = 0; i < width; i++)
                                    iw[i] = i * textureWidth;

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = textureWidth - 1 - j - heightOffset + (textureHeight - 1 - widthOffset) * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        mOutputBuffer[i + jb] = mInputBuffer[c - iw[i]];
                                }
                            });
                        }
                        break;

                    case 2:
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = mSlice[index];
                                int end = mSlice[index + 1];

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = widthOffset + (textureHeight - 1 - (j + heightOffset)) * textureWidth;
                                    Array.Copy(mInputBuffer, c, mOutputBuffer, jb, width);
                                }
                            });
                        }
                        break;

                    case 3:
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = mSlice[index];
                                int end = mSlice[index + 1];

                                int[] iw = new int[width];
                                for (int i = 0; i < width; i++)
                                    iw[i] = i * textureWidth;

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = j + heightOffset + widthOffset * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        mOutputBuffer[i + jb] = mInputBuffer[c + iw[i]];
                                }
                            });
                        }
                        break;
                }
            }

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            return threads;
        }

        private int[] GetSlice(int length, int count)
        {
            int[] slice = new int[count + 1];
            int unit = length / count;

            for (int i = 0; i < count; i++)
                slice[i] = unit * i;

            slice[count] = length;

            return slice;
        }
    }
}
