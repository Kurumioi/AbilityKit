using System;

namespace AbilityKit.Demo.Moba.Diagnostics
{
    public enum BattleDiagnosticFrameCursorChangeReason
    {
        None = 0,
        FollowLiveAdvanced = 1,
        UserSelectedFrame = 2,
        SelectionNavigation = 3,
        RetainedRangeClamped = 4,
        SessionChanged = 5
    }

    public readonly struct BattleDiagnosticFrameRange : IEquatable<BattleDiagnosticFrameRange>
    {
        public BattleDiagnosticFrameRange(int firstFrame, int lastFrame)
        {
            FirstFrame = firstFrame;
            LastFrame = lastFrame;
        }

        public int FirstFrame { get; }
        public int LastFrame { get; }

        public bool IsValid =>
            BattleDiagnosticFrames.IsValid(FirstFrame) &&
            LastFrame >= FirstFrame;

        public bool Contains(int frame)
        {
            return IsValid && frame >= FirstFrame && frame <= LastFrame;
        }

        public int Clamp(int frame)
        {
            if (!IsValid)
            {
                return BattleDiagnosticFrames.Invalid;
            }

            if (frame < FirstFrame)
            {
                return FirstFrame;
            }

            return frame > LastFrame ? LastFrame : frame;
        }

        public bool Equals(BattleDiagnosticFrameRange other)
        {
            return FirstFrame == other.FirstFrame && LastFrame == other.LastFrame;
        }

        public override bool Equals(object obj)
        {
            return obj is BattleDiagnosticFrameRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (FirstFrame * 397) ^ LastFrame;
            }
        }

        public static bool operator ==(BattleDiagnosticFrameRange left, BattleDiagnosticFrameRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BattleDiagnosticFrameRange left, BattleDiagnosticFrameRange right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct BattleDiagnosticFrameCursor : IEquatable<BattleDiagnosticFrameCursor>
    {
        public BattleDiagnosticFrameCursor(
            int frame,
            bool followsLive,
            BattleDiagnosticFrameCursorChangeReason changeReason)
        {
            Frame = frame;
            FollowsLive = followsLive;
            ChangeReason = changeReason;
        }

        public int Frame { get; }
        public bool FollowsLive { get; }
        public BattleDiagnosticFrameCursorChangeReason ChangeReason { get; }

        public bool HasFrame => BattleDiagnosticFrames.IsValid(Frame);

        public static BattleDiagnosticFrameCursor CreateFollowingLive(int latestCompleteFrame)
        {
            return new BattleDiagnosticFrameCursor(
                latestCompleteFrame,
                true,
                BattleDiagnosticFrameCursorChangeReason.SessionChanged);
        }

        public BattleDiagnosticFrameCursor SetFollowLive(bool followLive, int latestCompleteFrame)
        {
            if (!followLive)
            {
                return new BattleDiagnosticFrameCursor(Frame, false, ChangeReason);
            }

            return new BattleDiagnosticFrameCursor(
                latestCompleteFrame,
                true,
                BattleDiagnosticFrameCursorChangeReason.FollowLiveAdvanced);
        }

        public BattleDiagnosticFrameCursor AdvanceLive(int latestCompleteFrame)
        {
            if (!FollowsLive || latestCompleteFrame == Frame)
            {
                return this;
            }

            return new BattleDiagnosticFrameCursor(
                latestCompleteFrame,
                true,
                BattleDiagnosticFrameCursorChangeReason.FollowLiveAdvanced);
        }

        public BattleDiagnosticFrameCursor SelectFrame(int frame)
        {
            if (!BattleDiagnosticFrames.IsValid(frame))
            {
                throw new ArgumentOutOfRangeException(nameof(frame), frame, "Frame must be non-negative.");
            }

            return new BattleDiagnosticFrameCursor(
                frame,
                false,
                BattleDiagnosticFrameCursorChangeReason.UserSelectedFrame);
        }

        public BattleDiagnosticFrameCursor NavigateToSelection(BattleDiagnosticSelection selection)
        {
            if (!selection.IsValid || !BattleDiagnosticFrames.IsValid(selection.Frame))
            {
                return this;
            }

            return new BattleDiagnosticFrameCursor(
                selection.Frame,
                false,
                BattleDiagnosticFrameCursorChangeReason.SelectionNavigation);
        }

        public BattleDiagnosticFrameCursor ConstrainTo(BattleDiagnosticFrameRange retainedRange)
        {
            if (!retainedRange.IsValid)
            {
                return new BattleDiagnosticFrameCursor(
                    BattleDiagnosticFrames.Invalid,
                    false,
                    BattleDiagnosticFrameCursorChangeReason.RetainedRangeClamped);
            }

            var clampedFrame = retainedRange.Clamp(Frame);
            if (clampedFrame == Frame)
            {
                return this;
            }

            return new BattleDiagnosticFrameCursor(
                clampedFrame,
                FollowsLive && clampedFrame == retainedRange.LastFrame,
                BattleDiagnosticFrameCursorChangeReason.RetainedRangeClamped);
        }

        public bool Equals(BattleDiagnosticFrameCursor other)
        {
            return Frame == other.Frame &&
                   FollowsLive == other.FollowsLive &&
                   ChangeReason == other.ChangeReason;
        }

        public override bool Equals(object obj)
        {
            return obj is BattleDiagnosticFrameCursor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Frame;
                hashCode = (hashCode * 397) ^ FollowsLive.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ChangeReason;
                return hashCode;
            }
        }

        public static bool operator ==(BattleDiagnosticFrameCursor left, BattleDiagnosticFrameCursor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BattleDiagnosticFrameCursor left, BattleDiagnosticFrameCursor right)
        {
            return !left.Equals(right);
        }
    }
}
