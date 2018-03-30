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

        private StateMachine(object startState, Dictionary<Type, List<Transition>> transitions) : this(startState)
        {
            this.transitions = transitions;
        }

        public void AddTransition<TFromState, TWith, TToState>(Func<TFromState, TWith, bool> canTransition, Func<TFromState, TWith, TToState> transition)
        {
            var fromStateType = typeof(TFromState);

            if (!transitions.ContainsKey(fromStateType))
                transitions.Add(fromStateType, new List<Transition>());

            var canTransitionTarget = new MethodTarget(canTransition.Method, canTransition.Target);
            var transitionTarget = new MethodTarget(transition.Method, transition.Target);
            transitions[fromStateType].Add(new Transition(fromStateType, typeof(TWith), typeof(TToState), canTransitionTarget, transitionTarget));
        }

        public StateMachine Copy()
        {
            return new StateMachine(State, transitions);
        }

        public void ForceState(object state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state), "State can't be null!");
            StateType = state.GetType();
        }

        public bool TryTransitioning(object with)
        {
            if (with == null)
                throw new ArgumentNullException(nameof(with), "With object can't be null!");

            if (!transitions.ContainsKey(StateType))
                throw new InvalidOperationException("No Transitions have been added for the type of the current state!");

            var withType = with.GetType();
            foreach (var transition in transitions[StateType].Where(t => t.WithType == withType))
            {
                if (!transition.CanTransition(State, with))
                    continue;

                State = transition.DoTransition(State, with);
                StateType = transition.ToStateType;

                return true;
            }

            return false;
        }

        private struct MethodTarget
        {
            public readonly MethodInfo Method;
            public readonly object Target;

            public MethodTarget(MethodInfo method, object target)
            {
                Method = method;
                Target = target;
            }

            public T Invoke<T>(params object[] parameters)
            {
                return (T)Method.Invoke(Target, parameters);
            }

            public object Invoke(params object[] parameters)
            {
                return Method.Invoke(Target, parameters);
            }
        }

        private sealed class Transition
        {
            private readonly MethodTarget canTransitionTarget;
            private readonly MethodTarget transitionTarget;
            public Type FromStateType { get; }
            public Type ToStateType { get; }
            public Type WithType { get; }

            public Transition(Type fromStateType, Type withType, Type toStateType, MethodTarget canTransitionTarget, MethodTarget transitionTarget)
            {
                FromStateType = fromStateType;
                WithType = withType;
                ToStateType = toStateType;

                this.canTransitionTarget = canTransitionTarget;
                this.transitionTarget = transitionTarget;
            }

            public bool CanTransition(object state, object with)
            {
                return canTransitionTarget.Invoke<bool>(state, with);
            }

            public object DoTransition(object state, object with)
            {
                return transitionTarget.Invoke(state, with) ?? throw new InvalidOperationException("New State can't be null!");
            }

            public bool HasCorrectTypes(object state, object with)
            {
                return state.GetType() == FromStateType && with.GetType() == WithType;
            }
        }
    }
}