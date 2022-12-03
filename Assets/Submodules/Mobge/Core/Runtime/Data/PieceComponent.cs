using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core {
	public class PieceComponent : ComponentDefinition<PieceComponent.Data> {
		[Serializable]
		public class Data : BaseComponent, IRotationOwner {
			public Piece piece;
			private Dictionary<int, BaseComponent> _components;
			[SerializeField, HideInInspector] private LogicConnections _connections;
			public override LogicConnections Connections { get => _connections; set => _connections = value; }
			public Quaternion rotation = Quaternion.identity;
			Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }

			private InputListener _listener;
			#region Overrides
			public override void Load(in LevelPlayer.LoadArgs loadArgs) {
				var runtime = loadArgs.levelPlayer;
				var operationGroup = loadArgs.operationGroup;
				_components = runtime.LoadComponents(operationGroup, piece);
			}
			public override void Start(in InitArgs initData) {
				_listener = new InputListener(initData, _connections);
				_components.Add(Piece.InnerConnections.ID, _listener);
				initData.player.InitComponents(piece, position, rotation, _components);
			}
#if UNITY_EDITOR
			public override void EditorInputs(List<LogicSlot> slots) {
				if (piece == null) return;
				for (int i = 0; i < piece.inputSlotProperties.Length; i++) {
					Piece.SlotInfo si = piece.inputSlotProperties[i];
					si.outputSlot = i;
					slots.Add(new LogicSlot(si.name, si.outputSlot, si.CastSlot(si.type), si.CastSlot(si.returnType)));
				}
			}
			public override void EditorOutputs(List<LogicSlot> slots) {
				if (piece == null) return;
				for (int i = 0; i < piece.outputSlotProperties.Length; i++) {
					Piece.SlotInfo si = piece.outputSlotProperties[i];
					si.outputSlot = i;
					slots.Add(new LogicSlot(si.name, si.outputSlot, si.CastSlot(si.type), si.CastSlot(si.returnType)));
				}
			}
#endif
			public override object HandleInput(ILogicComponent sender, int index, object input)
			{
				return piece.Connections.InvokeSimple(sender, index, input, _components);
			}
		}
		#endregion
		[Serializable]
		public class InputListener : BaseComponent {
			public Dictionary<int, BaseComponent> Components { get; private set; }
			private LogicConnections _con;
			public InputListener(in InitArgs initData, LogicConnections connections) {
				Components = initData.components;
				_con = connections;
			}
			public override LogicConnections Connections { get => _con; set => _con = value; }
			public override void Start(in InitArgs initData) { }
			public override object HandleInput(ILogicComponent sender, int index, object input) {
				if (_con == null) return null;
				return _con.InvokeSimple(sender, index, input, Components);
			}
			public LogicConnections.InvokeEnumerator<BaseComponent> Invoke(ILogicComponent sender, int index, object input) {
				return _con.Invoke(sender, index, input, Components);
			}
		}
	}
}