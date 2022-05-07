using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TheKiwiCoder {
    public abstract class ActionNode : Node {
        private float startTime;
        int executionCount = 0;
        const int executionCap = 10;

        protected override void OnStart()
        {
            startTime = Time.time;
        }

        protected override void OnStop()
        {
            executionCount = 0;
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
                executionCount++;

                if (executionCount >= executionCap)
                {
                    return State.Success;
                }
            }

            return state;
        }

        protected abstract State PerformAction();
    }
}