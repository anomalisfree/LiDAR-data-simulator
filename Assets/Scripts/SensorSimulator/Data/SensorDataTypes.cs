using UnityEngine;
using System.Collections.Generic;

namespace SensorSimulator.Data
{
    public struct PointCloudPoint
    {
        public Vector3 position;
        public float intensity;
        public Color color;
        
        public PointCloudPoint(Vector3 pos, float intens = 1.0f, Color col = default)
        {
            position = pos;
            intensity = intens;
            color = col == default ? Color.white : col;
        }
    }

    [System.Serializable]
    public class LidarPacket
    {
        public int lineIndex;
        public bool leftToRight;
        public PointCloudPoint[] points;
        public float timestamp;
        public Vector3 sensorPosition;
        public Quaternion sensorRotation;
    }

    [System.Serializable]
    public class LidarFrame
    {
        public List<LidarPacket> packets;
        public float frameStartTime;
        public float frameEndTime;
        public int totalPoints;
    }

    public struct SensorFrame
    {
        public RenderTexture rgbImage;
        public RenderTexture depthMap;
        public float[,] arKitLidarData;
        public PointCloudPoint[] externalLidarData;
        public LidarFrame externalLidarFrame;
        public Matrix4x4 cameraPose;
        public Matrix4x4 lidarPose;
        public long timestamp;
    }
}
