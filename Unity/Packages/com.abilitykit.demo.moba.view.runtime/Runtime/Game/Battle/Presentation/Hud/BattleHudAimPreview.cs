using AbilityKit.Game.Battle.Entity;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudAimPreview
    {
        private GameObject _preview;

        public void Tick(BattleContext ctx)
        {
            if (!TryGetPreviewPosition(ctx, out var pos))
            {
                Hide();
                return;
            }

            EnsurePreview();
            _preview.transform.position = pos;
            _preview.SetActive(true);
        }

        public void Clear()
        {
            if (_preview != null)
            {
                Object.Destroy(_preview);
            }

            _preview = null;
        }

        private void Hide()
        {
            if (_preview != null)
            {
                _preview.SetActive(false);
            }
        }

        private void EnsurePreview()
        {
            if (_preview != null) return;

            _preview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _preview.name = "SkillAimPreview";
            _preview.hideFlags = HideFlags.DontSave;
            _preview.transform.localScale = Vector3.one * 0.35f;

            var collider = _preview.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private static bool TryGetPreviewPosition(BattleContext ctx, out Vector3 pos)
        {
            pos = default;
            if (ctx == null || ctx.EntityQuery == null) return false;

            if (!ctx.TryReadHudSkillAim(out _, out var aimDx, out var aimDz))
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

            pos = transform.Position + new Vector3(aimDx, 0f, aimDz);
            return true;
        }
    }
}
