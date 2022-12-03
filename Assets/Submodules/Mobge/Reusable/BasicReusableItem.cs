using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public class BasicReusableItem : AReusableItem
    {
        public override bool IsActive => mainGameObject.activeSelf;
        public GameObject mainGameObject;

        protected void Awake()
        {
            mainGameObject.SetActive(false);
        }
        protected override void OnPlay()
        {
            mainGameObject.SetActive(true);
        }
        public override void Stop()
        {
            mainGameObject.SetActive(false);
        }

        public override void StopImmediately()
        {
            mainGameObject.SetActive(false);
        }

    }
}