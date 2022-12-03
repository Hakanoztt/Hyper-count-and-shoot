using UnityEngine;

namespace Mobge.Fx
{
	// Rubber string
	public class LinearElasticBandData : AElasticBandData
	{
		public override Vector2 EndPoint { get => Masses[0].center; set => Masses[0].center = value; }
		public override Vector2 StartPoint { get => Masses[Masses.Length - 1].center; set => Masses[Masses.Length - 1].center = value; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Mobge.Fx.LinearElasticBandData"/> class with first anchor defaulting to zero vector.
		/// </summary>
		/// <param name="anchorPoint">Anchor point.</param>
		/// <param name="mass">Mass.</param>
		/// <param name="numberOfSpring">Number of spring.</param>
		/// <param name="width">Width.</param>
		/// <param name="springConstant">Spring constant.</param>
		/// <param name="gravityScale">Gravity scale.</param>
		/// <param name="drag">Drag.</param>
		public LinearElasticBandData(Vector3 anchorPoint, float mass, int numberOfSpring, float width, float springConstant, float gravityScale, float drag)
		{
			PrepareBand(Vector3.zero, anchorPoint, mass, numberOfSpring, width, springConstant, gravityScale, drag);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Mobge.Fx.LinearElasticBandData"/> class with first anchor defaulting to zero vector.
		/// </summary>
		/// <param name="anchorPoint">Anchor point.</param>
		/// <param name="settings">Settings.</param>
		public LinearElasticBandData(Vector3 anchorPoint, ElasticBand.Settings settings) => PrepareBand(Vector3.zero, anchorPoint, settings);

		/// <summary>
		/// Prepares the band.
		/// </summary>
		/// <param name="anchorPos">Anchor position.</param>
		/// <param name="otherAnchorPos">Other anchor position.</param>
		/// <param name="settings">Settings.</param>
		public override void PrepareBand(Vector3 anchorPos, Vector3 otherAnchorPos, ElasticBand.Settings settings)
		{
			PrepareBand(anchorPos, otherAnchorPos, settings.mass, settings.numberOfSpring, settings.width, settings.springConstant, settings.gravityScale, settings.drag);
		}

		/// <summary>
		/// Prepares the band.
		/// </summary>
		/// <param name="anchorPos">Anchor position.</param>
		/// <param name="otherAnchorPos">Other anchor position.</param>
		/// <param name="mass">Mass.</param>
		/// <param name="numberOfSpring">Number of spring.</param>
		/// <param name="width">Width.</param>
		/// <param name="springConstant">Spring constant.</param>
		/// <param name="gravityScale">Gravity scale.</param>
		/// <param name="drag">Drag.</param>
		private void PrepareBand(Vector3 anchorPos, Vector3 otherAnchorPos, float mass, int numberOfSpring, float width, float springConstant, float gravityScale, float drag)
		{
			float _stepSize = width / numberOfSpring;
			Masses = new Mass[numberOfSpring];
			for (int i = 0; i < numberOfSpring; i++) {
				Masses[i] = new Mass(new Vector2(i * _stepSize, 0), Vector2.zero, 1.0f);
			}
			Springs = new Spring[numberOfSpring - 1];
			for (int i = 1; i < numberOfSpring; i++) {
				float dist = (Masses[i - 1].center - Masses[i].center).magnitude;
				Springs[i - 1] = new Spring(i - 1, i, dist, springConstant);
			}

			SpringConstant = springConstant; 
			DragScale = GetDragScale(drag);
			GravityScale = gravityScale;
			StartPoint = anchorPos;
			EndPoint = otherAnchorPos;
		}

		/// <summary>
		/// Updates the band data according to specified delta time.
		/// </summary>
		/// <param name="deltaTime">Delta time.</param>
		public override void Update(float deltaTime)
		{
			// Update positions
			for (int i = 1; i < Masses.Length - 1; i++) {
				Masses[i].center += (deltaTime * (Vector2)Physics.gravity) * GravityScale;
				Masses[i].center += Masses[i].velocity * deltaTime;
			}

			// Update velocities
			for (int i = 0; i < Springs.Length; i++) {
				Vector2 N = Masses[Springs[i].oneEnd].center;
				N /= N.magnitude;
				float parameter1 = 0.1f;
				float parameter2 = 0.01f;
				Masses[Springs[i].oneEnd].velocity += (GetSpringForce(i) * deltaTime / Masses[Springs[i].oneEnd].mass) 
					- (parameter1 * N * deltaTime) 
					- (parameter2 * Masses[Springs[i].oneEnd].velocity * deltaTime);
				Masses[Springs[i].oneEnd].velocity *= DragScale;

				N = Masses[Springs[i].otherEnd].center;
				N /= N.magnitude;
				Masses[Springs[i].otherEnd].velocity -= (GetSpringForce(i) * deltaTime / Masses[Springs[i].oneEnd].mass) 
					+ (parameter1 * N * deltaTime) 
					+ (parameter2 * Masses[Springs[i].otherEnd].velocity * deltaTime);
				Masses[Springs[i].otherEnd].velocity *= DragScale;
			}
		}

		/// <summary>
		/// Snap the band to either first anchor or the second anchor.
		/// </summary>
		/// <param name="isToFirstAnchor">If set to <c>true</c> snap to first anchor. <c>false</c> to second anchor</param>
		public override void Snapback(bool isToFirstAnchor)
		{
			if (isToFirstAnchor) {
				for (int i = 0; i < Masses.Length; i++) {
					Masses[i].center = StartPoint;
				}
			} else {
				for (int i = 0; i < Masses.Length; i++) {
					Masses[i].center = EndPoint;
				}
			}
		}
	}
}