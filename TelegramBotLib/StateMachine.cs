using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TelegramBotLib
{
    public class StateMachine
    {
        private readonly Dictionary<Type, List<Transition>> transitions = new Dictionary<Type, List<Transition>>();

        public object State { get; private set; }

        public Type StateType { get; private set; }

        public StateMachine(object startState)
        {
            State = startState ?? throw new ArgumentNullException(nameof(startState), "Start State can't be null!");

            StateType = startState.GetType();
        }

        public void AddTransition<TFromState, TWith, TToState>(Func<TFromState, TWith, bool> canTransition, Func<TFromState, TWith, TToState> transition)
        {
            var fromStateType = typeof(TFromState);

            if (!transitions.ContainsKey(fromStateType))
                transitions.Add(fromStateType, new List<Transition>());

            transitions[fromStateType].Add(new Transition(fromStateType, typeof(TWith), typeof(TToState), canTransition.Method, transition.Method));
        }

        public bool TryTransitioning(object with)
        {
            if (with == null)
                throw new ArgumentNullException(nameof(with), "With object can't be null!");

            if (!transitions.ContainsKey(StateType))
                return false;

            foreach (var transition in transitions[StateType])
            {
                if (!transition.HasCorrectTypes(State, with) || !transition.CanTransition(State, with))
                    continue;

                State = transition.DoTransition(State, with);
                StateType = transition.ToStateType;

                return true;
            }

            return false;
        }

        private sealed class Transition
        {
            private readonly MethodInfo canTransitionMethod;
            private readonly MethodInfo transitionMethod;
            public Type FromStateType { get; }
            public Type ToStateType { get; }
            public Type WithType { get; }

            public Transition(Type fromStateType, Type toStateType, Type withType, MethodInfo canTransitionMethod, MethodInfo transitionMethod)
            {
                FromStateType = fromStateType;
                WithType = withType;
                ToStateType = toStateType;

                this.canTransitionMethod = canTransitionMethod;
                this.transitionMethod = transitionMethod;
            }

            public bool CanTransition(object state, object with)
            {
                return canTransitionMethod.Invoke<bool>(null, state, with);
            }

            public object DoTransition(object state, object with)
            {
                return transitionMethod.Invoke(null, state, with) ?? throw new InvalidOperationException("New State can't be null!");
            }

            public bool HasCorrectTypes(object state, object with)
            {
                return state.GetType() == FromStateType && with.GetType() == WithType;
            }
        }
    }
}