using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LKWebCam
{
    /// <summary>
    /// Class that rotates and mirrors the captured image according to the webcam orientation.
    /// </summary>
    public class WebCamCaptureWorker
    {
        private WebCamTexture mInputTexture;
        private float mRotationAngle;
        private bool mFlipHorizontally;
        private bool mClip;
        private float mViewportAspect;
        private int mThreadCount;

        private Thread[] threads;

        private Color32[] mInputBuffer;
        private Color32[] mOutputBuffer;
        private Texture2D mOutputTexture = null;

        public WebCamCaptureWorker(WebCamTexture texture, float rotationAngle, bool flipHorizontally,
            bool clip, float viewportAspect, int threadCount)
        {
            mInputTexture = texture;
            mRotationAngle = rotationAngle;
            mFlipHorizontally = flipHorizontally;
            mClip = clip;
            mViewportAspect = viewportAspect;
            mThreadCount = threadCount;
        }

        public System.Func<Texture2D> RunAsync()
        {
            mOutputTexture = null;
            threads = RunInternal();

            return delegate
            {
                for (int i = 0; i < threads.Length; i++)
                {
                    if (threads[i].IsAlive)
                        return null;
                }

                for (int i = 0; i < threads.Length; i++)
                    threads[i].Join();

                mOutputTexture.SetPixels32(mOutputBuffer);
                mOutputTexture.Apply();
                return mOutputTexture;
            };
        }

        public Texture2D Run()
        {
            mOutputTexture = null;
            threads = RunInternal();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            mOutputTexture.SetPixels32(mOutputBuffer);
            mOutputTexture.Apply();
            return mOutputTexture;
        }

        private Thread[] RunInternal()
        {
            int rotationStep = Mathf.RoundToInt(mRotationAngle / 90.0f);
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

            if (mClip)
            {
                float textureAspect = (float)width / height;

                if (mViewportAspect > textureAspect)
                {
                    int newHeight = (int)((float)width / mViewportAspect);
                    heightOffset = (height - newHeight) / 2;
                    height = newHeight;
                }
                else
                {
                    int newWidth = (int)((float)height * mViewportAspect);
                    widthOffset = (width - newWidth) / 2;
                    width = newWidth;
                }
            }

            mInputBuffer = mInputTexture.GetPixels32();
            mOutputBuffer = new Color32[width * height];
            mOutputTexture = new Texture2D(width, height);

            Thread[] threads = new Thread[mThreadCount];
            int[] starts;
            int[] ends;

            if (!mFlipHorizontally)
            {
                switch (rotationStep)
                {
                    case 0:
                        GetSlice(height, mThreadCount, out starts, out ends);
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                for (int j = starts[index]; j < ends[index]; j++)
                                {
                                    int jb = j * width;
                                    int ybase = (j + heightOffset) * textureWidth;
                                    for (int i = 0; i < width; i++)
                                        mOutputBuffer[i + jb] = mInputBuffer[(i + widthOffset) + ybase];
                                }
                            });
                        }
                        break;

                    case 1:
                        GetSlice(height, mThreadCount, out starts, out ends);
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                for (int j = starts[index]; j < ends[index]; j++)
                                {
                                    int jb = j * width;
                                    int x = textureWidth - 1 - (j + heightOffset);
                                    for (int i = 0; i < width; i++)
                                        mOutputBuffer[i + jb] = mInputBuffer[x + (i + widthOffset) * textureWidth];
                                }
                            });
                        }
                        break;

                    case 2:
                        GetSlice(width, mThreadCount, out starts, out ends);
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                for (int i = starts[index]; i < ends[index]; i++)
                                {
                                    int x = textureWidth - 1 - (i + widthOffset);
                                    for (int j = 0; j < height; j++)
                                        mOutputBuffer[i + j * width] = mInputBuffer[x + (textureHeight - 1 - (j + heightOffset)) * textureWidth];
                                }
                            });
                        }
                        break;

                    case 3:
                        GetSlice(width, mThreadCount, out starts, out ends);
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                for (int i = starts[index]; i < ends[index]; i++)
                                {
                                    int y = textureHeight - 1 - (i + widthOffset);
                                    for (int j = 0; j < height; j++)
                                        mOutputBuffer[i + j * width] = mInputBuffer[(j + heightOffset) + y * textureWidth];
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
                        GetSlice(width, mThreadCount, out starts, out ends);
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                for (int i = starts[index]; i < ends[index]; i++)
                                {
                                    int x = textureWidth - 1 - (i + widthOffset);
                                    for (int j = 0; j < height; j++)
                                        mOutputBuffer[i + j * width] = mInputBuffer[x + (j + heightOffset) * textureWidth];
                                }
                            });
                        }
                        break;

                    case 1:
                        GetSlice(width, mThreadCount, out starts, out ends);
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                for (int i = starts[index]; i < ends[index]; i++)
                                {
                                    int y = textureHeight - 1 - (i + widthOffset);
                                    for (int j = 0; j < height; j++)
                                        mOutputBuffer[i + j * width] = mInputBuffer[(textureWidth - 1 - (j + heightOffset)) + y * textureWidth];
                                }
                            });
                        }
                        break;

                    case 2:
                        GetSlice(height, mThreadCount, out starts, out ends);
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                for (int j = starts[index]; j < ends[index]; j++)
                                {
                                    int jb = j * width;
                                    int y = textureHeight - 1 - (j + heightOffset);
                                    for (int i = 0; i < width; i++)
                                        mOutputBuffer[i + jb] = mInputBuffer[(i + widthOffset) + y * textureWidth];
                                }
                            });
                        }
                        break;

                    case 3:
                        GetSlice(width, mThreadCount, out starts, out ends);
                        for (int th = 0; th < mThreadCount; th++)
                        {
                            int index = th;
                            threads[th] = new Thread(() =>
                            {
                                for (int i = starts[index]; i < ends[index]; i++)
                                {
                                    int ybase = (i + widthOffset) * textureWidth;
                                    for (int j = 0; j < height; j++)
                                        mOutputBuffer[i + j * width] = mInputBuffer[(j + heightOffset) + ybase];
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

        private void GetSlice(int length, int count, out int[] starts, out int[] ends)
        {
            starts = new int[count];
            ends = new int[count];
            int slice = length / count;

            for (int i = 0; i < count; i++)
            {
                starts[i] = slice * i;
                ends[i] = slice * (i + 1);
            }

            ends[count - 1] = length;
        }
    }
}
