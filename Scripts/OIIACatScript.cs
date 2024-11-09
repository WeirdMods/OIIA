using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace OIIA.Scripts
{
    internal class OIIACatScript : PhysicsProp
    {
        private Animator _animator = null!;
        private AudioSource _audio = null!;

        private const float BaseAnimSpeed = 0.3f;
        private const float MaxSpeedMultiplier = 3f;

        void Awake()
        {
            grabbable = true;

            itemProperties.positionOffset = new Vector3(-0.35f, 0.3f, 0.1f);
            itemProperties.rotationOffset = new Vector3(-90f, 28f, -90f);

            _animator = GetComponentInChildren<Animator>();
            _audio = transform.Find("Cat/OIIAAudio").gameObject.GetComponent<AudioSource>();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            SetSpinningServerRpc(buttonDown, Random.Range(1f, MaxSpeedMultiplier));
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetSpinningServerRpc(bool spinning, float speed)
        {
            SetSpinningClientRpc(spinning, speed);
        }

        [ClientRpc]
        private void SetSpinningClientRpc(bool spinning, float speed)
        {
            _animator.SetBool("Spinning", spinning);
            _animator.SetFloat("Speed", BaseAnimSpeed * speed);

            var steps = MaxSpeedMultiplier / OIIA.Instance.OIIAClips.Count;
            var clipIndex = Math.Min((int)Math.Floor(speed / steps), OIIA.Instance.OIIAClips.Count - 1);

            _audio.clip = OIIA.Instance.OIIAClips[clipIndex];

            if (spinning)
            {
                _audio.Play();
            }
            else
            {
                _audio.Stop();
            }
        }
    }
}
