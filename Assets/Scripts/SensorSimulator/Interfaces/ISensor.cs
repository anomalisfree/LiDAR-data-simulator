using UnityEngine;
using SensorSimulator.Data;

namespace SensorSimulator.Interfaces
{
    public interface ISensor
    {
        void Initialize();
        void UpdateSensor();
        bool IsInitialized { get; }
    }

    public interface IRGBCamera : ISensor
    {
        RenderTexture GetRGBImage();
        Matrix4x4 GetCameraPose();
        float GetFieldOfView();
    }

    public interface IDepthSensor : ISensor
    {
        RenderTexture GetDepthMap();
        float GetMinDepth();
        float GetMaxDepth();
    }

    public interface IARKitLidar : IDepthSensor
    {
        float[,] GetDepthData();
        Vector2Int GetResolution();
    }

    public interface IExternalLidar : ISensor
    {
        PointCloudPoint[] GetPointCloud();
        Matrix4x4 GetLidarPose();
        float GetScanRadius();
        float GetVerticalFOV();
        float GetHorizontalFOV();
        int GetChannelCount();
    }
}
