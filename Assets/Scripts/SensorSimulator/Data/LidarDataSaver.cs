using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace SensorSimulator.Data
{
    public static class LidarDataSaver
    {
        public static void SaveFrameToPLY(LidarFrame frame, string filePath, bool includePacketInfo = true)
        {
            if (frame == null || frame.packets == null || frame.packets.Count == 0)
            {
                Debug.LogWarning("Cannot save empty frame to PLY");
                return;
            }

            int totalPoints = 0;
            foreach (var packet in frame.packets)
            {
                if (packet.points != null)
                {
                    totalPoints += packet.points.Length;
                }
            }

            if (totalPoints == 0)
            {
                Debug.LogWarning("No points to save in PLY file");
                return;
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.ASCII))
                {
                    writer.WriteLine("ply");
                    writer.WriteLine("format ascii 1.0");
                    writer.WriteLine($"element vertex {totalPoints}");
                    writer.WriteLine("property float x");
                    writer.WriteLine("property float y");
                    writer.WriteLine("property float z");
                    writer.WriteLine("property float intensity");
                    writer.WriteLine("property uchar red");
                    writer.WriteLine("property uchar green");
                    writer.WriteLine("property uchar blue");

                    if (includePacketInfo)
                    {
                        writer.WriteLine("comment LiDAR Frame Information:");
                        writer.WriteLine($"comment Frame Start Time: {frame.frameStartTime:F6}s");
                        writer.WriteLine($"comment Frame End Time: {frame.frameEndTime:F6}s");
                        writer.WriteLine($"comment Frame Duration: {frame.frameEndTime - frame.frameStartTime:F6}s");
                        writer.WriteLine($"comment Total Packets: {frame.packets.Count}");
                        writer.WriteLine($"comment Total Points: {totalPoints}");
                        writer.WriteLine("comment Packet Information:");
                        
                        for (int i = 0; i < frame.packets.Count; i++)
                        {
                            var packet = frame.packets[i];
                            writer.WriteLine($"comment Packet {i}: Line {packet.lineIndex}, " +
                                          $"Direction: {(packet.leftToRight ? "L→R" : "R→L")}, " +
                                          $"Points: {packet.points?.Length ?? 0}, " +
                                          $"Time: {packet.timestamp:F6}s, " +
                                          $"Position: ({packet.sensorPosition.x:F3}, {packet.sensorPosition.y:F3}, {packet.sensorPosition.z:F3})");
                        }
                    }

                    writer.WriteLine("end_header");

                    foreach (var packet in frame.packets)
                    {
                        if (packet.points != null)
                        {
                            foreach (var point in packet.points)
                            {
                                writer.WriteLine($"{point.position.x:F6} {point.position.y:F6} {point.position.z:F6} " +
                                               $"{point.intensity:F6} " +
                                               $"{(int)(point.color.r * 255)} {(int)(point.color.g * 255)} {(int)(point.color.b * 255)}");
                            }
                        }
                    }
                }

                Debug.Log($"LiDAR frame saved to PLY: {filePath} ({totalPoints} points, {frame.packets.Count} packets)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving LiDAR frame to PLY: {e.Message}");
            }
        }

        public static void SavePacketToPLY(LidarPacket packet, string filePath)
        {
            if (packet == null || packet.points == null || packet.points.Length == 0)
            {
                Debug.LogWarning("Cannot save empty packet to PLY");
                return;
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.ASCII))
                {
                    writer.WriteLine("ply");
                    writer.WriteLine("format ascii 1.0");
                    writer.WriteLine($"element vertex {packet.points.Length}");
                    writer.WriteLine("property float x");
                    writer.WriteLine("property float y");
                    writer.WriteLine("property float z");
                    writer.WriteLine("property float intensity");
                    writer.WriteLine("property uchar red");
                    writer.WriteLine("property uchar green");
                    writer.WriteLine("property uchar blue");

                    writer.WriteLine("comment LiDAR Packet Information:");
                    writer.WriteLine($"comment Line Index: {packet.lineIndex}");
                    writer.WriteLine($"comment Direction: {(packet.leftToRight ? "Left to Right" : "Right to Left")}");
                    writer.WriteLine($"comment Points Count: {packet.points.Length}");
                    writer.WriteLine($"comment Timestamp: {packet.timestamp:F6}s");
                    writer.WriteLine($"comment Sensor Position: ({packet.sensorPosition.x:F3}, {packet.sensorPosition.y:F3}, {packet.sensorPosition.z:F3})");
                    writer.WriteLine($"comment Sensor Rotation: ({packet.sensorRotation.eulerAngles.x:F3}, {packet.sensorRotation.eulerAngles.y:F3}, {packet.sensorRotation.eulerAngles.z:F3})");

                    writer.WriteLine("end_header");

                    foreach (var point in packet.points)
                    {
                        writer.WriteLine($"{point.position.x:F6} {point.position.y:F6} {point.position.z:F6} " +
                                       $"{point.intensity:F6} " +
                                       $"{(int)(point.color.r * 255)} {(int)(point.color.g * 255)} {(int)(point.color.b * 255)}");
                    }
                }

                Debug.Log($"LiDAR packet saved to PLY: {filePath} ({packet.points.Length} points)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving LiDAR packet to PLY: {e.Message}");
            }
        }

        public static void SaveFrameStats(LidarFrame frame, string filePath)
        {
            if (frame == null || frame.packets == null)
            {
                Debug.LogWarning("Cannot save stats for null frame");
                return;
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("LiDAR Frame Statistics");
                    writer.WriteLine("=====================");
                    writer.WriteLine();
                    writer.WriteLine($"Frame Start Time: {frame.frameStartTime:F6}s");
                    writer.WriteLine($"Frame End Time: {frame.frameEndTime:F6}s");
                    writer.WriteLine($"Frame Duration: {frame.frameEndTime - frame.frameStartTime:F6}s");
                    writer.WriteLine($"Total Packets: {frame.packets.Count}");
                    writer.WriteLine($"Total Points: {frame.totalPoints}");
                    writer.WriteLine();
                    writer.WriteLine("Packet Details:");
                    writer.WriteLine("===============");

                    for (int i = 0; i < frame.packets.Count; i++)
                    {
                        var packet = frame.packets[i];
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

                Debug.Log($"Frame statistics saved to: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving frame statistics: {e.Message}");
            }
        }
    }
}
