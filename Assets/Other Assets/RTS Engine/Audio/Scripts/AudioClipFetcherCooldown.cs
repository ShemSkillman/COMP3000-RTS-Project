using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public class AudioClipFetcherCooldown : AudioClipFetcher
    {
        [SerializeField, Tooltip("Play audio clip again only if cooldown is disabled.")]
        private Cooldown cooldown = new Cooldown();
        public Cooldown Cooldown => cooldown;

        public override AudioClip Fetch()
        {
            if (cooldown.Enabled)
                return null;

            cooldown.Enabled = true;
            return base.Fetch();
        }
    }
}
