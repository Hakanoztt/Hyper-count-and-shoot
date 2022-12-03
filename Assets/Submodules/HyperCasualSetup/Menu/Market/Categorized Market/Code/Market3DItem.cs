using Mobge.HyperCasualSetup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI.CategorizedMarket {
    public class Market3DItem : MonoBehaviour, IMarketVisual {

        public Transform Visual => _visual;
        public Camera Camera => _previewCamera;

        [SerializeField] protected Camera _previewCamera;

        [SerializeField] protected Transform _visual;
        public void Init(BaseLevelPlayer player) {
            _previewCamera.transparencySortMode = TransparencySortMode.Orthographic;

        }

        public void OnVisualUpdated(ItemSet arg1 = null, int arg2 = -1) {

        }

      
    }
}