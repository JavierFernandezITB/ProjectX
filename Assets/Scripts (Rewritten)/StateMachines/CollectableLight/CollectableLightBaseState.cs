using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CollectableLightBaseState
{
    public abstract void EnterState(CollectableLightStateManager collectableLight);
    public abstract void UpdateState(CollectableLightStateManager collectableLight);
    public abstract void ExitState(CollectableLightStateManager collectableLight);
}
