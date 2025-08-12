using UnityEngine;
using SensorSimulator.Interfaces;
using SensorSimulator.Data;
using SensorSimulator.Sensors;

namespace SensorSimulator
{
    public class SensorManager : MonoBehaviour
    {
        [SerializeField]
        private RGBCameraSensor rgbCamera;
        [SerializeField]
        private ARKitLidarSensor arKitLidar;
        [SerializeField]
        private ExternalLidarSensor externalLidar;

        private System.DateTime startTime;

        private void Start()
        {
            startTime = System.DateTime.Now;
        }

        public SensorFrame CaptureFrame()
        {
            rgbCamera?.UpdateSensor();
            arKitLidar?.UpdateSensor();
            externalLidar?.UpdateSensor();

            return new SensorFrame
            {
                rgbImage = rgbCamera?.GetRGBImage(),
                depthMap = arKitLidar?.GetDepthMap(),
                arKitLidarData = arKitLidar?.GetDepthData(),
                externalLidarData = externalLidar?.GetPointCloud(),
                externalLidarFrame = externalLidar?.GetCurrentFrame(),
                cameraPose = rgbCamera?.GetCameraPose() ?? Matrix4x4.identity,
                lidarPose = externalLidar?.GetLidarPose() ?? Matrix4x4.identity,
                timestamp = (long)(System.DateTime.Now - startTime).TotalMilliseconds
            };
        }

        public System.Collections.IEnumerator CaptureFrameAsync(System.Action<SensorFrame> onComplete)
        {
            rgbCamera?.UpdateSensor();
            arKitLidar?.UpdateSensor();
            externalLidar?.UpdateSensor();

            if (externalLidar != null && externalLidar.IsScanning())
            {
                Debug.Log("LiDAR is scanning autonomously - not waiting for completion");
            }

            var frame = new SensorFrame
            {
                rgbImage = rgbCamera?.GetRGBImage(),
                depthMap = arKitLidar?.GetDepthMap(),
                arKitLidarData = arKitLidar?.GetDepthData(),
                externalLidarData = externalLidar?.GetPointCloud(),
                externalLidarFrame = externalLidar?.GetCurrentFrame(),
                cameraPose = rgbCamera?.GetCameraPose() ?? Matrix4x4.identity,
                lidarPose = externalLidar?.GetLidarPose() ?? Matrix4x4.identity,
                timestamp = (long)(System.DateTime.Now - startTime).TotalMilliseconds
            };

            onComplete?.Invoke(frame);
            yield return null;
        }

        public void SaveFrame(SensorFrame frame, string folderPath)
        {
            string timestamp = frame.timestamp.ToString();
            
            if (frame.rgbImage != null)
                SaveRenderTexture(frame.rgbImage, $"{folderPath}/rgb_{timestamp}.png");
            
            if (frame.depthMap != null)
                SaveRenderTexture(frame.depthMap, $"{folderPath}/depth_{timestamp}.png");
            
            if (frame.arKitLidarData != null)
                SaveDepthData(frame.arKitLidarData, $"{folderPath}/arkit_{timestamp}.txt");
            
            SaveMetadata(frame, $"{folderPath}/metadata_{timestamp}.json");
        }

        private void SaveRenderTexture(RenderTexture rt, string path)
        {
            var tmp = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tmp.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tmp.Apply();
            System.IO.File.WriteAllBytes(path, tmp.EncodeToPNG());
            Destroy(tmp);
        }

        private void SaveDepthData(float[,] depthData, string path)
        {
            using (var writer = new System.IO.StreamWriter(path))
            {
                writer.WriteLine($"# ARKit LiDAR Depth Data");
                writer.WriteLine($"# Resolution: {depthData.GetLength(0)}x{depthData.GetLength(1)}");
                writer.WriteLine($"# Format: X Y Depth(meters)");
                writer.WriteLine("# Data:");

                for (int y = 0; y < depthData.GetLength(0); y++)
                {
                    for (int x = 0; x < depthData.GetLength(1); x++)
                    {
                        writer.WriteLine($"{x} {y} {depthData[y, x]:F6}");
                    }
                }
            }
        }

        private void SaveMetadata(SensorFrame frame, string path)
        {
            float[] MatrixToArray(Matrix4x4 m)
            {
                return new float[]
                {
                    m.m00, m.m01, m.m02, m.m03,
                    m.m10, m.m11, m.m12, m.m13,
                    m.m20, m.m21, m.m22, m.m23,
                    m.m30, m.m31, m.m32, m.m33
                };
            }

            var metadata = new FrameMetadata
            {
                timestamp = frame.timestamp,
                cameraPose = MatrixToArray(frame.cameraPose),
                lidarPose = MatrixToArray(frame.lidarPose),
                rgbResolution = rgbCamera != null ? new int[] { rgbCamera.GetResolution().x, rgbCamera.GetResolution().y } : null,
                arkitResolution = arKitLidar != null ? new int[] { arKitLidar.GetResolution().x, arKitLidar.GetResolution().y } : null,
                lidarParams = externalLidar != null ? new LidarParams
                {
                    scanRadius = externalLidar.GetScanRadius(),
                    verticalFOV = externalLidar.GetVerticalFOV(),
                    horizontalFOV = externalLidar.GetHorizontalFOV(),
                    channelCount = externalLidar.GetChannelCount()
                } : null
            };

            string json = JsonUtility.ToJson(metadata, true);
            System.IO.File.WriteAllText(path, json);
        }

        [System.Serializable]
        private class FrameMetadata
        {
            public long timestamp;
            public float[] cameraPose;
            public float[] lidarPose;
            public int[] rgbResolution;
            public int[] arkitResolution;
            public LidarParams lidarParams;
        }

        [System.Serializable]
        private class LidarParams
        {
            public float scanRadius;
            public float verticalFOV;
            public float horizontalFOV;
            public int channelCount;
        }
    }
}
