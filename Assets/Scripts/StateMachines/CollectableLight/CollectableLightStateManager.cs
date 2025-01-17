using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableLightStateManager : MonoBehaviour
{
    // FSM States
    CollectableLightBaseState currentState;
    CollectableLightIdleState idleState = new();


    // Variables

    public Guid UUID;

    void Start()
    {
        currentState = idleState;
        currentState.EnterState(this);
    }

    // Update is called once per frame
    void Update()
    {
        currentState.UpdateState(this);
    }

    void SwitchState(CollectableLightBaseState newState)
    {
        currentState.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }
}
