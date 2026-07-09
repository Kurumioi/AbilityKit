using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewPrimitiveFactory
    {
        public GameObject CreateActorFallback()
        {
            return CreateActorFallback(actorId: 0, modelId: 0);
        }

        public GameObject CreateActorFallback(int actorId, int modelId)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(1f, 2f, 1f);
            ApplyColor(go, ResolveActorColor(actorId, modelId));
            return go;
        }

        public GameObject CreateAoeModelFallback()
        {
            return CreateAoeModelFallback(modelId: 0);
        }

        public GameObject CreateAoeModelFallback(int modelId)
        {
            if (modelId == BattleViewPlaceholderIds.AoeSectorModel)
            {
                return CreateSectorFallback(modelId, degrees: 90f);
            }

            return CreateAoeRangeFallback(templateId: modelId, radius: 1f, delayMs: 0);
        }

        public GameObject CreateAoeRangeFallback(int templateId, float radius, int delayMs)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var r = Mathf.Max(0.5f, radius);
            go.transform.localScale = new Vector3(r * 2f, 0.04f, r * 2f);
            ApplyColor(go, ResolveAoeColor(templateId, delayMs));
            return go;
        }

        public GameObject CreateVfxFallback()
        {
            return CreateVfxFallback(vfxId: 0);
        }

        public GameObject CreateVfxFallback(int vfxId)
        {
            if (vfxId == BattleViewPlaceholderIds.LianPoSkill2CircleVfx)
            {
                return CreateLianPoSkill2CircleVfxFallback(vfxId);
            }

            if (vfxId == BattleViewPlaceholderIds.XiaoQiaoSkill2LiftVfx || vfxId == BattleViewPlaceholderIds.XiaoQiaoSkill3StarVfx)
            {
                return CreateXiaoQiaoAreaVfxFallback(vfxId);
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = Vector3.one * 0.5f;
            ApplyColor(go, ResolveVfxColor(vfxId));
            return go;
        }

        public GameObject CreateProjectileFallback(int vfxId)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.localScale = new Vector3(0.35f, 0.35f, 0.8f);
            ApplyColor(go, ResolveProjectileColor(vfxId));
            return go;
        }

        private static GameObject CreateSectorFallback(int modelId, float degrees)
        {
            var go = new GameObject("AoeSectorFallback", typeof(MeshFilter), typeof(MeshRenderer));
            var mesh = BuildSectorMesh(36, Mathf.Clamp(degrees, 1f, 180f));
            mesh.hideFlags = HideFlags.DontSave;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            ApplyColor(go, ResolveAoeColor(modelId, delayMs: 0));
            return go;
        }

        private static GameObject CreateLianPoSkill2CircleVfxFallback(int vfxId)
        {
            var go = new GameObject("LianPoSkill2CircleVfxFallback", typeof(MeshFilter), typeof(MeshRenderer));
            var mesh = BuildRingMesh(innerRadius: 3.28f, outerRadius: 3.5f, segments: 96, y: 0.08f);
            mesh.hideFlags = HideFlags.DontSave;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            ApplyColor(go, ResolveVfxColor(vfxId));
            return go;
        }

        private static GameObject CreateXiaoQiaoAreaVfxFallback(int vfxId)
        {
            var go = new GameObject("XiaoQiaoAreaVfxFallback", typeof(MeshFilter), typeof(MeshRenderer));
            var mesh = BuildRingMesh(innerRadius: vfxId == BattleViewPlaceholderIds.XiaoQiaoSkill3StarVfx ? 0.15f : 0.3f, outerRadius: vfxId == BattleViewPlaceholderIds.XiaoQiaoSkill3StarVfx ? 2.5f : 4f, segments: 72, y: 0.1f);
            mesh.hideFlags = HideFlags.DontSave;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            ApplyColor(go, ResolveVfxColor(vfxId));
            return go;
        }

        private static Mesh BuildSectorMesh(int segments, float degrees)
        {
            var safeSegments = Mathf.Max(3, segments);
            var vertices = new Vector3[safeSegments + 2];
            var triangles = new int[safeSegments * 3];
            vertices[0] = Vector3.zero;

            var start = -degrees * 0.5f;
            for (var i = 0; i <= safeSegments; i++)
            {
                var angle = (start + degrees * i / safeSegments) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            }

            for (var i = 0; i < safeSegments; i++)
            {
                var ti = i * 3;
                triangles[ti] = 0;
                triangles[ti + 1] = i + 1;
                triangles[ti + 2] = i + 2;
            }

            var mesh = new Mesh { name = "AoeSectorFallbackMesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh BuildRingMesh(float innerRadius, float outerRadius, int segments, float y)
        {
            var safeSegments = Mathf.Max(8, segments);
            var inner = Mathf.Max(0.01f, Mathf.Min(innerRadius, outerRadius));
            var outer = Mathf.Max(inner + 0.01f, outerRadius);
            var vertices = new Vector3[(safeSegments + 1) * 2];
            var triangles = new int[safeSegments * 6];

            for (var i = 0; i <= safeSegments; i++)
            {
                var angle = Mathf.PI * 2f * i / safeSegments;
                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);
                var vi = i * 2;
                vertices[vi] = new Vector3(sin * inner, y, cos * inner);
                vertices[vi + 1] = new Vector3(sin * outer, y, cos * outer);
            }

            for (var i = 0; i < safeSegments; i++)
            {
                var vi = i * 2;
                var ti = i * 6;
                triangles[ti] = vi;
                triangles[ti + 1] = vi + 1;
                triangles[ti + 2] = vi + 2;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + 3;
                triangles[ti + 5] = vi + 2;
            }

            var mesh = new Mesh { name = "LianPoSkill2CircleVfxFallbackMesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            if (go == null) return;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var material = shader != null ? new Material(shader) : new Material(renderer.sharedMaterial);
            material.color = color;
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            ConfigureTransparency(material, color.a);
            renderer.material = material;
        }

        private static void ConfigureTransparency(Material material, float alpha)
        {
            if (material == null || alpha >= 0.99f) return;

            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static Color ResolveActorColor(int actorId, int modelId)
        {
            if (modelId == 1001) return new Color(0.85f, 0.28f, 0.12f, 1f);
            if (modelId == 1002) return new Color(0.25f, 0.45f, 0.95f, 1f);
            return PickColor(actorId + modelId, 1f);
        }

        private static Color ResolveAoeColor(int templateId, int delayMs)
        {
            var color = PickColor(templateId, 0.35f);
            if (delayMs > 0) color = Color.Lerp(color, new Color(1f, 0.95f, 0.25f, 0.35f), 0.45f);
            color.a = 0.35f;
            return color;
        }

        private static Color ResolveVfxColor(int vfxId)
        {
            if (vfxId == BattleViewPlaceholderIds.LianPoSkill2CircleVfx)
            {
                return new Color(1f, 0.42f, 0.12f, 0.72f);
            }

            if (vfxId == BattleViewPlaceholderIds.XiaoQiaoSkill1FanVfx)
            {
                return new Color(1f, 0.62f, 0.95f, 0.88f);
            }

            if (vfxId == BattleViewPlaceholderIds.XiaoQiaoSkill2LiftVfx)
            {
                return new Color(0.95f, 0.42f, 1f, 0.55f);
            }

            if (vfxId == BattleViewPlaceholderIds.XiaoQiaoSkill3StarVfx)
            {
                return new Color(1f, 0.9f, 0.28f, 0.62f);
            }

            return PickColor(vfxId, 0.8f);
        }

        private static Color ResolveProjectileColor(int vfxId)
        {
            if (vfxId == BattleViewPlaceholderIds.XiaoQiaoSkill1FanVfx)
            {
                return new Color(1f, 0.62f, 0.95f, 1f);
            }

            return Color.Lerp(PickColor(vfxId, 1f), new Color(0.15f, 0.95f, 1f, 1f), 0.5f);
        }

        private static Color PickColor(int seed, float alpha)
        {
            var value = Mathf.Abs(seed);
            switch (value % 6)
            {
                case 0: return new Color(0.95f, 0.25f, 0.2f, alpha);
                case 1: return new Color(0.2f, 0.7f, 1f, alpha);
                case 2: return new Color(0.25f, 0.9f, 0.35f, alpha);
                case 3: return new Color(1f, 0.8f, 0.15f, alpha);
                case 4: return new Color(0.8f, 0.35f, 1f, alpha);
                default: return new Color(1f, 0.45f, 0.15f, alpha);
            }
        }
    }

    internal static class BattleViewPlaceholderIds
    {
        public const int ProjectileVfx = 90000001;
        public const int ProjectileSpawnVfx = 90000002;
        public const int ProjectileHitVfx = 90000003;
        public const int ProjectileExpireVfx = 90000004;
        public const int PresentationCueVfx = 90000005;
        public const int AoeCircleModel = 90000101;
        public const int AoeSectorModel = 90000102;
        public const int LianPoSkill2CircleVfx = 90001001;
        public const int XiaoQiaoSkill1FanVfx = 90002001;
        public const int XiaoQiaoSkill2LiftVfx = 90002002;
        public const int XiaoQiaoSkill3StarVfx = 90002003;

        public static bool IsPlaceholderVfx(int vfxId)
        {
            return (vfxId >= ProjectileVfx && vfxId <= PresentationCueVfx)
                || vfxId == LianPoSkill2CircleVfx
                || (vfxId >= XiaoQiaoSkill1FanVfx && vfxId <= XiaoQiaoSkill3StarVfx);
        }
    }
}
