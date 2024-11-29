using UnityEditor;
using UnityEngine;

public abstract class State 
{
        public abstract void Enter();
        public abstract void Tick();
        public abstract void Exit();
 
}
public abstract class StateMachine : MonoBehaviour //core logic for switching states and executing their enter, exist and tick methods
{
    private State currentState; //pulls the current state of type state and two methods, one for switching states

    public void SwitchState(State state) //switches state
    {
        currentState?.Exit(); //exits the current state
        currentState = state; //switches to new state
        currentState.Enter(); //calls enter to start new state
    }

    private void Update()
    {
        currentState?.Tick(); //calls the tick method once per frame until state needs to switch
    }
}


