using System;
using System.Collections.Generic;
using AbilityKit.Core.Mathematics;

namespace AbilityKit.Demo.Moba.Console.Presentation
{
    /// <summary>
    /// 区域视图管理器接口 (Console 平台实现)
    /// 定义 AOE 区域的表现管理
    /// </summary>
    public interface IAreaViewManager
    {
        void ShowArea(int areaId, int templateId, in Vec3 position, float rotation);
        void HideArea(int areaId);
        void UpdateAreaPosition(int areaId, in Vec3 position);
        void UpdateAreaRotation(int areaId, float rotation);
        void ShowAreaWarning(int areaId);
        void HideAll();
        bool HasArea(int areaId);
    }

    public interface IAreaEventHandler
    {
        void HandleAreaEvents(in AreaEventData[] events);
    }

    public interface IAreaAppearance
    {
        AreaShapeType Shape { get; }
        float Radius { get; }
        float Width { get; }
        float Height { get; }
        float Depth { get; }
        Color4 Color { get; }
        float Alpha { get; }
        bool ShowBorder { get; }
    }

    public enum AreaShapeType
    {
        Circle = 0,
        Rectangle = 1,
        Sector = 2,
        Sphere = 3,
        Box = 4,
    }

    public readonly struct Color4
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public Color4(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static Color4 Red => new Color4(1f, 0.2f, 0.2f, 1f);
        public static Color4 Green => new Color4(0.2f, 1f, 0.2f, 1f);
        public static Color4 Blue => new Color4(0.2f, 0.2f, 1f, 1f);
        public static Color4 White => new Color4(1f, 1f, 1f, 1f);
        public static Color4 Yellow => new Color4(1f, 0.92f, 0.23f, 1f);
    }

    public struct AreaEventData
    {
        public int AreaId { get; }
        public AreaEventKind Kind { get; }
        public float CenterX { get; }
        public float CenterY { get; }
        public float CenterZ { get; }
        public float Radius { get; }

        public AreaEventData(int areaId, AreaEventKind kind, float x, float y, float z, float radius)
        {
            AreaId = areaId;
            Kind = kind;
            CenterX = x;
            CenterY = y;
            CenterZ = z;
            Radius = radius;
        }
    }

    public enum AreaEventKind
    {
        Appear = 0,
        Disappear = 1,
        Tick = 2,
    }
}
