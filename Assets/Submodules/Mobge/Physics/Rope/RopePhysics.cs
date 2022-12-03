using System;
using System.Collections;
using System.Collections.Generic;
using Mobge;
using UnityEngine;
using UnityEngine.Serialization;

public class RopePhysics : MonoBehaviour {
    
    [SerializeField] [HideInInspector] private GameObject[] linkObjects;
    [SerializeField] [HideInInspector] private Transform[] linkTransforms;
    [SerializeField] [HideInInspector] private Rigidbody2D[] linkBodies;
    [SerializeField] [HideInInspector] private CapsuleCollider2D[] linkCapsules;
    [SerializeField] [HideInInspector] private HingeJoint2D[] linkHinges;

    public bool useExternalTransforms = false;
    public int linkCount = 10;
    public float length = 2f;
    public float thickness = 1f;
    
    public void Construct(int linkCount) {
        if(useExternalTransforms) {
            Debug.LogError("RopePhysics is in external transform mode, cannot Construct without transform array");
            return;
        }
        transform.DestroyAllChildren();
        var pointPositions = new Vector2[linkCount + 1];
        for (int i = 0; i < linkCount + 1; i++) {
            pointPositions[i] = length * i * thickness * Vector2.right;
        }
        var transforms = new Transform[linkCount];
        for (int i = 0; i < linkCount; i++) {
            var go = new GameObject("piece " + i);
            var tr = go.transform;
            tr.SetParent(transform);
            transforms[i] = tr;
        }
        Construct(transforms);
        UpdatePoints(pointPositions, thickness);
    }
    
    public void Construct(Transform[] transforms) {
        if(useExternalTransforms) 
            transform.DestroyAllChildren();
        
        linkCount = transforms.Length;
        linkTransforms = transforms;
        linkObjects = new GameObject[linkCount];
        linkBodies = new Rigidbody2D[linkCount];
        linkCapsules = new CapsuleCollider2D[linkCount];
        linkHinges = new HingeJoint2D[linkCount - 1];

        for (int i = 0; i < linkCount; i++) {
            var go = linkTransforms[i].gameObject;
            linkObjects[i] = go;
            var capsule = go.GetComponent<CapsuleCollider2D>();
            if (capsule == null) capsule = go.AddComponent<CapsuleCollider2D>();
            linkCapsules[i] = capsule;
            var rigid = go.GetComponent<Rigidbody2D>();
            if (rigid == null) rigid = go.AddComponent<Rigidbody2D>();
            linkBodies[i] = rigid;
        }
        for (int i = 0; i < linkCount-1; i++) {
            var go = linkObjects[i];        
            var hinge = go.GetComponent<HingeJoint2D>();
            if (hinge == null) hinge = go.AddComponent<HingeJoint2D>();
            hinge.connectedBody = linkBodies[i+1];
            linkHinges[i] = hinge;
        }
    }
    
//    public void AddAnchor(Vector2 position, int connectionIndex, bool isStatic) {
//        GameObject go = new GameObject("anchors");
//        Transform tr = go.transform;
//        tr.SetParent(this.transform);
//        tr.localPosition = position;
//        _anchors.Add(go);
//        _anchors[i].localPosition = points[i];
//        _anchorBodies[i].isKinematic = pointIsStatic[i];
//        _anchorBodies[i].velocity = Vector2.zero;
//    }
    
    public void UpdatePoints(Vector2[] points, float thickness) {
        if (points == null || points.Length != linkCount + 1) {
            Debug.LogError("RopePhysics UpdatePoints cannot update point data if array size wont match");
            return;
        }
        if (thickness <= 0) {
            Debug.LogError("RopePhysics thickness cannot be less or equal than 0");
            return;
        }
        for (int i = 0; i < linkCount; i++) {
            linkTransforms[i].localPosition = points[i];
            linkTransforms[i].up = points[i + 1] - points[i];
            linkBodies[i].velocity = Vector2.zero;
            linkCapsules[i].offset = .5f * Vector2.Distance(points[i], points[i + 1]) * Vector2.up;
            linkCapsules[i].size = new Vector2(thickness, Vector2.Distance(points[i], points[i + 1]));
        }
        for (int i = 0; i < linkCount-1; i++) {
            linkHinges[i].anchor = Vector2.Distance(points[i], points[i + 1]) * Vector2.up;
        }
    }

    
}
