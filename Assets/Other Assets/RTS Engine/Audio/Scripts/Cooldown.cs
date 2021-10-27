using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public class Cooldown
    {
        private bool enabled = false;
        public bool Enabled
        {
            set
            {
                enabled = value;

                if (!enabled && coroutine != null)
                {
                    source.StopCoroutine(coroutine);
                    coroutine = null;
                }
                else if (enabled && coroutine == null)
                    coroutine = source.StartCoroutine(CooldownCoroutine());
            }
            get => enabled;
        }

        [SerializeField, Tooltip("Cooldown time.")]
        private FloatRange timeRange = new FloatRange(1.0f, 2.0f);
        private Coroutine coroutine = null;

        private MonoBehaviour source;

        //V1:
        public void Init(MonoBehaviour source)
        {
            this.source = source;
        }

        private IEnumerator CooldownCoroutine ()
        {
            yield return new WaitForSeconds(timeRange.getRandomValue());

            Enabled = false;
        }
    }
}
