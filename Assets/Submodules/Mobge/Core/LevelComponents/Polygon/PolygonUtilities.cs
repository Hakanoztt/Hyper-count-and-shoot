using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
	public static class PolygonUtilities {
		private static List<Vector2> s_tempPoints = new List<Vector2>();
		/// <summary>
		/// Findout if the present controllers are sane to work with, if not prepares the correct controller array.
		/// </summary>
		/// <returns>Correct controller array</returns>
		/// <param name="obj">Root object of the polygon renderers.</param>
		/// <param name="polygons">Polygon datas</param>
		/// <param name="GenerateFunction">Controller generator function</param>
		/// <typeparam name="T">Controller type</typeparam>
		public static T[] GetControllersWithSanityCheckTryFix<T>(IPolygonRenderer obj, in Polygon[] polygons, Func<Transform, T> GenerateFunction) {
			T[] _controllers = obj.Transform.GetComponentsInChildren<T>();
			int neededNumberOfControllers = polygons.Length - _controllers.Length;
			switch (neededNumberOfControllers) {
				// Polygon added
				case int n when neededNumberOfControllers > 0:
					List<T> tmp = new List<T>(_controllers);
					for (int i = 0; i < neededNumberOfControllers; i++)
						tmp.Add(GenerateFunction.Invoke(obj.Transform));
					return tmp.ToArray();
				// Number of polygons did not change, Controller array is sane.
				default:
					return _controllers;
				// Polygon removed
				case int n when neededNumberOfControllers < 0:
					obj.Transform.DestroyAllChildren();
					tmp = new List<T>();
					for (int i = 0; i < polygons.Length; i++)
						tmp.Add(GenerateFunction.Invoke(obj.Transform));
					return tmp.ToArray();
			}
		}

		private static PolygonCollider2D InternalAddCollider(GameObject target, Polygon polygon, Quaternion rotation, bool hasRotation, float thicknessForNoFill) {
			var col = target.AddComponent<PolygonCollider2D>();
			s_tempPoints.Clear();
			if (polygon.noFill) {
				var cs = polygon.corners;
				if (cs != null && cs.Length >= 2) {
					LineToPolygon(cs, thicknessForNoFill, s_tempPoints);
				}
			}
			else {
				s_tempPoints.Clear();
				polygon.AddPositions2List(s_tempPoints, true);
			}
			if (s_tempPoints.Count > 0) {
				if (hasRotation) {
					for(int i = 0; i < s_tempPoints.Count; i++) {
						s_tempPoints[i] = rotation * s_tempPoints[i];
					}
				}

				col.SetPath(0, s_tempPoints);
			}
			return col;
		}
		public static PolygonCollider2D AddCollider(GameObject target, Polygon polygon, Quaternion rotation, float thicknessForNoFill = 0) {

			return InternalAddCollider(target, polygon, rotation, true, thicknessForNoFill);
		}
		public static PolygonCollider2D AddCollider(GameObject target, Polygon polygon, float thicknessForNoFill = 0) {
			return InternalAddCollider(target, polygon, Quaternion.identity, false, thicknessForNoFill);
		}

		public static void SetCount<T>(List<T> list, int newCount) {
			if(list.Count > newCount) {
				list.RemoveRange(newCount, list.Count - newCount);
			}
			else {
				var def = default(T);
				while(list.Count < newCount) {
					list.Add(def);
				}
			}
		}
		public static void LineToPolygon(Corner[] cs, float thickness, List<Vector2> output) {
			float ht = thickness * 0.5f;
			//Vector2[] corners = new Vector2[cs.Length * 2];
			SetCount(output, cs.Length * 2);
			int lastI = output.Count - 1;
			var pos = cs[0].position;
			var dir = (cs[1].position - pos).normalized;
			var normal = new Vector2(-dir.y, dir.x);
			output[0] = pos + normal * ht;
			output[lastI] = pos + normal * -ht;
			int i;
			for (i = 1; i < cs.Length - 1; i++) {
				pos = cs[i].position;
				Vector2 newDir = (cs[i + 1].position - pos).normalized;
				Vector2 midNormal = (newDir + dir);
				midNormal = new Vector2(-midNormal.y, midNormal.x);
				if (midNormal.x == 0 && midNormal.y == 0) {
					midNormal = new Vector2(-newDir.y, newDir.x);
				}
				else {
					midNormal.Normalize();
				}
				output[i] = pos + midNormal * ht;
				output[lastI - i] = pos + midNormal * -ht;
				dir = newDir;
			}
			pos = cs[i].position;
			normal = new Vector2(-dir.y, dir.x);
			output[i] = pos + normal * ht;
			output[lastI - i] = pos + normal * -ht;
		}

		private static readonly BezierPath3D BezierCache = new BezierPath3D();
		public static Corner[] Subdivide(Corner[] oldCorners, int subdivisionCount, float cubicness) {
			BezierCache.Points.SetCount(oldCorners.Length);
			for (int i = 0; i < oldCorners.Length; i++) {
				var c = oldCorners[i];
				BezierCache.Points.array[i].position = c.position;
			}
			BezierCache.controlMode = BezierPath3D.ControlMode.Automatic;
			BezierCache.closed = true;
			BezierCache.autoControlLength = cubicness;
			BezierCache.UpdateControlsForAuto();
			var newCornerList = new List<Corner>();
			var e = BezierCache.GetEnumerator(1f / subdivisionCount);
			while (e.MoveForwardByPercent(1f / subdivisionCount)) {
				var s = e.CurrentPoint;
				newCornerList.Add(new Corner(s));
			}
			return newCornerList.ToArray();
		}
		public static Polygon[] GetSubdividedPolygon(Polygon[] input, int subdivisionCount, float cubicness) {
			if (subdivisionCount <= 1) return input;
			var newPolygons = new Polygon[input.Length];
			for (int i = 0; i < input.Length; i++) {
				Polygon p;
				var ip = input[i];
				p.noCollider = ip.noCollider;
				p.noFill = ip.noFill;
				p.openShape = ip.openShape;
				p.skinScale = ip.skinScale;
				p.corners = Subdivide(ip.corners, subdivisionCount, cubicness);
				p.height = ip.height;
				newPolygons[i] = p;
			}
			return newPolygons;
		}
	}
}