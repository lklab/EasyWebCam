using UnityEngine;

namespace LKWebCam
{
    public static class Utils
    {
        public static int GetRotationStep(float rotationAngle)
        {
            int rotationStep = Mathf.RoundToInt(rotationAngle / 90.0f);
            if (rotationStep >= 4)
                rotationStep = rotationStep % 4;
            else if (rotationStep < 0)
                rotationStep += -((rotationStep + 1) / 4 - 1) * 4;

            return rotationStep;
        }

        public static Vector2Int GetCapturedTextureSize(Texture texture, int rotationStep, Vector2Int clippingOffset)
        {
            int clippedWidth = texture.width - clippingOffset.x * 2;
            int clippedHeight = texture.height - clippingOffset.y * 2;

            if (rotationStep % 2 == 0)
                return new Vector2Int(clippedWidth, clippedHeight);
            else
                return new Vector2Int(clippedHeight, clippedWidth);
        }

        public static Vector2Int GetClippingOffset(Texture inputTexture, int rotationStep, float outputAspect)
        {
            int inputWidth = inputTexture.width;
            int inputHeight = inputTexture.height;

            float inputAspect = (float)inputWidth / inputHeight;

            if (rotationStep % 2 != 0)
                outputAspect = 1.0f / outputAspect;

            if (outputAspect > inputAspect)
            {
                int outputHeight = (int)((float)inputWidth / outputAspect);
                int offsetY = (inputHeight - outputHeight) / 2;
                return new Vector2Int(0, offsetY);
            }
            else
            {
                int outputWidth = (int)((float)inputHeight * outputAspect);
                int offsetX = (inputWidth - outputWidth) / 2;
                return new Vector2Int(offsetX, 0);
            }
        }
    }
}
