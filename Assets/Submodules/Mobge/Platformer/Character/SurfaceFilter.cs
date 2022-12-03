using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer.Character{
    [CreateAssetMenu(menuName="Mobge/Platformer/Ground Filter")]
    public sealed class SurfaceFilter : ScriptableObject
    {
        private static SurfaceFilter _default;
        public static SurfaceFilter Default{
            get{
                if(_default == null) {
                    _default = CreateInstance<SurfaceFilter>();
                }
                return _default;
            }
        }
        [SerializeField]
        private float minWallAngle = 80;
        [SerializeField]
        private float maxSlopeAngle = 45;
        [SerializeField]
        private LayerMask solidSurfaceMask = 0x1;
        [SerializeField]
        private LayerMask semiSolidSurfaceMask = 0x2;
        private float _minGroundY;
        private float _maxWallX;

        [SerializeField]
        private ContactFilter2D _filter;
        void OnEnable() {
            _filter = _filter.NoFilter();
            _filter.useLayerMask = true;
            _filter.useTriggers = false;
            _filter.useDepth = false;
            _filter.layerMask = solidSurfaceMask | semiSolidSurfaceMask;
            _filter.useNormalAngle = true;
            _filter.maxNormalAngle = 90 + maxSlopeAngle;
            _filter.minNormalAngle = 90 - maxSlopeAngle;
            _minGroundY = Mathf.Sin(_filter.maxNormalAngle * Mathf.Deg2Rad);
            _maxWallX = Mathf.Cos(minWallAngle * Mathf.Deg2Rad);
            //Debug.Log(_maxWallX);
        }
        public LayerMask GroundLayerMask => _filter.layerMask;
        public bool IsGroundNormal(Vector2 normal) {
            return normal.y >= _minGroundY;
        }
        

        public Rigidbody2D FindGround(Rigidbody2D rigidbody, out Vector2 normal) {
            UpdateFilter(true, false);
            var contacts = new ContactPoint2DList(rigidbody, in _filter);
            var uns = contacts.UnsafeContacts;
            if(contacts.Count > 0) {
                normal = contacts[0].normal;
                return contacts[0].rigidbody;
            }
            normal = Vector2.zero;
            return null;
        }
        private void UpdateFilter(bool useNormal, bool useOutsideOfNormal){
            _filter.useNormalAngle = useNormal;
            _filter.useOutsideNormalAngle = useOutsideOfNormal;
        }
        public Collider2dList OverlapBoundsWithOffset(Collider2D col, in Vector2 offset) {
            return Collider2dList.OverlapBoundsWithOffset(col, in _filter, in offset);
        }
        public bool IsTouchingGround(Rigidbody2D rb) {
            UpdateFilter(false, false);
            var result = rb.IsTouching(_filter);
            return result;
        }
        public bool IsTouchingLayers(Rigidbody2D rb, int layerMask) {
            UpdateFilter(false, false);
            var lm = _filter.layerMask;
            _filter.layerMask = layerMask;
            var result = rb.IsTouching(_filter);
            _filter.layerMask = lm;
            return result; 
        }
        private static RaycastHit2D[] s_singleResult = new RaycastHit2D[1];
        public RaycastHit2D CircleCast(in Ray2D ray, float radius, float distance) {
            UpdateFilter(false, false);
            if(Physics2D.CircleCast(ray.origin, radius, ray.direction, _filter, s_singleResult, distance) > 0) {
                return s_singleResult[0];
            }
            return new RaycastHit2D();
        }
        public Rigidbody2D FindCeiling(Rigidbody2D rigidbody, out Vector2 point, out Vector2 normal) {
            UpdateFilter(true, true);
            var contacts = new ContactPoint2DList(rigidbody, in _filter);
            var uns = contacts.UnsafeContacts;
            float maxY = float.NegativeInfinity;
            Rigidbody2D rb = null;
            normal = Vector2.zero;
            point = Vector2.zero;
            for(int i = 0; i < contacts.Count; i++) {
                var c = uns[i];
                if(c.point.y > maxY) {
                    rb = c.rigidbody;
                    normal = c.normal;
                    point = c.point;
                    maxY = c.point.y;
                }
            }
            return rb;
        }
        public Rigidbody2D FindWallTop(Rigidbody2D rigidbody, out Vector2 point, out Vector2 normal) {
            UpdateFilter(true, true);
            var contacts = new ContactPoint2DList(rigidbody, in _filter);
            var uns = contacts.UnsafeContacts;
            float maxY = float.NegativeInfinity;
            Rigidbody2D rb = null;
            normal = Vector2.zero;
            point = Vector2.zero;
            for(int i = 0; i < contacts.Count; i++) {
                var c = uns[i];
                if(c.point.y > maxY && (c.normal.x >= _maxWallX || c.normal.x <= -_maxWallX)) {
                    maxY = point.y;
                    rb = c.rigidbody;
                    normal = c.normal;
                    point = c.point;
                }
            }
            return rb;
        }

        public Rigidbody2D FindGroundOnDirection(Rigidbody2D rigidbody, Vector2 direction, out Vector2 point, out Vector2 normal)
        {
            UpdateFilter(false, false);
            var contacts = new ContactPoint2DList(rigidbody, in _filter);
            var uns = contacts.UnsafeContacts;
            float maxY = float.NegativeInfinity;
            Rigidbody2D rb = null;
            normal = Vector2.zero;
            point = Vector2.zero;
            float min = float.PositiveInfinity;
            for(int i = 0; i < contacts.Count; i++) {
                var c = uns[i];
                var dot = Vector2.Dot(c.normal, direction);
                if(dot < min) {
                    min = dot;
                    rb = c.rigidbody;
                    normal = c.normal;
                    point = c.point;
                    maxY = c.point.y;
                }
            }
            return rb;
        }
    }
}