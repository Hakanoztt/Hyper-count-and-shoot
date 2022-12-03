using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Fx
{
	public abstract class AElasticBandData
	{
		public abstract void Update(float deltaTime);
		public abstract void Snapback(bool isToAnchor);
		public abstract Vector2 EndPoint { get; set; }
		public abstract Vector2 StartPoint { get; set; }
		public abstract void PrepareBand(Vector3 anchorPos, Vector3 otherAnchorPos, ElasticBand.Settings settings);

		public Mass[] Masses { get; set; }
		public Spring[] Springs { get; set; }
		protected float SpringConstant { get; set; }
		protected float DragScale { get; set; }
		protected float GravityScale { get; set; }

		private Vector2[] _points;

		/// <summary>
		/// Gets the drag scale.
		/// </summary>
		/// <returns>The drag scale.</returns>
		/// <param name="dragValue">Drag value.</param>
		/// <remarks>dragValue parameter is arbitrarily capped between 0 and 10000</remarks>
		protected float GetDragScale(float dragValue)
		{
			float MaxDragVal = 10000f;
			// boundary check
			if (dragValue > MaxDragVal) 
				dragValue = MaxDragVal;
			else if (dragValue < 0) 
				dragValue = 0;
			// boundary check end
			return Mathf.Lerp(1f, 0.8f, dragValue / MaxDragVal);
		}

		/// <summary>
		/// Gets the all masses coordinates.
		/// </summary>
		/// <value>The points.</value>
		public Vector2[] Points {
			get {
				if (_points == null || _points.Length != Masses.Length)
					_points = new Vector2[Masses.Length];
				for (int i = 0; i < Masses.Length; i++)
					_points[i] = Masses[i].center;
				return _points;
			}
		}

		/// <summary>
		/// Gets the spring force.
		/// </summary>
		/// <returns>The spring force.</returns>
		/// <param name="i">The spring index.</param>
		public Vector2 GetSpringForce(int i)
		{
			int oneEnd = Springs[i].oneEnd;
			int otherEnd = Springs[i].otherEnd;
			float _springLength = (Masses[oneEnd].center - Masses[otherEnd].center).magnitude;
			float _forceScalar = Springs[i].springConstant * (Springs[i].restingLength - _springLength);
			var _springN = (Masses[oneEnd].center - Masses[otherEnd].center) / _springLength;
			return _forceScalar * _springN;
		}
	}

	#region Physics calc data structs
	public struct Spring
	{
		public readonly int oneEnd;
		public readonly int otherEnd;
		public readonly float restingLength;
		public readonly float springConstant;
		public Spring(int oneEnd, int otherEnd, float restingLength, float springConstant)
		{
			this.oneEnd = oneEnd;
			this.otherEnd = otherEnd;
			this.restingLength = restingLength;
			this.springConstant = springConstant;
		}
	}

	public struct Mass
	{
		public Vector2 center;
		public Vector2 velocity;
		public float mass;
		public Mass(Vector2 center, Vector2 velocity, float mass)
		{
			this.center = center;
			this.velocity = velocity;
			this.mass = mass;
		}
	}
	#endregion
}
