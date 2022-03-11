using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class CheckEcoOutOfBalance : ActionNode
{
    protected override State PerformAction() {

        int coinCollectorCount = context.economyManager.GetResourceCollectorCount(context.Info.IronMine);
        int woodCollectorCount = context.economyManager.GetResourceCollectorCount(context.Info.Tree);

        if (Mathf.Abs(coinCollectorCount - woodCollectorCount) > 1)
        {
            return State.Success;
        }
        else
        {
            return State.Failure;
        }
    }
}
