using UnityEngine;
using SensorSimulator.Interfaces;
using SensorSimulator.Data;
using System.Collections;
using System.Collections.Generic;
using System;

namespace SensorSimulator.Sensors
{
    public class ExternalLidarSensor : BaseSensor, IExternalLidar
    {
        [Header("LiDAR Configuration")]
        [SerializeField, Tooltip("Scan radius (meters)")]
        private float scanRadius = 100f;
        [SerializeField, Tooltip("Vertical field of view (degrees)")]
        private float verticalFOV = 30f;
        [SerializeField, Tooltip("Horizontal field of view (degrees)")]
        private float horizontalFOV = 360f;
        [SerializeField, Tooltip("Number of scan lines")]
        private int lineCount = 64;
        [SerializeField, Tooltip("Number of points per line")]
        private int pointsPerLine = 1024;

        [Header("Noise & Distortion")]
        [SerializeField, Tooltip("Gaussian noise standard deviation (meters)")]
        private float noiseStdDev = 0.01f;
        [SerializeField, Tooltip("Fisheye effect strength (0 = none, 1 = strong)")]
        private float fisheyeStrength = 0.0f;

        [Header("Scanning Delays")]
        [SerializeField, Tooltip("Delay between points in line (ms)")]
        private float pointDelayMs = 0.1f;
        [SerializeField, Tooltip("Delay between packets (lines) (ms)")]
        private float packetDelayMs = 10.0f;
        [SerializeField, Tooltip("Delay after frame completion (ms)")]
        private float frameEndDelayMs = 100.0f;
        [SerializeField, Tooltip("Enable scanning delays")]
        private bool enableDelays = true;

        [Header("Debug")]
        [SerializeField, Tooltip("Show scanning information")]
        private bool showDebugInfo = true;

        [Header("Auto Save Settings")]
        [SerializeField, Tooltip("Automatically save PLY files on frame completion")]
        private bool autoSavePLY = true;
        [SerializeField, Tooltip("Automatically save packet information on frame completion")]
        private bool autoSavePacketInfo = true;
        [SerializeField, Tooltip("Folder for saving PLY files (relative to SensorData)")]
        private string plySaveFolder = "LidarPLY";

        private LayerMask layerMask;
        private Coroutine scanningCoroutine;
        private bool isScanning = false;
        private LidarFrame currentFrame;
        private PointCloudPoint[] lastPointCloud;
        private System.Random random;

        public event System.Action<LidarPacket> OnPacketReceived;
        public event System.Action<LidarFrame> OnFrameCompleted;

        public override void Initialize()
        {
            layerMask = ~0;
            random = new System.Random();
            lastPointCloud = new PointCloudPoint[0];
            base.Initialize();
        }

        public override void UpdateSensor()
        {
            if (!isInitialized || isScanning) return;

            if (scanningCoroutine != null)
            {
                StopCoroutine(scanningCoroutine);
            }

            scanningCoroutine = StartCoroutine(ScanFrameCoroutine());
        }

        private IEnumerator ScanFrameCoroutine()
        {
            try
            {
                isScanning = true;
                currentFrame = new LidarFrame
                {
                    packets = new List<LidarPacket>(),
                    frameStartTime = Time.time,
                    totalPoints = 0
                };

                if (showDebugInfo)
                {
                    Debug.Log($"Starting LiDAR frame scan: {lineCount} lines, {pointsPerLine} points per line");
                }

                for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    bool leftToRight = lineIndex % 2 == 0;
                    LidarPacket packet = ScanLine(lineIndex, leftToRight);

                    currentFrame.packets.Add(packet);
                    currentFrame.totalPoints += packet.points.Length;
                    OnPacketReceived?.Invoke(packet);

                    if (showDebugInfo)
                    {
                        Debug.Log($"Packet {lineIndex + 1}/{lineCount}: {packet.points.Length} points, " +
                                $"direction: {(leftToRight ? "L→R" : "R→L")}, " +
                                $"time: {packet.timestamp:F3}s");
                    }

                    if (enableDelays && packetDelayMs > 0)
                    {
                        yield return new WaitForSeconds(packetDelayMs / 1000f);
                    }
                }

                if (enableDelays && frameEndDelayMs > 0)
                {
                    yield return new WaitForSeconds(frameEndDelayMs / 1000f);
                }

                currentFrame.frameEndTime = Time.time;
                float frameDuration = currentFrame.frameEndTime - currentFrame.frameStartTime;

                if (showDebugInfo)
                {
                    Debug.Log($"Frame completed: {currentFrame.totalPoints} points, " +
                            $"{currentFrame.packets.Count} packets, " +
                            $"duration: {frameDuration:F3}s");
                }

                lastPointCloud = CombinePacketsToPointCloud(currentFrame.packets);
                OnFrameCompleted?.Invoke(currentFrame);

                if (autoSavePLY)
                {
                    SaveFrameToPLY();
                }
                
                if (autoSavePacketInfo)
                {
                    SavePacketInfo();
                }
            }
            finally
            {
                isScanning = false;
                scanningCoroutine = null;
            }
        }

        private LidarPacket ScanLine(int lineIndex, bool leftToRight)
        {
            Vector3 sensorPosition = transform.position;
            Quaternion sensorRotation = transform.rotation;

            float verticalStep = verticalFOV / Mathf.Max(1, lineCount - 1);
            float verticalAngle = -verticalFOV / 2 + lineIndex * verticalStep;
            float horizontalStep = horizontalFOV / Mathf.Max(1, pointsPerLine - 1);

            int startIndex = leftToRight ? 0 : pointsPerLine - 1;
            int endIndex = leftToRight ? pointsPerLine : -1;
            int step = leftToRight ? 1 : -1;

            List<PointCloudPoint> linePoints = new List<PointCloudPoint>();

            for (int i = startIndex; leftToRight ? i < endIndex : i > endIndex; i += step)
            {
                float horizontalAngle = -horizontalFOV / 2 + i * horizontalStep;

                float normH = (i / (float)(pointsPerLine - 1)) * 2f - 1f;
                float normV = (lineIndex / (float)(lineCount - 1)) * 2f - 1f;
                float fisheye = 1f + fisheyeStrength * (normH * normH + normV * normV);
                float vAngle = verticalAngle * fisheye;
                float hAngle = horizontalAngle * fisheye;

                Quaternion rotation = sensorRotation * Quaternion.Euler(vAngle, hAngle, 0);
                Vector3 direction = rotation * Vector3.forward;
                Ray ray = new Ray(sensorPosition, direction);

                if (Physics.Raycast(ray, out RaycastHit hit, scanRadius, layerMask))
                {
                    float noise = (float)SampleGaussian(random, 0, noiseStdDev);
                    Vector3 noisyPoint = hit.point + hit.normal * noise;
                    Color pointColor = GetPointColor(hit);
                    float intensity = Mathf.Max(0, Vector3.Dot(-ray.direction, hit.normal));

                    linePoints.Add(new PointCloudPoint(noisyPoint, intensity, pointColor));
                }
            }

            return new LidarPacket
            {
                lineIndex = lineIndex,
                leftToRight = leftToRight,
                points = linePoints.ToArray(),
                timestamp = Time.time,
                sensorPosition = sensorPosition,
                sensorRotation = sensorRotation
            };
        }

        private Color GetPointColor(RaycastHit hit)
        {
            if (hit.collider.TryGetComponent<Renderer>(out var renderer))
            {
                if (renderer.material.mainTexture != null && renderer.material.mainTexture is Texture2D texture2D)
                {
                    return texture2D.GetPixelBilinear(hit.textureCoord.x, hit.textureCoord.y);
                }
                else
                {
                    return renderer.material.color;
                }
            }
            return Color.white;
        }

        private PointCloudPoint[] CombinePacketsToPointCloud(List<LidarPacket> packets)
        {
            List<PointCloudPoint> allPoints = new List<PointCloudPoint>();
            
            foreach (var packet in packets)
            {
                allPoints.AddRange(packet.points);
            }

            return allPoints.ToArray();
        }

        private double SampleGaussian(System.Random rand, double mean, double stddev)
        {
            double u1 = 1.0 - rand.NextDouble();
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stddev * randStdNormal;
        }

        public PointCloudPoint[] GetPointCloud()
        {
            return lastPointCloud ?? new PointCloudPoint[0];
        }

        public LidarFrame GetCurrentFrame()
        {
            return currentFrame;
        }

        public bool IsScanning()
        {
            return isScanning;
        }

        public bool IsDataReady()
        {
            return !isScanning && currentFrame != null && currentFrame.packets != null && currentFrame.packets.Count > 0;
        }

        public Matrix4x4 GetLidarPose()
        {
            return transform.localToWorldMatrix;
        }

        public float GetScanRadius()
        {
            return scanRadius;
        }

        public float GetVerticalFOV()
        {
            return verticalFOV;
        }

        public float GetHorizontalFOV()
        {
            return horizontalFOV;
        }

        public int GetChannelCount()
        {
            return lineCount;
        }

        public void SetScanRadius(string radius)
        {
            if (float.TryParse(radius, out float parsedRadius) && parsedRadius > 0)
            {
                scanRadius = parsedRadius;
            }
            else
            {
                Debug.LogError("Invalid scan radius: " + radius);
            }
        }

        public void SetVerticalFOV(string fov)
        {
            if (float.TryParse(fov, out float parsedFov))
            {
                verticalFOV = parsedFov;
            }
            else
            {
                Debug.LogError("Invalid FOV value: " + fov);
            }
        }

        public void SetHorizontalFOV(string fov)
        {
            if (float.TryParse(fov, out float parsedFov))
            {
                horizontalFOV = parsedFov;
            }
            else
            {
                Debug.LogError("Invalid FOV value: " + fov);
            }
        }

        public void SetChannelCount(string count)
        {
            if (int.TryParse(count, out int parsedCount) && parsedCount > 0)
            {
                lineCount = parsedCount;
            }
            else
            {
                Debug.LogError("Invalid channel count: " + count);
            }
        }

        public void SetPointsPerChannel(string count)
        {
            if (int.TryParse(count, out int parsedCount) && parsedCount > 0)
            {
                pointsPerLine = parsedCount;
            }
            else
            {
                Debug.LogError("Invalid points per channel: " + count);
            }
        }

        public void SetNoiseStdDev(string stdDev)
        {
            if (float.TryParse(stdDev, out float parsedStdDev) && parsedStdDev >= 0)
            {
                noiseStdDev = parsedStdDev;
            }
            else
            {
                Debug.LogError("Invalid noise standard deviation: " + stdDev);
            }
        }

        public void SetFisheyeStrength(string strength)
        {
            if (float.TryParse(strength, out float parsedStrength) && parsedStrength >= 0)
            {
                fisheyeStrength = parsedStrength;
            }
            else
            {
                Debug.LogError("Invalid fisheye strength: " + strength);
            }
        }

        public void SetPointDelayMs(string delay)
        {
            if (float.TryParse(delay, out float parsedDelay) && parsedDelay >= 0)
            {
                pointDelayMs = parsedDelay;
            }
            else
            {
                Debug.LogError("Invalid point delay: " + delay);
            }
        }

        public void SetLineDelayMs(string delay)
        {
            if (float.TryParse(delay, out float parsedDelay) && parsedDelay >= 0)
            {
                packetDelayMs = parsedDelay;
            }
            else
            {
                Debug.LogError("Invalid line delay: " + delay);
            }
        }

        public void SetFrameDelayMs(string delay)
        {
            if (float.TryParse(delay, out float parsedDelay) && parsedDelay >= 0)
            {
                frameEndDelayMs = parsedDelay;
            }
            else
            {
                Debug.LogError("Invalid frame delay: " + delay);
            }
        }

        protected override void OnDestroy()
        {
            if (scanningCoroutine != null)
            {
                StopCoroutine(scanningCoroutine);
            }
            base.OnDestroy();
        }

        private void SaveFrameToPLY()
        {
            if (currentFrame == null || currentFrame.packets == null || currentFrame.packets.Count == 0)
            {
                Debug.LogWarning("Cannot save empty LiDAR frame to PLY");
                return;
            }

            try
            {
                string basePath = System.IO.Path.Combine(Application.dataPath, "..", "SensorData", plySaveFolder);
                System.IO.Directory.CreateDirectory(basePath);

                string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fff");
                string fileName = $"lidar_frame_{timestamp}.ply";
                string filePath = System.IO.Path.Combine(basePath, fileName);

                using (var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.ASCII))
                {
                    writer.WriteLine("ply");
                    writer.WriteLine("format ascii 1.0");
                    writer.WriteLine($"element vertex {currentFrame.totalPoints}");
                    writer.WriteLine("property float x");
                    writer.WriteLine("property float y");
                    writer.WriteLine("property float z");
                    writer.WriteLine("property float intensity");
                    writer.WriteLine("property uchar red");
                    writer.WriteLine("property uchar green");
                    writer.WriteLine("property uchar blue");

                    writer.WriteLine("comment LiDAR Frame Information:");
                    writer.WriteLine($"comment Frame Start Time: {currentFrame.frameStartTime:F6}s");
                    writer.WriteLine($"comment Frame End Time: {currentFrame.frameEndTime:F6}s");
                    writer.WriteLine($"comment Frame Duration: {currentFrame.frameEndTime - currentFrame.frameStartTime:F6}s");
                    writer.WriteLine($"comment Total Packets: {currentFrame.packets.Count}");
                    writer.WriteLine($"comment Total Points: {currentFrame.totalPoints}");
                    writer.WriteLine("comment Packet Information:");
                    
                    for (int i = 0; i < currentFrame.packets.Count; i++)
                    {
                        var packet = currentFrame.packets[i];
                        writer.WriteLine($"comment Packet {i}: Line {packet.lineIndex}, " +
                                      $"Direction: {(packet.leftToRight ? "L→R" : "R→L")}, " +
                                      $"Points: {packet.points?.Length ?? 0}, " +
                                      $"Time: {packet.timestamp:F6}s, " +
                                      $"Position: ({packet.sensorPosition.x:F3}, {packet.sensorPosition.y:F3}, {packet.sensorPosition.z:F3})");
                    }

                    writer.WriteLine("end_header");

                    foreach (var packet in currentFrame.packets)
                    {
                        if (packet.points != null)
                        {
                            foreach (var point in packet.points)
                            {
                                int r = Mathf.Clamp(Mathf.RoundToInt(point.color.r * 255f), 0, 255);
                                int g = Mathf.Clamp(Mathf.RoundToInt(point.color.g * 255f), 0, 255);
                                int b = Mathf.Clamp(Mathf.RoundToInt(point.color.b * 255f), 0, 255);
                                writer.WriteLine(
                                    string.Format(
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        "{0:F6} {1:F6} {2:F6} {3:F6} {4} {5} {6}",
                                        point.position.x, point.position.y, point.position.z, point.intensity, r, g, b
                                    )
                                );
                            }
                        }
                    }
                }

                Debug.Log($"LiDAR frame automatically saved to PLY: {filePath} ({currentFrame.totalPoints} points, {currentFrame.packets.Count} packets)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving LiDAR frame to PLY: {e.Message}");
            }
        }

        private void SavePacketInfo()
        {
            if (currentFrame == null || currentFrame.packets == null || currentFrame.packets.Count == 0)
            {
                Debug.LogWarning("Cannot save empty LiDAR frame packet info");
                return;
            }

            try
            {
                string basePath = System.IO.Path.Combine(Application.dataPath, "..", "SensorData", plySaveFolder);
                System.IO.Directory.CreateDirectory(basePath);

                string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fff");
                string fileName = $"packet_info_{timestamp}.txt";
                string filePath = System.IO.Path.Combine(basePath, fileName);

                using (var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("LiDAR Packet Information");
                    writer.WriteLine("=======================");
                    writer.WriteLine();
                    writer.WriteLine($"Frame Start Time: {currentFrame.frameStartTime:F6}s");
                    writer.WriteLine($"Frame End Time: {currentFrame.frameEndTime:F6}s");
                    writer.WriteLine($"Frame Duration: {currentFrame.frameEndTime - currentFrame.frameStartTime:F6}s");
                    writer.WriteLine($"Total Packets: {currentFrame.packets.Count}");
                    writer.WriteLine($"Total Points: {currentFrame.totalPoints}");
                    writer.WriteLine();
                    writer.WriteLine("Packet Details:");
                    writer.WriteLine("===============");

                    for (int i = 0; i < currentFrame.packets.Count; i++)
                    {
                        var packet = currentFrame.packets[i];
                        writer.WriteLine($"Packet {i + 1}:");
                        writer.WriteLine($"  Line Index: {packet.lineIndex}");
                        writer.WriteLine($"  Direction: {(packet.leftToRight ? "Left to Right" : "Right to Left")}");
                        writer.WriteLine($"  Points: {packet.points?.Length ?? 0}");
                        writer.WriteLine($"  Timestamp: {packet.timestamp:F6}s");
                        writer.WriteLine($"  Sensor Position: ({packet.sensorPosition.x:F3}, {packet.sensorPosition.y:F3}, {packet.sensorPosition.z:F3})");
                        writer.WriteLine($"  Sensor Rotation: ({packet.sensorRotation.eulerAngles.x:F3}, {packet.sensorRotation.eulerAngles.y:F3}, {packet.sensorRotation.eulerAngles.z:F3})");
                        writer.WriteLine();
                    }
                }

                Debug.Log($"LiDAR packet information automatically saved: {filePath} ({currentFrame.packets.Count} packets)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving LiDAR packet information: {e.Message}");
            }
        }
    }
}
