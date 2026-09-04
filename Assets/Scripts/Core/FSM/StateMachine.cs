using System;
using System.Collections.Generic;

namespace KOA.Core.FSM
{
    /// <summary>
    /// Generic Finite State Machine แบบ decoupled จาก Presentation Layer (Section 9)
    /// </summary>
    public class StateMachine<TContext, TStateKey>
    {
        private readonly TContext _context;
        private readonly Dictionary<TStateKey, IFSMState<TContext>> _states = new();

        public TStateKey CurrentStateKey { get; private set; }
        public IFSMState<TContext> CurrentState { get; private set; }

        public event Action<TStateKey, TStateKey> OnStateChanged;

        public StateMachine(TContext context)
        {
            _context = context;
        }

        public void RegisterState(TStateKey key, IFSMState<TContext> state)
        {
            _states[key] = state;
        }

        public void ChangeState(TStateKey newKey)
        {
            if (!_states.TryGetValue(newKey, out var newState))
            {
                throw new InvalidOperationException($"State with key '{newKey}' is not registered.");
            }

            if (EqualityComparer<TStateKey>.Default.Equals(CurrentStateKey, newKey) && CurrentState != null)
            {
                return;
            }

            var previousKey = CurrentStateKey;
            CurrentState?.OnExit(_context);

            CurrentStateKey = newKey;
            CurrentState = newState;
            CurrentState.OnEnter(_context);

            OnStateChanged?.Invoke(previousKey, newKey);
        }

        public void Update(float deltaTime)
        {
            CurrentState?.OnUpdate(_context, deltaTime);
        }
    }
}
