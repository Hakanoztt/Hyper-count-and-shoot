using Mobge.Core;
using Mobge.Core.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.UI
{
    public class OffScreenIndicatorCanvas : MonoBehaviour, IComponentExtension
    {
        public const string ic_key = "ic_data";
        public static OffScreenIndicatorCanvas GetCanvas(LevelPlayer player) {
            player.TryGetExtra(ic_key, out OffScreenIndicatorCanvas data);
            return data;
        }

        LevelPlayer _player;
        [OwnComponent(true)] public Canvas canvas;
        public UICollection<UIItem> indicators;
        public float edgeOffset = 30;
        public int extraIndexForRotation = -1;

        private List<IndicatorData> _indicatorList;
        private List<int> _includedIndicators;
        private RectTransform _transform;




        public List<IndicatorData> IndicatorList => _indicatorList;



        void IComponentExtension.Start(in BaseComponent.InitArgs initData)
        {
            _player = initData.player;
            _player.SetExtra(ic_key,this);
            _indicatorList = new List<IndicatorData>();
            _includedIndicators = new List<int>();
            canvas.worldCamera = Camera.main;
            _transform = (RectTransform)canvas.transform;
        }

        public struct IndicatorData
        {
            public Transform transform;
            public Sprite icon;
        }


        void LateUpdate() {
            var camera = canvas.worldCamera;
            _includedIndicators.Clear();
            for (int i = 0; i < _indicatorList.Count; i++) {
                var target = _indicatorList[i];
                var position = target.transform.position;
                Vector3 viewPoint = camera.WorldToViewportPoint(position);
                Rect r = new Rect(0, 0, 1, 1);
                if (viewPoint.z < 0 || !r.Contains(viewPoint)) {
                    _includedIndicators.Add(i);
                }
            }
            indicators.Count = _includedIndicators.Count;
            for (int i = 0; i < _includedIndicators.Count; i++) {
                var ui = indicators[i];
                var target = _indicatorList[_includedIndicators[i]];
                ui.images[0].sprite = target.icon;
                var position = target.transform.position;


                var canvasRect = _transform.rect;
                //Vector2 rectCenter = canvasRect.center;

                Vector2 rTarget = _transform.InverseTransformPoint(position);
                Vector2 rOrigin = Vector2.zero;
                Ray2D ray = new Ray2D(rOrigin, rTarget - rOrigin);

                GeometryUtils.RayIntersectsRect(ray, canvasRect, out var intersectionPoint);
                ui.transform.localPosition = intersectionPoint - ray.direction * edgeOffset;

                if (extraIndexForRotation >= 0) {
                    var rotationRoot = ui.extraReferences[extraIndexForRotation];
                    float angle = Mathf.Atan2(rTarget.x, rTarget.y) * Mathf.Rad2Deg;
                    rotationRoot.transform.localRotation = Quaternion.Euler(rotationRoot.transform.localEulerAngles.x, rotationRoot.transform.localEulerAngles.y, -angle);
                }

            }

        }
         
        public void Register(IndicatorData chr)
        {
            

            _indicatorList.Add(chr);
        }
        public bool UnRegister(IndicatorData chr)
        {
            int index = _indicatorList.IndexOf(chr);
            if (index >= 0) {
                return true;
            }
            return false;
        }


    }
}
