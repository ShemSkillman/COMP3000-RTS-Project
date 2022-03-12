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
            Print("Eco needs rebalancing.");
            return State.Success;
        }
        else
        {
            Print("Eco is balanced.");
            return State.Failure;
        }
    }
}
