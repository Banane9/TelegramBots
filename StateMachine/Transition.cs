using System;

namespace StateMachine
{
    public interface ITransition<in TStateMachine, out TFrom, in TWith, out TTo>
        where TStateMachine : StateMachine<TStateMachine, TWith>
    {
        bool CanTransition(TStateMachine stateMachine, TFrom from, TWith with);

        TTo DoTransition(TStateMachine stateMachine, TFrom from, TWith with);
    }

    internal class Test : ITransition<TestMachine, int, int, int>
    {
        public bool CanTransition(TestMachine stateMachine, int from, int with)
        {
            throw new NotImplementedException();
        }

        public int DoTransition(TestMachine stateMachine, int from, int with)
        {
            throw new NotImplementedException();
        }
    }
}