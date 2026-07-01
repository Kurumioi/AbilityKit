using System;
using AbilityKit.Combat.MotionSystem.Constraints;
using AbilityKit.Combat.MotionSystem.Core;
using AbilityKit.Core.Mathematics;

namespace AbilityKit.Combat.MotionSystem.Collision
{
    public sealed class ConfigurableMotionSolver : IMotionSolver
    {
        public delegate MotionConstraints ConstraintsProvider(int moverId, in MotionState state, in MotionOutput input, float dt);

        private readonly IMotionCollisionWorld _world;
        private readonly ConstraintsProvider _constraints;

        public ConfigurableMotionSolver(IMotionCollisionWorld world, ConstraintsProvider constraints, IMotionSolverDiagnostics diagnostics = null)
        {
            _world = world;
            _constraints = constraints;
            Diagnostics = diagnostics;
        }

        public IMotionSolverDiagnostics Diagnostics { get; set; }

        public MotionSolveResult Solve(int id, in MotionState state, in MotionOutput input, float dt)
        {
            var constraints = ResolveConstraints(id, in state, in input, dt);
            var desiredDelta = input.DesiredDelta;

            if (constraints.Leash.Enable && constraints.Leash.Radius > 0f)
            {
                desiredDelta = ApplyLeash(in state.Position, in desiredDelta, constraints.Leash);
            }

            if (!constraints.Collision.Enable) return MotionSolveResult.NoHit(desiredDelta);
            if (_world == null) return MotionSolveResult.NoHit(desiredDelta);
            if (constraints.Collision.AllowPassThrough) return MotionSolveResult.NoHit(desiredDelta);

            var start = state.Position;
            var desired = desiredDelta;

            if (_world.Sweep(
                    moverId: id,
                    start: in start,
                    desiredDelta: in desired,
                    radius: constraints.Collision.Radius,
                    obstacleMask: constraints.Collision.ObstacleMask,
                    ignoreMask: constraints.Collision.IgnoreMask,
                    hit: out var hit,
                    appliedDelta: out var applied))
            {
                return ResolveEndOverlap(id, in state, in constraints.Collision, in applied, in hit);
            }

            var noHit = new MotionHit(false, 0, Vec3.Zero, 0f);
            return ResolveEndOverlap(id, in state, in constraints.Collision, in desired, in noHit);
        }

        private MotionConstraints ResolveConstraints(int id, in MotionState state, in MotionOutput input, float dt)
        {
            if (_constraints == null) return MotionConstraints.Disabled;

            try
            {
                return _constraints.Invoke(id, in state, in input, dt);
            }
            catch (Exception ex)
            {
                Diagnostics?.OnConstraintsProviderException(id, in state, in input, dt, ex);
                return MotionConstraints.Disabled;
            }
        }

        private MotionSolveResult ResolveEndOverlap(int id, in MotionState state, in MotionCollisionConstraints constraints, in Vec3 candidateDelta, in MotionHit hit)
        {
            var end = state.Position + candidateDelta;
            if (!_world.Overlap(id, in end, constraints.Radius, constraints.ObstacleMask, constraints.IgnoreMask))
            {
                return new MotionSolveResult(candidateDelta, hit);
            }

            switch (constraints.EndOverlapPolicy)
            {
                case MotionEndOverlapPolicy.AllowInside:
                    Diagnostics?.OnEndOverlapResolved(id, in state, in constraints, constraints.EndOverlapPolicy, true);
                    return new MotionSolveResult(candidateDelta, hit);

                case MotionEndOverlapPolicy.ProjectToNearestFree:
                    if (_world.TryProjectToFree(id, in end, constraints.Radius, constraints.ObstacleMask, constraints.IgnoreMask, out var projected))
                    {
                        var projectedDelta = projected - state.Position;
                        Diagnostics?.OnEndOverlapResolved(id, in state, in constraints, constraints.EndOverlapPolicy, true);
                        return new MotionSolveResult(projectedDelta, hit);
                    }

                    Diagnostics?.OnEndOverlapResolved(id, in state, in constraints, constraints.EndOverlapPolicy, false);
                    return MotionSolveResult.NoHit(Vec3.Zero);

                case MotionEndOverlapPolicy.ClampToLastValid:
                    Diagnostics?.OnEndOverlapResolved(id, in state, in constraints, constraints.EndOverlapPolicy, true);
                    return new MotionSolveResult(candidateDelta, hit);

                case MotionEndOverlapPolicy.Reject:
                default:
                    Diagnostics?.OnEndOverlapResolved(id, in state, in constraints, constraints.EndOverlapPolicy, false);
                    return MotionSolveResult.NoHit(Vec3.Zero);
            }
        }

        private static Vec3 ApplyLeash(in Vec3 start, in Vec3 desiredDelta, in MotionLeashConstraints leash)
        {
            var endX = start.X + desiredDelta.X;
            var endZ = start.Z + desiredDelta.Z;

            var dx = endX - leash.Center.X;
            var dz = endZ - leash.Center.Z;

            var dist2 = dx * dx + dz * dz;
            var r = leash.Radius;
            var r2 = r * r;
            if (dist2 <= r2)
            {
                return desiredDelta;
            }

            switch (leash.Policy)
            {
                case MotionLeashPolicy.Reject:
                    return Vec3.Zero;
                case MotionLeashPolicy.ClampToRadius:
                default:
                    break;
            }

            var dist = (float)Math.Sqrt(dist2);
            if (dist <= 1e-6f)
            {
                return Vec3.Zero;
            }

            var s = r / dist;
            var clampedEndX = leash.Center.X + dx * s;
            var clampedEndZ = leash.Center.Z + dz * s;

            return new Vec3(clampedEndX - start.X, desiredDelta.Y, clampedEndZ - start.Z);
        }
    }
}
