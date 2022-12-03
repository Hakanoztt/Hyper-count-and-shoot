using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    public partial class ElementEditor {
        private class LogicEditor {
            private EditorTools _tools;
            private ElementEditor _editor;
            private SlotInfo _startSlot;
            private HashSet<AEditableElement> _logicalElements = new HashSet<AEditableElement>();
            private List<ConnectionInfo> _connections = new List<ConnectionInfo>();
            private int _deleteCandidate;

			private int _pressedDeleteConnection;
            private bool _deletePressed;
            private SlotInfo _currentSlot;
            

			public LogicEditor(ElementEditor editor) {
                InitTools();
                _editor = editor;
            }

            private void InitTools()
            {
                _tools = new EditorTools();
                _tools.AddTool(new EditorTools.Tool("drag connection") {
                    activation = new EditorTools.ActivationRule() {
                        mouseButton = 0,
                    },
                    onPress = DragStart,
                    onUpdate = DragUpdate,
                    onRelease = DragEnd,
                });
                _tools.AddTool(new EditorTools.Tool("delete connection") {
                    activation = new EditorTools.ActivationRule() {
                        mouseButton = 0,
                    },
                    onPress = DeleteStart,
                    onRelease = DeleteEnd,
                });
            }

            private bool DragStart()
            {
                _startSlot = _currentSlot;
                if(_startSlot.element != null) {
                    return true;
                }
                return false;
            }
            private SlotInfo FindSlot() {
                var e = _logicalElements.GetEnumerator();
                var ray = _editor._visualHandler.MouseRay;
                while(e.MoveNext()) {
                    var element = e.Current;
                    var slot = element.inputSlots.FindSlot(ray);
                    if(slot._slot.name != null) {
                        return new SlotInfo(slot, true, element);
                    }
                    slot = element.outputSlots.FindSlot(ray);
                    if(slot._slot.name != null) {
                        return new SlotInfo(slot, false, element);
                    }
                }
                return new SlotInfo();
            }
            private bool CanConnect(SlotInfo slot1, SlotInfo slot2, out SlotInfo input, out SlotInfo output) {
                
                if (slot1.isInput == slot2.isInput) {
                    input = default;
                    output = default;
                    return false;
                }
                if (slot1.isInput) {
                    input = slot1;
                    output = slot2;
                }
                else {
                    input = slot2;
                    output = slot1;
                }
                if ((Event.current.modifiers & EventModifiers.Shift) != 0) {
                    return true;
                }
                if (input.slot._slot.parameter != null) {
                    if (!input.slot._slot.parameter.IsAssignableFrom(output.slot._slot.parameter)) {
                        return false;
                    }
                }
                if (output.slot._slot.returnType != null) {
                    if (!output.slot._slot.returnType.IsAssignableFrom(input.slot._slot.returnType)) {
                        return false;
                    }
                }
                return true;
            }


            private void DragEnd()
            {
                if (_currentSlot.element != null) {
                    if(CanConnect(_currentSlot, _startSlot, out SlotInfo input, out SlotInfo output)) {
                        LogicConnection lc;
                        lc.input = input.slot._slot.id;
                        lc.output = output.slot._slot.id;
                        lc.target = input.element.Id;
                        bool result = AddConnection(output.element.LogicComponent, lc);
                        if (result) {
                            output.element.UpdateData();
                        }
                    }
                }

            }
            private bool DeleteStart() {
                if(_deleteCandidate >= 0) {
                    _deletePressed = true;
                    _pressedDeleteConnection = _deleteCandidate;
                    return true;
                }
                return false;
            }
            private void DeleteEnd() {
                _deletePressed = false;
                if(_deleteCandidate >= 0) {
                    var ci = _connections[_deleteCandidate];
                    LogicConnection lc;
                    lc.target = ci.inputId;
                    lc.output = ci.output._slot.id;
                    lc.input = ci.input._slot.id;
                    ci.sourceElement.LogicComponent.Connections.RemoveConnection(lc);
                    ci.sourceElement.UpdateData();
                }
            }
            private bool AddConnection(ILogicComponent comp, LogicConnection connection) {
                if(comp.Connections == null) {
                    comp.Connections = new LogicConnections();
                }
                return comp.Connections.AddConnection(connection);
            }
            private void DragUpdate()
            {
                var mpos = _editor._visualHandler.MousePosition;
                var position = _startSlot.slot.visual.position;
                bool highlighted = _currentSlot.element != null && CanConnect(_currentSlot, _startSlot, out _, out _);

                if(_startSlot.isInput) {
                    DrawBezier(position, _startSlot.slot.axis, mpos, _startSlot.slot.axis, highlighted);
                }
                else {
                    DrawBezier(mpos, _startSlot.slot.axis, position, _startSlot.slot.axis, highlighted);
                }
                _editor.Repaint();
            }
            private void DrawBezier(Vector3 input, in Axis inAxis, Vector3 output, in Axis outAxis, bool highlighted) {
                if (highlighted) {
                    _editor._visualHandler.DrawHihlightedBezier(input, inAxis, output, outAxis);
                }
                else {
                    _editor._visualHandler.DrawBezier(input, inAxis, output, outAxis);
                }
            }
            public void PreVisualSceneGUI() {
                if(!Enabled) {
                    return;
                }
                _logicalElements.Clear();
				//todo changed
				for (int i = 0; i < _editor._elements.Count; i++) {
                    var e = _editor._elements[i];
                    var lc = e.LogicComponent;
                    if(lc == null) {
                        continue;
                    }
                    var selected = _editor._selection.Contains(e);
                    if(selected) {
                        e.UpdateSlots();
                        _logicalElements.Add(e);
					} else {
						e.ClearSlots();
						var targets = FindAllConnectionTargets(lc);

						while (targets.MoveNext()) {
							var target = targets.Current;
							if (_editor._selection.Contains(target)) {
								e.UpdateSlots();
								_logicalElements.Add(e);
								break;
							}
						}
					}
				}
                var selection = _editor._selection.GetEnumerator();
                while (selection.MoveNext()) {
                    var current = selection.Current;
                    var lc = current.LogicComponent;
                    if (lc != null) {
                        var targets = FindAllConnectionTargets(lc);
                        while (targets.MoveNext()) {
                            var target = targets.Current;
                            if (target.inputSlots.Count == 0) {
                                target.UpdateSlots();
                                _logicalElements.Add(target);
                            }
                        }
                    }
                }

            }
            private void UpdateDeleteCandidate() {
                var ray = _editor._visualHandler.MouseRay;
                float minDistance = float.PositiveInfinity;
                _deleteCandidate = -1;
                for(int i = 0; i < _connections.Count; i++) {
                    if(_connections[i].visual.deleteButton.Contains(ray, out float distanceSqr) && minDistance > distanceSqr) {
                        minDistance = distanceSqr;
                        _deleteCandidate = i;
                    }
					
				}
            }
            private void PrepareConnections() {
                UpdateVisualConnections();
                if (_deletePressed) {
                    Ray ray = _editor._visualHandler.MouseRay;
                    bool deleteEnabled = _connections[_pressedDeleteConnection].visual.deleteButton.Contains(ray);
                    if (deleteEnabled) {
                        _deleteCandidate = _pressedDeleteConnection;
                    }
                    else {
                        _deleteCandidate = -1;
                    }
                }
                else {
                    UpdateDeleteCandidate();
                }
            }
            private void DrawConnections() {
                var s = FindSlot();
                if (s.Equals(_currentSlot)) {
                    _editor.Repaint();
                }
                _currentSlot = s;
                if (_currentSlot.element != null) {
                    _editor._visualHandler.DrawSlotLabel(_currentSlot.slot.visual.position, _currentSlot.slot._slot.ToString());
                }

                for (int i = 0; i < _connections.Count; i++) {
                    var c = _connections[i];
                    bool isHighlighted = false;
                    if (_currentSlot.element != null) {
                        if (_currentSlot.isInput) {
                            isHighlighted = c.inputId == _currentSlot.element.Id && _currentSlot.slot._slot.id == c.input._slot.id;
                        }
                        else {
                            isHighlighted = _currentSlot.element == c.sourceElement && _currentSlot.slot._slot.id == c.output._slot.id;
                        }
                    }
                    _editor._visualHandler.DrawConection(c.visual, i == _deleteCandidate || isHighlighted);
                }
            }
            public void PostVisualSceneGUI()
			{
				if (!Enabled) {
					return;
                }
                PrepareConnections();
                DrawConnections();

                _tools.OnSceneGUI();
			}
            private IEnumerator<AEditableElement> FindAllConnectionTargets(ILogicComponent component) {
                var cons = component.Connections;
                if (cons != null) {
                    var connections = cons.List;
                    for (int i = 0; i < connections.Count; i++) {
                        var c = connections[i];
                        if (_editor._elements.TryGet(c.target, out AEditableElement target)) {
                            yield return target;
                        }
                    }
                }
            }

            private void UpdateVisualConnections() {
                _connections.Clear();
                ConnectionInfo ci;
                var logicalElements = _logicalElements.GetEnumerator();
                while(logicalElements.MoveNext()) {
					var element = logicalElements.Current;
                    ci.sourceElement = element;
                    if(ci.sourceElement != null) {
                        var cons = ci.sourceElement.LogicComponent.Connections;
                        if(cons != null) {
                            var connections = cons.List;
                            for(int i = 0; i < connections.Count; i++) {
                                var c = connections[i];
                                if(_editor._elements.TryGet(c.target, out AEditableElement target) && 
                                target.inputSlots.TryFindSlot(c.input, out ci.input) && 
                                element.outputSlots.TryFindSlot(c.output, out ci.output)) {
                                    ci.visual = _editor._visualHandler.GetConnectionBezier(ci.input.visual.position, ci.input.axis,
                                        ci.output.visual.position, ci.output.axis, new Color(1f,1f,0.5f), i);
                                    ci.inputId = c.target;
                                    _connections.Add(ci);
                                }
                            }
                        }
                    }
				}
			}

            public bool FixAllConnections() {
                for(int i = 0; i < _editor._elements.Count; i++) {
                    var element = _editor._elements[i];
                    element.UpdateSlots();
                }
                bool deletedAny = false;
                for(int i = 0; i < _editor._elements.Count; i++) {
                    var element = _editor._elements[i];
                    var component = element.LogicComponent;
                    if(component != null) {
                        var cons = component.Connections;
                        if(cons != null) {
                            var connections = cons.List;
                            bool deleted = false;
                            for(int j = 0; j < connections.Count; ) {
                                var c = connections[j];
                                if(_editor._elements.TryGet(c.target, out AEditableElement target) && 
                                target.inputSlots.TryFindSlot(c.input, out AEditableElement.Slot input) && 
                                element.outputSlots.TryFindSlot(c.output, out AEditableElement.Slot output)) {
                                    j++;
                                }
                                else {
                                    deleted = true;
                                    deletedAny = true;
                                    connections.RemoveAt(j);
                                }
                            }
                            if(deleted) {
                                element.UpdateData();
                            }
                        }
                    }
                }
                return deletedAny;
            }
            public bool Enabled {
                get => _editor._selection.LogicMode;
                set => _editor._selection.LogicMode = value;
            }
            private struct SlotInfo {
                public SlotInfo(AEditableElement.Slot slot, bool isInput, AEditableElement element) {
                    this.slot = slot;
                    this.isInput = isInput;
                    this.element = element;
                }
                public AEditableElement.Slot slot;
                public bool isInput;
                public AEditableElement element;
            }
            private struct ConnectionInfo {
                public AEditableElement.Slot input;
                public AEditableElement.Slot output;
                public AEditableElement sourceElement;
                public int inputId;
                public ConnectionBezier visual;
                
            }
        }
        public struct Sphere {
            
            public Vector3 position;
            public float radius;
            public bool Contains(in Ray ray, out float distanceSqr) {
                var p1 = ray.origin;
                var p2 = ray.direction + ray.origin;
                distanceSqr = Vector3.Cross(position - p1, position - p2).sqrMagnitude;
                return distanceSqr < radius * radius;
            }
            public bool Contains(in Ray ray) {
                return Contains(ray, out float d);
            }
        }
        public struct Bezier {
            public Vector3 point1, point2, tangent1, tangent2;
			public Vector3[] generatedBezierPoints;
			public Quality quality;
			public enum Quality { Low = 5, Mid = 10, High = 20 }

		}
        public struct ConnectionBezier {
            public Bezier connection;
            public Sphere deleteButton;
			public Color color;
            public int index;
		}
	}
}