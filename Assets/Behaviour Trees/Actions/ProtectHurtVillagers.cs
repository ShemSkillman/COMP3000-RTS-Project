using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class ProtectHurtVillagers : ActionNode
{
    protected override State PerformAction() {

        List<Unit> hurtVills = context.economyManager.HurtVillagers;

        if (hurtVills.Count > 0)
        {
            context.gameMgr.MvtMgr.Move(hurtVills[0], context.factionMgr.GetBaseCenter(), 0, null, InputMode.movement, false);
            hurtVills.RemoveAt(0);

            return State.Running;
        }        

        return State.Success;
    }
}
