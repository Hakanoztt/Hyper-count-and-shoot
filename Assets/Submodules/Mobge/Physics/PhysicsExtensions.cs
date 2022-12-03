using System;
using UnityEngine;
namespace Mobge{
    public static class PhysicsExtensions
    {
        public static ContactPoint2DList GetContacts(this Rigidbody2D rigidbody, in ContactFilter2D filter) {
            return new ContactPoint2DList(rigidbody, in filter);
        }
        public static Collider2dList OverlapBoundsWithOffset(this Collider2D col, in ContactFilter2D filter, in Vector2 offset) {
            return Collider2dList.OverlapBoundsWithOffset(col, in filter, in offset);
        }
        public static void MoveByCenterOfMass(this Rigidbody rigidbody, Vector3 centerOfMassPosition) {
            var offset = rigidbody.worldCenterOfMass - rigidbody.position;
            rigidbody.MovePosition(centerOfMassPosition - offset);
        }
        public static void ApplyAngularVelocity(this ref Quaternion rotation, float deltaTime, Vector3 angularVelocityInRadians) {
            var velocity = new Quaternion(angularVelocityInRadians.x, angularVelocityInRadians.y, angularVelocityInRadians.z, 0);
            float scalarComponent = deltaTime * 0.5f;
            Quaternion qComponent = velocity * rotation;
            qComponent.x *= scalarComponent;
            qComponent.y *= scalarComponent;
            qComponent.z *= scalarComponent;
            qComponent.w *= scalarComponent;


            rotation.x += qComponent.x;
            rotation.y += qComponent.y;
            rotation.z += qComponent.z;
            rotation.w += qComponent.w;

            rotation.Normalize();
        }
        public static bool TryCalculateRequiredAngularVelocity(in Quaternion rotation, Quaternion targetRotation, float deltaTime, out Vector3 velocity) {
            float scalarComponent = deltaTime * 0.5f;

            targetRotation.x -= rotation.x;
            targetRotation.y -= rotation.y;
            targetRotation.z -= rotation.z;
            targetRotation.w -= rotation.w;

            targetRotation.x /= scalarComponent;
            targetRotation.y /= scalarComponent;
            targetRotation.z /= scalarComponent;
            targetRotation.w /= scalarComponent;

            var velq = targetRotation * Quaternion.Inverse(rotation);
            var vel = new Vector3(velq.x, velq.y, velq.z);
            velocity = vel;
            var dif = Quaternion.Angle(rotation, targetRotation) * Mathf.Deg2Rad / deltaTime;

            // there should be a correct solution for these kind of situations
            // todo: this function should never return false and try should be removed from its name

            if (vel.sqrMagnitude > dif*dif * 2f) {
                return false;
                
            }
            return true;
        }     
        public static Vector3 GetLocalCenter(this Collider @this) {

            if (@this is BoxCollider bc) {
                return bc.center;
            }
            if (@this is CapsuleCollider cc) {
                return cc.center;
            }
            if (@this is SphereCollider sc) {
                return sc.center;
            }
            return Vector3.zero;
        }
        private static float Magnitude(in Quaternion q) {
            return Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        }
    }
    public struct Collider2dList {
        private int _count;
        private int _requestId;
        private static int _currentRequest;
        private static int NextRequest() {
            return ++_currentRequest;
        }
        public int Count{ get{ return _count; } }
        private static readonly Collider2D[] _contacts = new Collider2D[50];
        public static Collider2D[] Buffer => _contacts;
        public Collider2dList(int count) {
            _count = count;
            _requestId = NextRequest();
        }
        public static Collider2dList OverlapBoundsWithOffset(Collider2D collider, in ContactFilter2D filter, in Vector2 offset){
            Collider2dList l;
            l._requestId = NextRequest();
            var b = collider.bounds;
            l._count = Physics2D.OverlapArea((Vector2)b.min + offset, (Vector2)b.max + offset, filter, _contacts);
            return l;
        }
        public static Collider2dList OverlapCircle(Vector2 position, float radius, int layerMask) {
            Collider2dList l;
            l._requestId = NextRequest();
            l._count = Physics2D.OverlapCircleNonAlloc(position, radius, _contacts, layerMask);
            return l;
        }
        public static Collider2dList OverlapBox(Vector2 position, Vector2 size, int layerMask, float angle) {
            Collider2dList l;
            l._requestId = NextRequest();
            l._count = Physics2D.OverlapBoxNonAlloc(position, size, angle, _contacts, layerMask);
            return l;
        }
        public Collider2D this[int index] {
            get{
                if(_requestId != _currentRequest) throw new InvalidOperationException("This instance of " + typeof(ContactPoint2DList) + " is invalidated.");
                if(index >= _count) throw new IndexOutOfRangeException("Specified index must be smaller than Count.");
                return _contacts[index];
            }
        }
    }
    public struct ContactPoint2DList {
        private static int _currentRequest;
        private static int NextRequest() {
            return ++_currentRequest;
        }
        private static ContactPoint2D[] contacts = new ContactPoint2D[50];
        public ContactPoint2DList(Rigidbody2D rigidbody, in ContactFilter2D filter) { 
            _count = rigidbody.GetContacts(filter, ContactPoint2DList.contacts);
            _requestId = NextRequest();
        }
        private int _count;
        private int _requestId;
        public int Count{ get{ return _count; } }
        public ContactPoint2D[] UnsafeContacts{
            get{
                return contacts;
            }
        }
        public ContactPoint2D this[int index] {
            get{
                if(_requestId != _currentRequest) throw new InvalidOperationException("This instance of " + typeof(ContactPoint2DList) + " is invalidated.");
                if(index >= _count) throw new IndexOutOfRangeException("Specified index must be smaller than Count.");
                return contacts[index];
            }
        }
    }
    public struct RaycastHit2DList {
        private static int _currentRequest;
        private static int NextRequest() {
            return ++_currentRequest;
        }
        private static RaycastHit2D[] _contacts = new RaycastHit2D[50];
        private int _count;
        private int _requestId;
        public int Count { get{ return _count; } }
        public static RaycastHit2D[] UnsafeBuffer => _contacts;
        public RaycastHit2DList(int count) {
            _count = count;
            _requestId = NextRequest();
        }
        public RaycastHit2D this[int index] {
            get{
                if(_requestId != _currentRequest) throw new InvalidOperationException("This instance of " + typeof(ContactPoint2DList) + " is invalidated.");
                if(index >= _count) throw new IndexOutOfRangeException("Specified index must be smaller than Count.");
                return _contacts[index];
            }
        }

    }
}