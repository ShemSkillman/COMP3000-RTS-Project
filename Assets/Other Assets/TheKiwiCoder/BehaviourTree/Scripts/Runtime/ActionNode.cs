using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TheKiwiCoder {
    public abstract class ActionNode : Node {
        private float startTime;

        protected override void OnStart()
        {
            startTime = Time.time;
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            if (Time.time - startTime <= context.factionMgr.Slot.NPCType.PerformActionTime)
            {
                return State.Running;
            }

            State state = PerformAction();
            if (state == State.Running)
            {
                startTime = Time.time;
            }

            return state;
        }

        protected abstract State PerformAction();
    }
}