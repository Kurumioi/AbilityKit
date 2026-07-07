using System.Collections.Generic;
using AbilityKit.Game.Battle.Entity;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudAimPreview
    {
        private readonly BattleHudAimPreviewPositionResolver _positions;
        private readonly BattleHudAimPreviewObjectFactory _objects;
        private IReadOnlyDictionary<int, BattleHudSkillPresentationSpec> _skillSpecs;
        private BattleHudAimPreviewObject _preview;

        public BattleHudAimPreview(
            BattleHudAimPreviewPositionResolver positions = null,
            BattleHudAimPreviewObjectFactory objects = null)
        {
            _positions = positions ?? new BattleHudAimPreviewPositionResolver();
            _objects = objects ?? new BattleHudAimPreviewObjectFactory();
        }

        public void SetSkillSpecs(IReadOnlyDictionary<int, BattleHudSkillPresentationSpec> skillSpecs)
        {
            _skillSpecs = skillSpecs;
        }

        public void Tick(BattleContext ctx)
        {
            if (!_positions.TryResolve(ctx, out var state) || !TryGetSpec(state.Slot, out var spec))
            {
                Hide();
                return;
            }

            EnsurePreview();
            _preview.Apply(state, spec);
        }

        public void Clear()
        {
            if (_preview != null)
            {
                Object.Destroy(_preview.Root);
            }

            _preview = null;
        }

        private bool TryGetSpec(int slot, out BattleHudSkillPresentationSpec spec)
        {
            spec = default;
            return _skillSpecs != null
                && _skillSpecs.TryGetValue(slot, out spec)
                && spec.PreviewShape != BattleHudSkillPreviewShape.None;
        }

        private void Hide()
        {
            if (_preview != null)
            {
                _preview.SetVisible(false);
            }
        }

        private void EnsurePreview()
        {
            if (_preview != null) return;

            _preview = _objects.Create();
        }
    }

    internal readonly struct BattleHudAimPreviewState
    {
        public readonly int Slot;
        public readonly Vector3 CasterPosition;
        public readonly Vector3 AimDirection;
        public readonly float AimDistance;

        public BattleHudAimPreviewState(int slot, Vector3 casterPosition, Vector3 aimDirection, float aimDistance)
        {
            Slot = slot;
            CasterPosition = casterPosition;
            AimDirection = aimDirection;
            AimDistance = aimDistance;
        }
    }

    internal sealed class BattleHudAimPreviewPositionResolver
    {
        public bool TryResolve(BattleContext ctx, out BattleHudAimPreviewState state)
        {
            state = default;
            if (ctx == null || ctx.EntityQuery == null) return false;

            if (!ctx.TryReadHudSkillAim(out var slot, out var aimDx, out var aimDz))
            {
                return false;
            }

            var casterId = ctx.LocalActorId;
            if (casterId <= 0) return false;

            if (!ctx.EntityQuery.TryResolve(new BattleNetId(casterId), out var caster))
            {
                return false;
            }

            if (!caster.TryGetRef(out AbilityKit.Game.Battle.Component.BattleTransformComponent transform) || transform == null)
            {
                return false;
            }

            var aim = new Vector3(aimDx, 0f, aimDz);
            var distance = aim.magnitude;
            var direction = distance > 0.001f ? aim / distance : Vector3.forward;
            state = new BattleHudAimPreviewState(slot, transform.Position, direction, distance);
            return true;
        }
    }

    internal sealed class BattleHudAimPreviewObjectFactory
    {
        public BattleHudAimPreviewObject Create()
        {
            var root = new GameObject("SkillAimPreview");
            root.hideFlags = HideFlags.DontSave;

            var line = CreatePrimitive(root.transform, "Line", PrimitiveType.Cube);
            var circle = CreatePrimitive(root.transform, "Circle", PrimitiveType.Cylinder);
            var dot = CreatePrimitive(root.transform, "Dot", PrimitiveType.Sphere);
            var sector = CreateSector(root.transform, "Sector", segments: 36, degrees: 90f);

            var preview = new BattleHudAimPreviewObject(root, line, circle, dot, sector);
            preview.SetVisible(false);
            return preview;
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent, false);

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateMaterial(new Color(0.2f, 0.75f, 1f, 0.28f));
            }

            return go;
        }

        private static GameObject CreateSector(Transform parent, string name, int segments, float degrees)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent, false);

            var mesh = BuildSectorMesh(Mathf.Max(3, segments), Mathf.Clamp(degrees, 1f, 180f));
            mesh.hideFlags = HideFlags.DontSave;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().material = CreateMaterial(new Color(0.2f, 0.75f, 1f, 0.28f));
            return go;
        }

        private static Mesh BuildSectorMesh(int segments, float degrees)
        {
            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;

            var start = -degrees * 0.5f;
            for (var i = 0; i <= segments; i++)
            {
                var angle = (start + degrees * i / segments) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            }

            for (var i = 0; i < segments; i++)
            {
                var ti = i * 3;
                triangles[ti] = 0;
                triangles[ti + 1] = i + 1;
                triangles[ti + 2] = i + 2;
            }

            var mesh = new Mesh { name = "SkillAimPreviewSector" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = color;
            material.hideFlags = HideFlags.DontSave;
            return material;
        }
    }

    internal sealed class BattleHudAimPreviewObject
    {
        private const float HeightOffset = 0.04f;

        private readonly GameObject _line;
        private readonly GameObject _circle;
        private readonly GameObject _dot;
        private readonly GameObject _sector;

        public GameObject Root { get; }

        public BattleHudAimPreviewObject(GameObject root, GameObject line, GameObject circle, GameObject dot, GameObject sector)
        {
            Root = root;
            _line = line;
            _circle = circle;
            _dot = dot;
            _sector = sector;
        }

        public void Apply(in BattleHudAimPreviewState state, in BattleHudSkillPresentationSpec spec)
        {
            SetVisible(true);
            SetColor(spec.Color);

            var direction = state.AimDirection.sqrMagnitude > 0.001f ? state.AimDirection.normalized : Vector3.forward;
            var range = Mathf.Max(0.1f, spec.Range);
            var distance = Mathf.Clamp(state.AimDistance, 0f, range);
            var target = state.CasterPosition + direction * distance;

            switch (spec.PreviewShape)
            {
                case BattleHudSkillPreviewShape.DirectionLine:
                    ShowLine(state.CasterPosition, direction, range, spec.Width);
                    ShowDot(state.CasterPosition + direction * range, Mathf.Max(0.25f, spec.Width * 0.35f));
                    HideCircle();
                    HideSector();
                    break;
                case BattleHudSkillPreviewShape.TargetCircle:
                    HideLine();
                    ShowCircle(target, Mathf.Max(0.25f, spec.Radius));
                    ShowDot(target, Mathf.Max(0.25f, spec.Radius * 0.18f));
                    HideSector();
                    break;
                case BattleHudSkillPreviewShape.SelfCircle:
                    HideLine();
                    ShowCircle(state.CasterPosition, Mathf.Max(0.25f, spec.Radius));
                    HideDot();
                    HideSector();
                    break;
                case BattleHudSkillPreviewShape.Sector:
                    HideLine();
                    HideCircle();
                    HideDot();
                    ShowSector(state.CasterPosition, direction, range);
                    break;
                default:
                    SetVisible(false);
                    break;
            }
        }

        public void SetVisible(bool visible)
        {
            if (Root != null)
            {
                Root.SetActive(visible);
            }
        }

        private void ShowLine(Vector3 start, Vector3 direction, float length, float width)
        {
            if (_line == null) return;

            _line.SetActive(true);
            var safeLength = Mathf.Max(0.1f, length);
            var safeWidth = Mathf.Max(0.08f, width);
            _line.transform.position = start + direction * (safeLength * 0.5f) + Vector3.up * HeightOffset;
            _line.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            _line.transform.localScale = new Vector3(safeWidth, 0.035f, safeLength);
        }

        private void ShowCircle(Vector3 center, float radius)
        {
            if (_circle == null) return;

            var diameter = Mathf.Max(0.1f, radius * 2f);
            _circle.SetActive(true);
            _circle.transform.position = center + Vector3.up * HeightOffset;
            _circle.transform.rotation = Quaternion.identity;
            _circle.transform.localScale = new Vector3(diameter, 0.035f, diameter);
        }

        private void ShowDot(Vector3 center, float radius)
        {
            if (_dot == null) return;

            var diameter = Mathf.Max(0.1f, radius * 2f);
            _dot.SetActive(true);
            _dot.transform.position = center + Vector3.up * (HeightOffset + 0.035f);
            _dot.transform.rotation = Quaternion.identity;
            _dot.transform.localScale = Vector3.one * diameter;
        }

        private void ShowSector(Vector3 start, Vector3 direction, float length)
        {
            if (_sector == null) return;

            var safeLength = Mathf.Max(0.1f, length);
            _sector.SetActive(true);
            _sector.transform.position = start + Vector3.up * (HeightOffset + 0.01f);
            _sector.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            _sector.transform.localScale = new Vector3(safeLength, 1f, safeLength);
        }

        private void HideLine()
        {
            if (_line != null) _line.SetActive(false);
        }

        private void HideCircle()
        {
            if (_circle != null) _circle.SetActive(false);
        }

        private void HideDot()
        {
            if (_dot != null) _dot.SetActive(false);
        }

        private void HideSector()
        {
            if (_sector != null) _sector.SetActive(false);
        }

        private void SetColor(Color color)
        {
            SetColor(_line, color);
            SetColor(_circle, color);
            SetColor(_dot, color);
            SetColor(_sector, color);
        }

        private static void SetColor(GameObject go, Color color)
        {
            if (go == null) return;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null || renderer.material == null) return;

            renderer.material.color = color;
        }
    }
}
