using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace EasyWebCam
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

        /* properties */
        public bool IsBusy { get { return mIsBusy; } }

        public MultithreadCaptureWorker(WebCamTexture texture, int threadCount)
        {
            mInputTexture = texture;
            mThreadCount = threadCount;
        }

        public CaptureInfo Capture(int rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info)
        {
            if (mIsBusy)
                return CaptureInfo.Busy;

            ThreadContext context = RunInternal(mInputTexture, mThreadCount, rotationAngle, flipHorizontally, clip, viewportAspect);

            for (int i = 0; i < context.threads.Length; i++)
                context.threads[i].Join();

            return GetCaptureInfoFromResult(context, info);
        }

        public IEnumerator CaptureAsync(int rotationAngle, bool flipHorizontally, bool clip, float viewportAspect, CaptureInfo info, Action<CaptureInfo> onCompleted)
        {
            if (mIsBusy)
            {
                onCompleted?.Invoke(CaptureInfo.Busy);
                yield break;
            }
            mIsBusy = true;

            ThreadContext context = RunInternal(mInputTexture, mThreadCount, rotationAngle, flipHorizontally, clip, viewportAspect);

            bool working = true;
            while (working)
            {
                yield return null;

                working = false;
                for (int i = 0; i < context.threads.Length; i++)
                {
                    if (context.threads[i].IsAlive)
                    {
                        working = true;
                        break;
                    }
                }
            }

            for (int i = 0; i < context.threads.Length; i++)
                context.threads[i].Join();

            mIsBusy = false;
            onCompleted?.Invoke(GetCaptureInfoFromResult(context, info));
        }

        private class ThreadContext
        {
            public Thread[] threads;

            public Color32[] inputBuffer;
            public Color32[] outputBuffer;

            public Vector2Int outputTextureSize;

            public int[] slice;
        }

        private CaptureInfo GetCaptureInfoFromResult(ThreadContext context, CaptureInfo info)
        {
            Texture2D capturedTexture;
            int width = context.outputTextureSize.x;
            int height = context.outputTextureSize.y;

            if (info == null || info.Width != width || info.Height != height)
            {
                info?.Destroy();
                info = new CaptureInfo(width, height, Format.Default);
            }

            info.GetTexture2DRaw(out capturedTexture);

            capturedTexture.SetPixels32(context.outputBuffer);
            capturedTexture.Apply();
            info.NotifyTexture2DIsUpdated();

            return info;
        }

        private ThreadContext RunInternal(WebCamTexture inputTexture, int threadCount,
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

            ThreadContext context = new ThreadContext()
            {
                threads = new Thread[threadCount],

                inputBuffer = inputTexture.GetPixels32(),
                outputBuffer = new Color32[width * height],

                outputTextureSize = new Vector2Int(width, height),

                slice = GetSlice(height, threadCount),
            };

            Thread[] threads = context.threads;

            if (!flipHorizontally)
            {
                switch (rotationStep)
                {
                    case 0:
                        for (int th = 0; th < threadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = context.slice[index];
                                int end = context.slice[index + 1];

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = widthOffset + (j + heightOffset) * textureWidth;
                                    Array.Copy(context.inputBuffer, c, context.outputBuffer, jb, width);
                                }
                            });
                        }
                        break;

                    case 1:
                        for (int th = 0; th < threadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = context.slice[index];
                                int end = context.slice[index + 1];

                                int[] iw = new int[width];
                                for (int i = 0; i < width; i++)
                                    iw[i] = i * textureWidth;

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = (textureWidth - 1 - (j + widthOffset)) + heightOffset * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        context.outputBuffer[i + jb] = context.inputBuffer[c + iw[i]];
                                }
                            });
                        }
                        break;

                    case 2:
                        for (int th = 0; th < threadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = context.slice[index];
                                int end = context.slice[index + 1];

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = textureWidth - 1 - widthOffset + (textureHeight - 1 - j - heightOffset) * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        context.outputBuffer[i + jb] = context.inputBuffer[c - i];
                                }
                            });
                        }
                        break;

                    case 3:
                        for (int th = 0; th < threadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = context.slice[index];
                                int end = context.slice[index + 1];

                                int[] iw = new int[width];
                                for (int i = 0; i < width; i++)
                                    iw[i] = i * textureWidth;

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = j + widthOffset + (textureHeight - 1 - heightOffset) * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        context.outputBuffer[i + jb] = context.inputBuffer[c - iw[i]];
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
                        for (int th = 0; th < threadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = context.slice[index];
                                int end = context.slice[index + 1];

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = textureWidth - 1 - widthOffset + (j + heightOffset) * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        context.outputBuffer[i + jb] = context.inputBuffer[c - i];
                                }
                            });
                        }
                        break;

                    case 1:
                        for (int th = 0; th < threadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = context.slice[index];
                                int end = context.slice[index + 1];

                                int[] iw = new int[width];
                                for (int i = 0; i < width; i++)
                                    iw[i] = i * textureWidth;

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = textureWidth - 1 - j - widthOffset + (textureHeight - 1 - heightOffset) * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        context.outputBuffer[i + jb] = context.inputBuffer[c - iw[i]];
                                }
                            });
                        }
                        break;

                    case 2:
                        for (int th = 0; th < threadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = context.slice[index];
                                int end = context.slice[index + 1];

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = widthOffset + (textureHeight - 1 - (j + heightOffset)) * textureWidth;
                                    Array.Copy(context.inputBuffer, c, context.outputBuffer, jb, width);
                                }
                            });
                        }
                        break;

                    case 3:
                        for (int th = 0; th < threadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                int start = context.slice[index];
                                int end = context.slice[index + 1];

                                int[] iw = new int[width];
                                for (int i = 0; i < width; i++)
                                    iw[i] = i * textureWidth;

                                for (int j = start; j < end; j++)
                                {
                                    int jb = j * width;
                                    int c = j + widthOffset + heightOffset * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        context.outputBuffer[i + jb] = context.inputBuffer[c + iw[i]];
                                }
                            });
                        }
                        break;
                }
            }

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            return context;
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
