using System;
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

        private Thread[] mThreads = null;
        private Texture2D mOutputTexture = null;
        private Vector2Int mOutputTextureSize;

        /* thread contexts */
        private Color32[] mInputBuffer;
        private Color32[] mOutputBuffer;
        private int[] mSlice;

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
            if (mOutputTexture != null)
                return delegate { return mOutputTexture; };

            if (mThreads == null)
                mThreads = RunInternal();

            return delegate
            {
                if (mOutputTexture != null)
                    return mOutputTexture;

                for (int i = 0; i < mThreads.Length; i++)
                {
                    if (mThreads[i].IsAlive)
                        return null;
                }

                for (int i = 0; i < mThreads.Length; i++)
                    mThreads[i].Join();

                mOutputTexture = new Texture2D(mOutputTextureSize.x, mOutputTextureSize.y);
                mOutputTexture.SetPixels32(mOutputBuffer);
                mOutputTexture.Apply();
                return mOutputTexture;
            };
        }

        public Texture2D Run()
        {
            if (mOutputTexture != null)
                return mOutputTexture;

            if (mThreads != null)
                return null; /* busy. This code will probably never run. */

            mThreads = RunInternal();

            for (int i = 0; i < mThreads.Length; i++)
                mThreads[i].Join();

            mOutputTexture = new Texture2D(mOutputTextureSize.x, mOutputTextureSize.y);
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
            mOutputTextureSize = new Vector2Int(width, height);

            Thread[] threads = new Thread[mThreadCount];
            mSlice = GetSlice(height, mThreadCount);

            if (!mFlipHorizontally)
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
