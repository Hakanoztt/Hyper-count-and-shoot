using UnityEngine;

namespace Mobge.Fx
{
	// Rubber band
	public class CircularElasticBandData : AElasticBandData
	{
		public override Vector2 StartPoint { get => Masses[0].center; set => Masses[0].center = value; }
		public override Vector2 EndPoint { get => Masses[0].center; set => Masses[0].center = value; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Mobge.Fx.CircularElasticBandData"/> class with first anchor defaulting to zero vector.
		/// </summary>
		/// <param name="anchorPoint">Anchor point.</param>
		/// <param name="mass">Mass.</param>
		/// <param name="numberOfSpring">Number of spring.</param>
		/// <param name="width">Width.</param>
		/// <param name="springConstant">Spring constant.</param>
		/// <param name="gravityScale">Gravity scale.</param>
		/// <param name="drag">Drag.</param>
		public CircularElasticBandData(Vector3 anchorPoint, float mass, int numberOfSpring, float width, float springConstant, float gravityScale, int drag)
		{
			PrepareBand(Vector3.zero, anchorPoint, mass, numberOfSpring, width, springConstant, gravityScale, drag);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Mobge.Fx.CircularElasticBandData"/> class with first anchor defaulting to zero vector.
		/// </summary>
		/// <param name="anchorPoint">Anchor point.</param>
		/// <param name="settings">Settings.</param>
		public CircularElasticBandData(Vector3 anchorPoint, ElasticBand.Settings settings) => PrepareBand(Vector3.zero, anchorPoint, settings);

		/// <summary>
		/// Prepares the band
		/// </summary>
		/// <param name="anchorPos">Anchor position.</param>
		/// <param name="otherAnchorPos">Other anchor position.</param>
		/// <param name="settings">Settings.</param>
		public override void PrepareBand(Vector3 anchorPos, Vector3 otherAnchorPos, ElasticBand.Settings settings)
		{
			PrepareBand(anchorPos, otherAnchorPos, settings.mass, settings.numberOfSpring, settings.width, settings.springConstant, settings.gravityScale, settings.drag);
		}

		/// <summary>
		/// Prepares the band
		/// </summary>
		/// <param name="anchorPos">1st Anchor point.</param>
		/// <param name="otherAnchorPos">2nd Anchor point.</param>
		/// <param name="mass">Mass.</param>
		/// <param name="numberOfSpring">Number of spring.</param>
		/// <param name="width">Width.</param>
		/// <param name="springConstant">Spring constant.</param>
		/// <param name="gravityScale">Gravity scale.</param>
		/// <param name="drag">Drag.</param>
		private void PrepareBand(Vector3 anchorPos, Vector3 otherAnchorPos, float mass, int numberOfSpring, float width, float springConstant, float gravityScale, float drag)
		{
			Masses = new Mass[numberOfSpring];
			for (int i = 0; i < numberOfSpring; i++) {
				float theta = (1.0f / numberOfSpring) * i * 2 * Mathf.PI;
				float x = ((0.5f + 0.25f * Mathf.Sin(theta)) * width);
				float y = ((0.5f + 0.25f * Mathf.Cos(theta)) * width);
				Masses[i] = new Mass(new Vector2(x, y), Vector2.one, 1.0f);
			}
			Springs = new Spring[(numberOfSpring * (numberOfSpring - 1)) / 2];
			int count = 0;
			for (int i = 0; i < numberOfSpring; i++) {
				for (int j = 0; j < numberOfSpring; j++) {
					if (i < j) {
						float dist = (Masses[i].center - Masses[j].center).magnitude;
						Springs[i - 1] = new Spring(i - 1, i, dist, springConstant);
						count++;
					}
				}
			}

			SpringConstant = springConstant;
			DragScale = GetDragScale(drag);
			GravityScale = gravityScale;
			StartPoint = otherAnchorPos;
			EndPoint = anchorPos;
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

		private Vector2[] CicularSmooth(Vector2[] points, int mult)
		{
			Vector2[] result = new Vector2[mult * points.Length];
			for (int i = 0; i < points.Length; i++) {
				for (int j = 0; j < mult; j++) {
					int idx = mult * i + j;
					float lambda = (float)j / mult;
					result[idx] = ((1.0f - lambda) * points[i]); // + (lambda * points[(i + 1) % points.Length]);
				}
			}
			for (int i = 0; i < 10; i++) {
				result = Smoother(result);
			}
			return result;
		}
		private Vector2[] Smoother(Vector2[] points)
		{
			Vector2[] result = new Vector2[points.Length];
			for (int i = 0; i < points.Length; i++) {
				result[i] = 0.1f * (points[(i + 9) % points.Length] +
									points[(i + 8) % points.Length] +
									points[(i + 7) % points.Length] +
									points[(i + 6) % points.Length] +
									points[(i + 5) % points.Length] +
									points[(i + 4) % points.Length] +
									points[(i + 3) % points.Length] +
									points[(i + 2) % points.Length] +
									points[(i + 1) % points.Length] +
									points[i]);
			}
			return result;
		}

		public override void Snapback(bool isToAnchor)
		{
			throw new System.NotImplementedException();
		}
	}
}