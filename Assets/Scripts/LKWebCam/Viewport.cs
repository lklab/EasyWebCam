using UnityEngine;
using UnityEngine.UI;

namespace LKWebCam
{
    public class Viewport : MonoBehaviour
    {
        [Header("UI components")]
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private AspectRatioFitter _aspectRatioFitter;

        private WebCamTexture mTexture = null;
        private WebCamProperties mWebCamProperties;
        private ScreenOrientation mCurrentOrientation = ScreenOrientation.Portrait;

        public RectTransform RectTr { get { return _viewport; } }
        public Vector2 Size { get { return new Vector2(_viewport.rect.width, _viewport.rect.height); } }
        public float AspectRatio { get { return _viewport.rect.width / _viewport.rect.height; } }
        public WebCamProperties WebCamProperties { get { return mWebCamProperties; } }

        private void Update()
        {
            /* check current webcam properties and current orientation */
            if (mWebCamProperties != new WebCamProperties(mTexture) ||
                mCurrentOrientation != Screen.orientation)
            {
                ResizeInternal();
            }
        }

        /// <summary>
        /// Set the WebCamTexture to show
        /// </summary>
        public void SetWebCamTexture(WebCamTexture texture)
        {
            mTexture = texture;

            _rawImage.texture = mTexture;
            _rawImage.gameObject.SetActive(true);

            ResizeInternal();
        }

        /// <summary>
        /// Clear current WebCamTexture
        /// </summary>
        public void ClearWebCamTexture()
        {
            mTexture = null;
            _rawImage.gameObject.SetActive(false);
            SetAutoResizingEnabled(false);
        }

        /// <summary>
        /// Enable/Disable auto resizing
        /// </summary>
        public void SetAutoResizingEnabled(bool enabled)
        {
            if (enabled && mTexture == null)
            {
                Debug.LogError("To enable the Auto Resizing, first set the WebCamTexture.");
                return;
            }

            this.enabled = enabled;
        }

        /// <summary>
        /// Resize UI
        /// </summary>
        public void Resize()
        {
            if (mTexture == null)
                return;

            ResizeInternal();
        }

        private void ResizeInternal()
        {
            /* setup params */
            float rotationAngle = mTexture.videoRotationAngle;
            int rotationStep = Mathf.RoundToInt(rotationAngle / 90.0f);
            bool isOrthogonal = (rotationStep % 2) != 0;
            float scale = 1.0f;
            float aspectRatio = (float)mTexture.width / mTexture.height;

            /* rotation */
            float angle = rotationStep * 90.0f;
            _rawImage.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -angle);

            /* size */
            _aspectRatioFitter.aspectRatio = aspectRatio;

            /* scale */
            if (isOrthogonal)
            {
                float viewportRatio = _viewport.rect.width / _viewport.rect.height;
                scale = Mathf.Max(1.0f / aspectRatio, viewportRatio);
            }

            /* flip */
            if (mTexture.videoVerticallyMirrored)
                _rawImage.transform.localScale = new Vector3(scale, -scale, scale);
            else
                _rawImage.transform.localScale = new Vector3(scale, scale, scale);

            /* save webcam properties */
            mWebCamProperties = new WebCamProperties(mTexture);
            mCurrentOrientation = Screen.orientation;
        }
    }

    public struct WebCamProperties
    {
        public int videoRotationAngle;
        public bool videoVerticallyMirrored;
        public int width;
        public int height;

        public WebCamProperties(WebCamTexture texture)
        {
            videoRotationAngle = texture.videoRotationAngle;
            videoVerticallyMirrored = texture.videoVerticallyMirrored;
            width = texture.width;
            height = texture.height;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is WebCamProperties))
                return false;

            WebCamProperties other = (WebCamProperties)obj;

            return this == other;
        }

        public override int GetHashCode()
        {
            return videoRotationAngle.GetHashCode() +
                videoVerticallyMirrored.GetHashCode() +
                width.GetHashCode() +
                height.GetHashCode();
        }

        public static bool operator ==(WebCamProperties p1, WebCamProperties p2)
        {
            return p1.videoRotationAngle == p2.videoRotationAngle &&
                p1.videoVerticallyMirrored == p2.videoVerticallyMirrored &&
                p1.width == p2.width &&
                p1.height == p2.height;
        }

        public static bool operator !=(WebCamProperties p1, WebCamProperties p2)
        {
            return !(p1 == p2);
        }
    }
}
