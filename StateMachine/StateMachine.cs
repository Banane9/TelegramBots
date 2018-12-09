using System;
using System.Collections.Generic;

namespace StateMachine
{
    public abstract class StateMachine<TStateMachine, TStates, TWith>
        where TStateMachine : StateMachine<TStateMachine, TStates, TWith>
    {
        private readonly Dictionary<Type, ITransition<TStateMachine, >>
    }

    internal class TestMachine : StateMachine<TestMachine, IState, int>
    { }
}