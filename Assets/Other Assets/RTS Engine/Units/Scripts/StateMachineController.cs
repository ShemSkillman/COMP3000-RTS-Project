using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* StateMachineController script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class StateMachineController : StateMachineBehaviour {

        /// <summary>
        /// Defines a parameter and its target/requested state.
        /// </summary>
        [System.Serializable]
        public struct ParameterState
        {
            public string name;
            public bool enable;
        }
        [SerializeField, Tooltip("Input parameters that get enabled/disabled when this animator state is entered.")]
        private ParameterState[] onStateEnter = new ParameterState[0];
        [SerializeField, Tooltip("Input parameters that get enabled/disabled when this animator state is exited.")]
        private ParameterState[] onStateExit = new ParameterState[0];

        /// <summary>
        /// Called when the animator state that has this component attached to it is entered.
        /// </summary>
        /// <param name="animator">The animator component that includes and controls the animator state.</param>
        /// <param name="stateInfo">Information about the current entered state.</param>
        /// <param name="layerIndex">The current layer's index of the animator controller.</param>
		override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            //update parameter states
            foreach (ParameterState param in onStateEnter)
                animator.SetBool(param.name, param.enable);
        }

        /// <summary>
        /// Called when the animator state that has this component attached to it is exited.
        /// </summary>
        /// <param name="animator">The animator component that includes and controls the animator state.</param>
        /// <param name="stateInfo">Information about the current entered state.</param>
        /// <param name="layerIndex">The current layer's index of the animator controller.</param>
		override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            //update parameter states
            foreach (ParameterState param in onStateExit)
                animator.SetBool(param.name, param.enable);
        }
	}
}
