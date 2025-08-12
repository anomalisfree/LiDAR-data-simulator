using UnityEngine;
using SensorSimulator.Interfaces;

namespace SensorSimulator.Sensors
{
    [RequireComponent(typeof(Camera))]
    public class RGBCameraSensor : BaseSensor, IRGBCamera
    {
        private Camera sensorCamera;
        private RenderTexture rgbTexture;

        public override void Initialize()
        {
            sensorCamera = GetComponent<Camera>();
            rgbTexture = CreateRenderTexture(RenderTextureFormat.ARGB32);
            sensorCamera.targetTexture = rgbTexture;
            base.Initialize();
        }

        public override void UpdateSensor()
        {
            if (!isInitialized) return;
            // Камера обновляется автоматически Unity
        }

        public RenderTexture GetRGBImage()
        {
            return rgbTexture;
        }

        public Matrix4x4 GetCameraPose()
        {
            return transform.localToWorldMatrix;
        }

        public float GetFieldOfView()
        {
            return sensorCamera.fieldOfView;
        }

        public Vector2Int GetResolution()
        {
            return new Vector2Int(textureWidth, textureHeight);
        }

        protected override void OnDestroy()
        {
            if (rgbTexture != null)
            {
                rgbTexture.Release();
                Destroy(rgbTexture);
            }
            base.OnDestroy();
        }
        
        public void SetCameraFOV(string fov)
        {
            if (float.TryParse(fov, out float parsedFov))
            {
                sensorCamera.fieldOfView = parsedFov;
            }
            else
            {
                Debug.LogError("Invalid FOV value: " + fov);
            }
        }
    }
}
