using System;
using System.Collections.Generic;

namespace AbilityKit.Game.View.Flow
{
    public sealed class PhaseStateFeatureSpec
    {
        private readonly List<string> _featureIds;
        private readonly List<string> _enterBeforeActionIds;
        private readonly List<string> _enterAfterActionIds;
        private readonly List<string> _exitActionIds;
        private readonly List<string> _switchFlowIds;
 
        public PhaseStateFeatureSpec(string stateId, bool clearBeforeEnter = false, int initialFeatureCapacity = 4)
        {
            if (string.IsNullOrEmpty(stateId)) throw new ArgumentException("State id is required.", nameof(stateId));
 
            StateId = stateId;
            ClearBeforeEnter = clearBeforeEnter;
            _featureIds = new List<string>(Math.Max(0, initialFeatureCapacity));
            _enterBeforeActionIds = new List<string>(1);
            _enterAfterActionIds = new List<string>(1);
            _exitActionIds = new List<string>(1);
            _switchFlowIds = new List<string>(1);
        }
 
        public string StateId { get; }
        public bool ClearBeforeEnter { get; }
        public IReadOnlyList<string> FeatureIds => _featureIds;
        public IReadOnlyList<string> EnterBeforeActionIds => _enterBeforeActionIds;
        public IReadOnlyList<string> EnterAfterActionIds => _enterAfterActionIds;
        public IReadOnlyList<string> ExitActionIds => _exitActionIds;
        public IReadOnlyList<string> SwitchFlowIds => _switchFlowIds;

        public PhaseStateFeatureSpec AddFeature(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) throw new ArgumentException("Feature id is required.", nameof(featureId));
 
            _featureIds.Add(featureId);
            return this;
        }

        public PhaseStateFeatureSpec AddEnterBeforeAction(string actionId)
        {
            AddActionId(actionId, _enterBeforeActionIds, nameof(actionId));
            return this;
        }

        public PhaseStateFeatureSpec AddEnterAfterAction(string actionId)
        {
            AddActionId(actionId, _enterAfterActionIds, nameof(actionId));
            return this;
        }

        public PhaseStateFeatureSpec AddExitAction(string actionId)
        {
            AddActionId(actionId, _exitActionIds, nameof(actionId));
            return this;
        }

        public PhaseStateFeatureSpec AddSwitchFlow(string switchFlowId)
        {
            if (string.IsNullOrEmpty(switchFlowId)) throw new ArgumentException("Switch flow id is required.", nameof(switchFlowId));

            _switchFlowIds.Add(switchFlowId);
            return this;
        }

        private static void AddActionId(string actionId, List<string> actionIds, string paramName)
        {
            if (string.IsNullOrEmpty(actionId)) throw new ArgumentException("Action id is required.", paramName);

            actionIds.Add(actionId);
        }
    }
}
