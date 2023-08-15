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

        public static Vector2Int GetCapturedTextureSize(Texture texture, int rotationStep)
        {
            if (rotationStep % 2 == 0)
                return new Vector2Int(texture.width, texture.height);
            else
                return new Vector2Int(texture.height, texture.width);
        }
    }
}
