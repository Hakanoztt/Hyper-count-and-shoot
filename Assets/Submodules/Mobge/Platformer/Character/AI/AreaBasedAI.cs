
using System;
using UnityEngine;
using System.Collections.Generic;

namespace Mobge.Platformer.Character {
    public class AreaBasedAI : AControlModule
    {
        public AreaBasedAIData data;
        #if UNITY_EDITOR
        public bool printEditorLogs;
        #endif
        [SerializeField]
        [Layer]
        private int _triggerLayer;
        private ActiveSequence _activeSequence;
        private int _updateCount;
        private BoxCollider2D _trigger;
        private Character2D _character;
        private HashSet<Character2D> _targetCharacters = new HashSet<Character2D>();
        private List<Candidate> _tempCandidates = new List<Candidate>();
        private static List<Character2D> _tempRemoveList = new List<Character2D>();
        
        public override void Initialize(Character2D character) {
            _activeSequence.Reset(data, character);
            _character = character;
            if(enabled) {
                if(!_trigger) {
                    /*Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                    Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                    for(int i = 0; i < data.rules.Length; i++) {
                        var a = data.rules[i].area;
                        min = Vector2.Min(a.min, min);
                        max = Vector2.Max(a.max, max);
                    }
                    */
                    var t = gameObject.AddComponent<BoxCollider2D>();
                    t.transform.SetParent(transform, false);
                    t.isTrigger = true;
                    t.offset = data.detectArea.center;
                    t.size = data.detectArea.size;
                    t.gameObject.layer = _triggerLayer;
                    _trigger = t;
                }
            }
            else{
                if(_trigger) {
                    Destroy(_trigger);
                }
            }
        }

        public override void UpdateModule(Character2D character)
        {
            if(!_activeSequence.DoingRule && character.CurrentState == null) {
                TrySelectSequence(character);
            }
            
            UpdateModuleSequence(character);
        }
        protected void OnTriggerEnter2D(Collider2D col) {
            var c = Character2D.FromCollider(col);
            if(c && c.Alive && c.IsEnemy(_character)) {
                _targetCharacters.Add(c);
            }
        }
        public int CurrentRuleId {
            get{
                return _activeSequence.CurrentRule;
            }
        }
        private void InverseTransform(ref Bounds bounds) {
            var tr = transform;
            bounds.center += _character.CurrentVelocity * Time.deltaTime;
            var corner = bounds.center + bounds.extents;
            bounds.center = tr.InverseTransformPoint(bounds.center);
            var extends = tr.InverseTransformPoint(corner) - bounds.center;
            extends.x = Mathf.Abs(extends.x);
            extends.y = Mathf.Abs(extends.y);
            bounds.extents = extends;
        } 
        private void TrySelectSequence(Character2D thisChr)
        {
            if(_targetCharacters.Count == 0){
                #if UNITY_EDITOR
                if(printEditorLogs)
                    Debug.Log("return because no target ");
                #endif
                return;
            }

            var tr = transform;
            float weight = 0;
            
            foreach(var e in _targetCharacters) {
                if(e == null || !e.Alive) {
                    _tempRemoveList.Add(e);
                    continue;
                }
                var b = e.Bounds;
                InverseTransform(ref b);
                
                var rect = new Rect(b.center - b.extents, b.extents * 2);
                if(!rect.Overlaps(data.releaseArea)){
                    _tempRemoveList.Add(e);
                    continue;
                }
                for(int i = 0; i < data.rules.Length; i++){
                    var r = data.rules[i];
                    if(r.area.Overlaps(rect)) {
                        Candidate c;
                        weight += r.weight;
                        c.totalWeight = weight;
                        c.target = e;
                        c.rule = r;
                        c.ruleId = i;
                        _tempCandidates.Add(c);
                    }
                }
            }
            for(int i = 0; i < _tempRemoveList.Count; i++){
                _targetCharacters.Remove(_tempRemoveList[i]);
            }
            _tempRemoveList.Clear();
            if(_tempCandidates.Count > 0) {
                var w = UnityEngine.Random.Range(0f, weight);
                int sel = 0;
                Candidate c;
                do {
                    c = _tempCandidates[sel];
                    sel++;
                }
                while(c.totalWeight < w);
                _tempCandidates.Clear();
                _activeSequence.SetRule(thisChr, in c);
                #if UNITY_EDITOR
                if(printEditorLogs)
                    Debug.Log("setting rule: " + c.rule.area);
                #endif
                //Debug.Log(c.rule.sequence[0].type + " " + c.rule.sequence[0].actionParameter);
            }
        }

        private void UpdateModuleSequence(Character2D thisChr)
        {
            _activeSequence.Update(thisChr, this);
        }
        private struct Candidate {
            public AreaBasedAIData.Rule rule;
            public int ruleId;
            public float totalWeight;
            public Character2D target;
        }
        private struct ActiveSequence
        {
            private AreaBasedAIData.ActionSequence _actions;
            private int _ruleId;
            //private float _startTime;
            //private byte[] _states;
            private int _nextAction;
            private float _actionStartTime;
            public bool DoingRule {
                get => _ruleId >= 0;
            }
            public int CurrentRule => _ruleId;
            private Vector3 _targetPos;
            // AreaBasedAIData.Rule value, int ruleId, Character2D target
            public void SetRule(Character2D thisChr, in Candidate c) {
                
                if(c.rule != null) {
                    _actions = c.rule.actions;
                    _ruleId = c.ruleId;
                }
                //_startTime = Time.fixedTime;
                thisChr.Input.Target = c.target;
                NextAction = 0;
                UpdateTargetPos(thisChr);
            }
            private void SetToDefault(AreaBasedAIData data, Character2D thisChr) {
                _actions = data.defaultActions;
                _ruleId = -1;
                //_startTime = Time.fixedTime;
                NextAction = 0;
                thisChr.Input.Target = null;
                _targetPos = thisChr.Position;
                _targetPos.x = thisChr.Direction > 0 ? float.PositiveInfinity : float.NegativeInfinity;
            }
            private int NextAction{
                get => _nextAction;
                set{
                    _nextAction = value;
                    _actionStartTime = Time.fixedTime;
                }
            }
            private void UpdateTargetPos(Character2D thisCharacter) {
                var t = thisCharacter.Input.Target;
                if(t != null && t.Alive){
                    _targetPos = t.Position;
                }
                else{
                    thisCharacter.Input.Target = null;
                }
            }
            private bool UpdateAction(Character2D c, ref AreaBasedAIData.Action action, float elapsed, AreaBasedAI ai) {
                
                switch(action.type) {
                    default:
                    case AreaBasedAIData.ActionType.WalkInputX:
                        UpdateTargetPos(c);
                        float xInput;
                        float ap = action.ActionParameterI;
                        
                        if(_targetPos.x-c.Position.x > 0) {
                            xInput = ap;
                        }
                        else{
                            xInput = -ap;
                        }
                        c.Input.MoveInput = new Vector2(xInput, 0);
                        return false;
                    case AreaBasedAIData.ActionType.ActionPress:
                        c.Input.UpdateAction(action.ActionParameterI, true);
                        return false;
                    case AreaBasedAIData.ActionType.ActionRelease:
                        c.Input.UpdateAction(action.ActionParameterI, false);
                        return false;
                    case AreaBasedAIData.ActionType.JumpStart:
                        c.Input.Jump.Value = true;
                        return false;
                    case AreaBasedAIData.ActionType.JumpEnd:
                        c.Input.Jump.Value = false;
                        return false;
                    case AreaBasedAIData.ActionType.WaitTime:
                        return elapsed < action.ActionParameterF;
                    case AreaBasedAIData.ActionType.WaitForAction:
                        return c.CurrentState != null;
                    case AreaBasedAIData.ActionType.WaitWhileTargetInside:
                        if(_ruleId >= 0) {
                            var rule = ai.data.rules[_ruleId];
                            var t = c.Input.Target;
                            var b = t.Bounds;
                            ai.InverseTransform(ref b);
                            Rect r = new Rect(b.center-b.extents, b.extents*2);
                            var o = r.Overlaps(rule.area);
                            //Debug.Log("overlaps: " + o);
                            if(!o) {
                                return false;
                            }
                        }
                        return elapsed < action.ActionParameterF;
                }
            }
            public void Update(Character2D thisChr, AreaBasedAI ai) {
                if(_nextAction >= _actions.actions.Length) {
                    SetToDefault(ai.data, thisChr);
                }
                else{
                    if(!UpdateAction(thisChr, ref _actions.actions[_nextAction], Time.fixedTime - _actionStartTime, ai)) {
                        NextAction++;
                    }
                }
            }
            
            public void Reset(AreaBasedAIData data, Character2D character) {
                SetToDefault(data, character);
            }
        }
    }
}
