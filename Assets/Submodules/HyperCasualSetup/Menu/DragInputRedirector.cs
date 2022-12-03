using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Mobge.HyperCasualSetup
{
    public class DragInputRedirector : MonoBehaviour, IEndDragHandler, IDragHandler, IBeginDragHandler
    {
        public Component targetComponent;
        public Behaviour componentToDisable;
        public RedirectCase redirectCase;
        private bool _redirectStarted;

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
            if (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y) == (redirectCase == RedirectCase.HorizontalMove)) {
                if (componentToDisable != null) {
                    componentToDisable.enabled = false;
                }
                _redirectStarted = true;
                if (TryCastTarget(out IBeginDragHandler d)) {
                    d.OnBeginDrag(eventData);
                }
            }
        }
        private bool TryCastTarget<T>(out T c) where T : class {

            c = targetComponent as T;
            if (c != null) {
                return true;
            }
            return false;
        }

        void IDragHandler.OnDrag(PointerEventData eventData) {
            if (_redirectStarted) {
                if (TryCastTarget(out IDragHandler d)) {
                    d.OnDrag(eventData);
                }
            }
        }


        void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
            if (_redirectStarted) {
                _redirectStarted = false;
                if (TryCastTarget(out IEndDragHandler d)) {
                    d.OnEndDrag(eventData);
                }
                if (componentToDisable != null) {
                    componentToDisable.enabled = true;
                }
            }
        }

        public enum RedirectCase
        {
            VerticalMove = 0,
            HorizontalMove = 1
        }
    }
}