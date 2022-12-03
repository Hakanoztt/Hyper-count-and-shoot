using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core
{
	[CreateAssetMenu(menuName = "Mobge/Piece")]
	public class Piece : ScriptableObject
    {

		#region Fields and properties
#if UNITY_EDITOR
		public EditorGrid editorGrid;
		public SlotInfo[] outputSlotProperties = new SlotInfo[0];
		public SlotInfo[] inputSlotProperties = new SlotInfo[0];
#endif
		[SerializeField] private LevelComponentMap _components;
		public LevelComponentMap Components { get => _components; set => _components = value; }
		[SerializeField] [HideInInspector] private LogicConnections _connections;
		public LogicConnections Connections { get => _connections; }
#endregion
#region Data structures
		[Serializable]
		public class InnerConnections : BaseComponent
		{
			public const int ID = 400000000;
			public Piece piece;
			public override LogicConnections Connections { get => piece._connections; set => piece._connections = value; }
			public override void Start(in InitArgs initData) {}
#if UNITY_EDITOR
			public override void EditorInputs(List<LogicSlot> slots)
			{
				for (int i = 0; i < piece.outputSlotProperties.Length; i++) {
					SlotInfo si = piece.outputSlotProperties[i];
					si.outputSlot = i;
					slots.Add(new LogicSlot(si.name, si.outputSlot, si.CastSlot(si.type), si.CastSlot(si.returnType)));
				}
			}
			public override void EditorOutputs(List<LogicSlot> slots)
			{
				for (int i = 0; i < piece.inputSlotProperties.Length; i++) {
					SlotInfo si = piece.inputSlotProperties[i];
					si.outputSlot = i;
					slots.Add(new LogicSlot(si.name, si.outputSlot, si.CastSlot(si.type), si.CastSlot(si.returnType)));
				}
			}
#endif
		}
		[Serializable]
		public struct Atom
		{
			public RectInt rectangle;
			public int decorationID;
		}
		[Serializable]
		public struct RectInt
		{
			[SerializeField]
			private int m_XMin;
			[SerializeField]
			private int m_YMin;
			[SerializeField]
			private int m_Width;
			[SerializeField]
			private int m_Height;
			// Left coordinate of the rectangle.
			public int x { get { return m_XMin; } set { m_XMin = value; } }

			// Top coordinate of the rectangle.
			public int y { get { return m_YMin; } set { m_YMin = value; } }

			public RectInt(int xMin, int yMin, int width, int height)
			{
				m_XMin = xMin;
				m_YMin = yMin;
				m_Width = width;
				m_Height = height;
			}
			public RectInt(Vector2Int position, Vector2Int size)
			{
				m_XMin = position.x;
				m_YMin = position.y;
				m_Width = size.x;
				m_Height = size.y;
			}
			public Vector2 center { get { return new Vector2(x + m_Width / 2f, y + m_Height / 2f); } }
			public Vector2Int size { get { return new Vector2Int(m_Width, m_Height); } set { m_Width = value.x; m_Height = value.y; } }
			public Vector2Int position { get { return new Vector2Int(m_XMin, m_YMin); } set { m_XMin = value.x; m_YMin = value.y; } }
			public int xMin { get { return Math.Min(m_XMin, m_XMin + m_Width); } set { int oldxmax = xMax; m_XMin = value; m_Width = oldxmax - m_XMin; } }
			public int yMin { get { return Math.Min(m_YMin, m_YMin + m_Height); } set { int oldymax = yMax; m_YMin = value; m_Height = oldymax - m_YMin; } }
			public int xMax { get { return Math.Max(m_XMin, m_XMin + m_Width); } set { m_Width = value - m_XMin; } }
			public int yMax { get { return Math.Max(m_YMin, m_YMin + m_Height); } set { m_Height = value - m_YMin; } }
			// Top left corner of the rectangle.
			public Vector2Int min { get { return new Vector2Int(xMin, yMin); } set { xMin = value.x; yMin = value.y; } }

			// Bottom right corner of the rectangle.
			public Vector2Int max { get { return new Vector2Int(xMax, yMax); } set { xMax = value.x; yMax = value.y; } }

			public static explicit operator UnityEngine.RectInt(RectInt v)
			{
				return new UnityEngine.RectInt(v.position, v.size);
			}
			public static explicit operator RectInt(UnityEngine.RectInt v)
			{
				return new RectInt(v.position, v.size);
			}
		}
		[Serializable]
		public struct PieceRef
		{
			public Piece piece;
			public Vector3Int offset;
			public override string ToString()
			{
				if (piece == null) return "null";
				return piece.ToString();
			}
		}
		[Serializable]
		public class LevelComponentMap : AutoIndexedMap<LevelComponentData>, ISerializationCallbackReceiver
		{
			void ISerializationCallbackReceiver.OnAfterDeserialize() {

			}

			void ISerializationCallbackReceiver.OnBeforeSerialize() {
				// todo: test and uncomment
//#if !UNITY_EDITOR
//				var e = KeyEnumerator();
//				while (e.MoveNext()) {
//					var i = e.Current;
//					var val = this[i];
//					val.SetObject(val.GetObject());
//				}
//#endif
			}
		}
		[Serializable]
		public class SlotInfo
		{
			public string name;
			public SlotType type;
			public SlotType returnType;
			public int outputSlot;
			public SlotInfo()
			{
				type = SlotType.Void;
				returnType = SlotType.Void;
				outputSlot = 0;
			}
			public SlotInfo(SlotType type, string name)
			{
				this.type = type;
				this.name = name;
			}
			public SlotInfo(SlotType type, SlotType returnType, string name) : this(type, name)
			{
				this.returnType = returnType;
			}

			public override string ToString()
			{
				return name + "\t" + type.ToString() + " | " + returnType.ToString();
			}

			public Type CastSlot(SlotType st)
			{
				switch (st) {
					case SlotType.Void:        return null;
					case SlotType.Transform:   return typeof(Transform);
					case SlotType.RigidBody2D: return typeof(Rigidbody2D);
					case SlotType.RigidBody3D: return typeof(Rigidbody);
					case SlotType.Float:       return typeof(float);
					case SlotType.Collider:    return typeof(Collider2D);
					case SlotType.String:      return typeof(string);
					case SlotType.Vector3:     return typeof(Vector3);
					case SlotType.Bool:        return typeof(bool);
					case SlotType.Color:       return typeof(Color);
					default:                   return null;
				}
			}
		}
		public enum SlotType
		{
			Void = 0,
			Transform = 1,
			RigidBody2D = 2,
			RigidBody3D = 9,
			Float = 3,
			Collider = 4,
			String = 5,
			Vector3 = 6,
			Bool = 7,
			Color = 8,
		}
#endregion
        public Rect SelfRect {
            get {
                return new Rect();
                // Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
                // Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
                // for(int i = 0; i < atoms.Length; i++) {
                //     var p = atoms[i].rectangle;
                //     min = Vector2Int.Min(min, p.min);
                //     max = Vector2Int.Max(max, p.max);
                // }
                // return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
        }
        public Rect Rect {
            get {
                var r = SelfRect;
                return r;
            }
        }
    }
}
