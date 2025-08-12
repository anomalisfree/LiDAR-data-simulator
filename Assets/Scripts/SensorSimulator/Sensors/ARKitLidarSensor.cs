using UnityEngine;
using SensorSimulator.Interfaces;
using UnityEngine.UI;

namespace SensorSimulator.Sensors
{
    [RequireComponent(typeof(Camera))]
    public class ARKitLidarSensor : BaseSensor, IARKitLidar
    {
        private Camera depthCamera;
        private RenderTexture depthTexture;      // Исходная текстура глубины
        private RenderTexture visualDepthTexture; // Текстура для визуализации
        private float[,] depthData;
        private Material depthMaterial;

        [SerializeField]
        private float minDepth = 0.1f;
        [SerializeField]
        private float maxDepth = 5.0f; // ARKit LiDAR обычно работает до 5 метров

        [SerializeField]
        private RawImage depthMapPreview;
        public override void Initialize()
        {
            depthCamera = GetComponent<Camera>();

            // // Создаем текстуру глубины
            depthTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.Depth);
            depthTexture.Create();

            // Создаем текстуру для визуализации глубины
            visualDepthTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
            visualDepthTexture.Create();
            depthMapPreview.texture = visualDepthTexture;

            // Создаем материал для визуализации глубины
            depthMaterial = new Material(Shader.Find("Custom/DepthMapShader"));

            // Настраиваем камеру
            depthCamera.targetTexture = depthTexture;
            depthCamera.depthTextureMode = DepthTextureMode.Depth;

            depthData = new float[textureHeight, textureWidth];
            base.Initialize();
        }

        public override void UpdateSensor()
        {
            if (!isInitialized) return;
            UpdateDepthData();
        }

        private void UpdateDepthData()
        {
            // Ждем завершения рендеринга камеры
            depthCamera.Render();

            // Визуализируем глубину через шейдер
            depthMaterial.SetTexture("_DepthTex", depthTexture);
            Graphics.Blit(depthTexture, visualDepthTexture, depthMaterial);

            // Copy visualDepthTexture to a Texture2D to access pixel data
            RenderTexture.active = visualDepthTexture;
            var visualTexture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            visualTexture2D.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
            visualTexture2D.Apply();

            var pixels = visualTexture2D.GetPixels();
            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    Color pixel = pixels[y * textureWidth + x];
                    depthData[y, x] = (pixel.r + pixel.g) * maxDepth;
                }
            }

            Destroy(visualTexture2D);
        }

        public RenderTexture GetDepthMap()
        {
            return visualDepthTexture;
        }

        public float[,] GetDepthData()
        {
            return depthData;
        }

        public float GetMinDepth()
        {
            return minDepth;
        }

        public float GetMaxDepth()
        {
            return maxDepth;
        }

        public Vector2Int GetResolution()
        {
            return new Vector2Int(textureWidth, textureHeight);
        }

        protected override void OnDestroy()
        {
            if (depthTexture != null)
            {
                depthTexture.Release();
                Destroy(depthTexture);
            }

            if (visualDepthTexture != null)
            {
                visualDepthTexture.Release();
                Destroy(visualDepthTexture);
            }

            if (depthMaterial != null)
            {
                Destroy(depthMaterial);
            }

            base.OnDestroy();
        }

        public void SetCameraFOV(string fov)
        {
            if (float.TryParse(fov, out float parsedFov))
            {
                depthCamera.fieldOfView = parsedFov;
            }
            else
            {
                Debug.LogError("Invalid FOV value: " + fov);
            }
        }

        public void SetMinDepth(string minDepth)
        {
            if (float.TryParse(minDepth, out float parsedMinDepth))
            {
                this.minDepth = parsedMinDepth;
            }
            else
            {
                Debug.LogError("Invalid Min Depth value: " + minDepth);
            }
        }

        public void SetMaxDepth(string maxDepth)
        {
            if (float.TryParse(maxDepth, out float parsedMaxDepth))
            {
                this.maxDepth = parsedMaxDepth;
            }
            else
            {
                Debug.LogError("Invalid Max Depth value: " + maxDepth);
            }
        }
    }
}
