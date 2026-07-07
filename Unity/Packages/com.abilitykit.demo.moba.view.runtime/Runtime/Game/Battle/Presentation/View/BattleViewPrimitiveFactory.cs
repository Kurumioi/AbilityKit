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

        private static void ApplyColor(GameObject go, Color color)
        {
            if (go == null) return;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var material = shader != null ? new Material(shader) : new Material(renderer.sharedMaterial);
            material.color = color;
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
            return PickColor(vfxId, 0.8f);
        }

        private static Color ResolveProjectileColor(int vfxId)
        {
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

        public static bool IsPlaceholderVfx(int vfxId)
        {
            return vfxId >= ProjectileVfx && vfxId <= PresentationCueVfx;
        }
    }
}
