using AbilityKit.Combat.MotionSystem.Core;
using AbilityKit.Core.Mathematics;
using AbilityKit.Core.Pooling;

namespace AbilityKit.Combat.MotionSystem.Trajectory
{
    public sealed class TrajectoryMotionSource : IMotionSource, IMotionFinishEventSource, IMotionSnapshotSource
    {
        private static readonly ObjectPool<TrajectoryMotionSource> Pool = Pools.GetPool(
            createFunc: () => new TrajectoryMotionSource(),
            onRelease: s => s.Reset(),
            defaultCapacity: 64,
            maxSize: 2048,
            collectionCheck: false);

        private ITrajectory3D _trajectory;
        private int _priority;
        private int _groupId;
        private MotionStacking _stacking;
        private float _time;
        private bool _active;

        private TrajectoryMotionSource()
        {
            Reset();
        }

        public TrajectoryMotionSource(ITrajectory3D trajectory, int priority = 10, int groupId = MotionGroups.Ability, MotionStacking stacking = MotionStacking.ExclusiveHighestPriority)
        {
            Configure(trajectory, priority, groupId, stacking);
        }

        public static TrajectoryMotionSource Rent(ITrajectory3D trajectory, int priority = 10, int groupId = MotionGroups.Ability, MotionStacking stacking = MotionStacking.ExclusiveHighestPriority)
        {
            var source = Pool.Get();
            source.Configure(trajectory, priority, groupId, stacking);
            return source;
        }

        public static void Release(TrajectoryMotionSource source)
        {
            if (source == null) return;
            Pool.Release(source);
        }

        public void Configure(ITrajectory3D trajectory, int priority = 10, int groupId = MotionGroups.Ability, MotionStacking stacking = MotionStacking.ExclusiveHighestPriority)
        {
            _trajectory = trajectory;
            _priority = priority;
            _groupId = groupId;
            _stacking = stacking;
            _time = 0f;
            _active = trajectory != null && trajectory.Duration > 0f;
        }

        public void Reset()
        {
            _trajectory = null;
            _priority = 10;
            _groupId = MotionGroups.Ability;
            _stacking = MotionStacking.ExclusiveHighestPriority;
            _time = 0f;
            _active = false;
        }

        public int GroupId => _groupId;

        public MotionStacking Stacking => _stacking;

        public MotionFinishEvent FinishEvent => MotionFinishEvent.Arrive;

        public int Priority => _priority;
        public bool IsActive => _active;

        public float Time => _time;

        public bool IsFinished
        {
            get
            {
                if (!_active || _trajectory == null) return true;
                return _time >= _trajectory.Duration;
            }
        }

        public void Tick(int id, ref MotionState state, float dt, ref Vec3 outDesiredDelta)
        {
            if (!_active || _trajectory == null) return;
            if (dt <= 0f) return;

            var prev = _trajectory.SamplePosition(_time);
            _time += dt;
            if (_time > _trajectory.Duration) _time = _trajectory.Duration;
            var next = _trajectory.SamplePosition(_time);

            outDesiredDelta = outDesiredDelta + (next - prev);

            if (_trajectory.TrySampleForward(_time, out var f))
            {
                state.Forward = f;
            }

            if (_time >= _trajectory.Duration)
            {
                _active = false;
            }
        }

        public void Cancel()
        {
            _active = false;
        }

        public bool ExportSnapshot(out MotionSourceSnapshot snapshot)
        {
            snapshot = new MotionSourceSnapshot
            {
                GroupId = _groupId,
                Priority = _priority,
                Stacking = _stacking,
                IsActive = _active,
                Time = _time,
            };
            return true;
        }

        public bool ImportSnapshot(in MotionSourceSnapshot snapshot)
        {
            _groupId = snapshot.GroupId;
            _priority = snapshot.Priority;
            _stacking = snapshot.Stacking;
            _active = snapshot.IsActive && _trajectory != null;
            _time = snapshot.Time;
            if (_trajectory != null && _time > _trajectory.Duration) _time = _trajectory.Duration;
            if (_time < 0f) _time = 0f;
            return _trajectory != null;
        }
    }
}
