#nullable enable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace UnityHFSM.Extension
{
    public readonly struct HfsmRuntimeActionStateSpec<TAction>
    {
        public readonly string Id;
        public readonly float IntervalSeconds;
        public readonly IReadOnlyList<TAction> Actions;

        public HfsmRuntimeActionStateSpec(string id, float intervalSeconds, IReadOnlyList<TAction>? actions)
        {
            Id = id ?? string.Empty;
            IntervalSeconds = intervalSeconds < 0f ? 0f : intervalSeconds;
            Actions = actions ?? Array.Empty<TAction>();
        }
    }

    public readonly struct HfsmRuntimeTransitionSpec
    {
        public readonly string From;
        public readonly string To;
        public readonly string Condition;

        public HfsmRuntimeTransitionSpec(string from, string to, string condition)
        {
            From = from ?? string.Empty;
            To = to ?? string.Empty;
            Condition = condition ?? string.Empty;
        }
    }

    public sealed class HfsmRuntimeProfile<TAction>
    {
        public HfsmRuntimeProfile(
            string startState,
            IReadOnlyList<HfsmRuntimeActionStateSpec<TAction>>? states,
            IReadOnlyList<HfsmRuntimeTransitionSpec>? transitions)
        {
            StartState = startState ?? string.Empty;
            States = states ?? Array.Empty<HfsmRuntimeActionStateSpec<TAction>>();
            Transitions = transitions ?? Array.Empty<HfsmRuntimeTransitionSpec>();
        }

        public string StartState { get; }

        public IReadOnlyList<HfsmRuntimeActionStateSpec<TAction>> States { get; }

        public IReadOnlyList<HfsmRuntimeTransitionSpec> Transitions { get; }
    }

    public sealed class HfsmRuntimeProfileBuilder<TBlackboard, TAction>
    {
        private readonly Func<TBlackboard, TAction, IActionBehaviour> _actionFactory;
        private readonly Func<TBlackboard, string, bool> _conditionEvaluator;

        public HfsmRuntimeProfileBuilder(
            Func<TBlackboard, TAction, IActionBehaviour> actionFactory,
            Func<TBlackboard, string, bool> conditionEvaluator)
        {
            _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
            _conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
        }

        public StateMachine<string> Build(
            IActionTimeSource timeSource,
            TBlackboard blackboard,
            HfsmRuntimeProfile<TAction> profile)
        {
            if (timeSource == null) throw new ArgumentNullException(nameof(timeSource));
            if (blackboard == null) throw new ArgumentNullException(nameof(blackboard));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var fsm = new StateMachine<string>(needsExitTime: false);
            var stateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < profile.States.Count; i++)
            {
                var state = profile.States[i];
                if (string.IsNullOrWhiteSpace(state.Id))
                {
                    continue;
                }

                stateIds.Add(state.Id);
                fsm.AddState(state.Id, CreateActionState(timeSource, blackboard, state));
            }

            for (var i = 0; i < profile.Transitions.Count; i++)
            {
                var transition = profile.Transitions[i];
                if (!stateIds.Contains(transition.From) || !stateIds.Contains(transition.To))
                {
                    continue;
                }

                fsm.AddTransition(new Transition<string>(transition.From, transition.To, _ => _conditionEvaluator(blackboard, transition.Condition)));
            }

            var startState = string.IsNullOrWhiteSpace(profile.StartState) ? SelectFallbackStartState(profile.States) : profile.StartState;
            if (!stateIds.Contains(startState))
            {
                startState = SelectFallbackStartState(profile.States);
            }

            if (string.IsNullOrWhiteSpace(startState))
            {
                throw new InvalidOperationException("HFSM profile must contain at least one valid state.");
            }

            fsm.SetStartState(startState);
            fsm.Init();
            return fsm;
        }

        private CompositeActionState<string, string> CreateActionState(
            IActionTimeSource timeSource,
            TBlackboard blackboard,
            HfsmRuntimeActionStateSpec<TAction> state)
        {
            var sequence = new SequenceBehaviour();
            for (var i = 0; i < state.Actions.Count; i++)
            {
                sequence.Add(_actionFactory(blackboard, state.Actions[i]));
            }

            if (state.IntervalSeconds > 0f)
            {
                sequence.Add(new DelayBehaviour(state.IntervalSeconds));
            }

            return new CompositeActionState<string>(needsExitTime: false)
                .SetTimeSource(timeSource)
                .SetLoop(true)
                .SetRoot(sequence);
        }

        private static string SelectFallbackStartState(IReadOnlyList<HfsmRuntimeActionStateSpec<TAction>> states)
        {
            for (var i = 0; i < states.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(states[i].Id))
                {
                    return states[i].Id;
                }
            }

            return string.Empty;
        }
    }
}
