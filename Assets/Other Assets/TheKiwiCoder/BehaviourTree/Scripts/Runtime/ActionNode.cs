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
            float waitTime = Time.time - startTime;

            if (waitTime <= context.factionMgr.Slot.NPCType.PerformActionTime)
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