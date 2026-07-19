using System;

namespace AbilityKit.Demo.Moba.Diagnostics
{
    public sealed class BattleDiagnosticWorkspaceState
    {
        private readonly BattleDiagnosticNavigationHistory _navigation;

        public BattleDiagnosticWorkspaceState(int navigationCapacity = BattleDiagnosticNavigationHistory.DefaultCapacity)
        {
            _navigation = new BattleDiagnosticNavigationHistory(navigationCapacity);
            FrameCursor = new BattleDiagnosticFrameCursor(
                BattleDiagnosticFrames.Invalid,
                true,
                BattleDiagnosticFrameCursorChangeReason.None);
            Filter = BattleDiagnosticFilter.Default;
        }

        public event Action Changed;

        public BattleDiagnosticSessionScope Scope { get; private set; }
        public BattleDiagnosticSelection Selection { get; private set; }
        public BattleDiagnosticFrameCursor FrameCursor { get; private set; }
        public BattleDiagnosticFilter Filter { get; private set; }
        public BattleDiagnosticNavigationHistory Navigation => _navigation;

        public void AttachSession(BattleDiagnosticSessionScope scope, int latestCompleteFrame)
        {
            if (!scope.IsValid)
            {
                throw new ArgumentException("A valid session scope is required.", nameof(scope));
            }

            var scopeChanged = scope != Scope;
            Scope = scope;
            FrameCursor = BattleDiagnosticFrameCursor.CreateFollowingLive(latestCompleteFrame);

            if (scopeChanged)
            {
                Selection = default;
                _navigation.Reset(scope);
            }

            RaiseChanged();
        }

        public void DetachSession()
        {
            Scope = default;
            Selection = default;
            FrameCursor = new BattleDiagnosticFrameCursor(
                BattleDiagnosticFrames.Invalid,
                false,
                BattleDiagnosticFrameCursorChangeReason.SessionChanged);
            _navigation.Reset(default);
            RaiseChanged();
        }

        public bool Select(BattleDiagnosticSelection selection, bool navigateToSelectionFrame = true)
        {
            if (!selection.BelongsTo(Scope))
            {
                return false;
            }

            var selectionChanged = selection != Selection;
            if (!selectionChanged)
            {
                return false;
            }

            Selection = selection;
            _navigation.NavigateTo(selection);
            if (navigateToSelectionFrame)
            {
                FrameCursor = FrameCursor.NavigateToSelection(selection);
            }

            RaiseChanged();
            return true;
        }

        public bool GoBack()
        {
            if (!_navigation.TryGoBack(out var selection))
            {
                return false;
            }

            ApplyNavigationSelection(selection);
            return true;
        }

        public bool GoForward()
        {
            if (!_navigation.TryGoForward(out var selection))
            {
                return false;
            }

            ApplyNavigationSelection(selection);
            return true;
        }

        public void SetFrame(int frame)
        {
            var next = FrameCursor.SelectFrame(frame);
            if (next == FrameCursor)
            {
                return;
            }

            FrameCursor = next;
            RaiseChanged();
        }

        public void SetFollowLive(bool followLive, int latestCompleteFrame)
        {
            var next = FrameCursor.SetFollowLive(followLive, latestCompleteFrame);
            if (next == FrameCursor)
            {
                return;
            }

            FrameCursor = next;
            RaiseChanged();
        }

        public void AdvanceLive(int latestCompleteFrame)
        {
            var next = FrameCursor.AdvanceLive(latestCompleteFrame);
            if (next == FrameCursor)
            {
                return;
            }

            FrameCursor = next;
            RaiseChanged();
        }

        public void ConstrainToRetainedRange(BattleDiagnosticFrameRange retainedRange)
        {
            var next = FrameCursor.ConstrainTo(retainedRange);
            if (next == FrameCursor)
            {
                return;
            }

            FrameCursor = next;
            RaiseChanged();
        }

        public void SetFilter(BattleDiagnosticFilter filter)
        {
            if (filter.Equals(Filter))
            {
                return;
            }

            Filter = filter;
            RaiseChanged();
        }

        private void ApplyNavigationSelection(BattleDiagnosticSelection selection)
        {
            Selection = selection;
            FrameCursor = FrameCursor.NavigateToSelection(selection);
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
