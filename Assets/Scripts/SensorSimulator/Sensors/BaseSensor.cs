using UnityEngine;
using SensorSimulator.Interfaces;

namespace SensorSimulator.Sensors
{
    public abstract class BaseSensor : MonoBehaviour, ISensor
    {
        protected bool isInitialized = false;

        [SerializeField]
        protected int textureWidth = 1024;
        [SerializeField]
        protected int textureHeight = 1024;

        public bool IsInitialized => isInitialized;

        protected virtual void Start()
        {
            Initialize();
        }

        public virtual void Initialize()
        {
            isInitialized = true;
        }

        public abstract void UpdateSensor();

        protected virtual void OnDestroy()
        {
            isInitialized = false;
        }

        protected RenderTexture CreateRenderTexture(RenderTextureFormat format)
        {
            var texture = new RenderTexture(textureWidth, textureHeight, 24, format);
            texture.Create();
            return texture;
        }

        public void SetTextureWidth(string width)
        {
            textureWidth = int.Parse(width);
        }

        public void SetTextureHeight(string height)
        {
            textureHeight = int.Parse(height);
        }

        public void SetPosX(string x)
        {
            transform.localPosition = new Vector3(float.Parse(x), transform.localPosition.y, transform.localPosition.z);
        }

        public void SetPosY(string y)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, float.Parse(y), transform.localPosition.z);
        }

        public void SetPosZ(string z)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, float.Parse(z));
        }

        public void SetRotationX(string x)
        {
            transform.localRotation = Quaternion.Euler(float.Parse(x), transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z);
        }

        public void SetRotationY(string y)
        {
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, float.Parse(y), transform.localRotation.eulerAngles.z);
        }
        
        public void SetRotationZ(string z)
        {
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, float.Parse(z));
        }
    }
}
