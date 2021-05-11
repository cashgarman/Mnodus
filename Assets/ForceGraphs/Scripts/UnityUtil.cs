using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
	public class UnityUtil
	{
		public static void Shuffle<T>(IEnumerable<T> list)
		{
			var enumerable = list as IList<T> ?? list.ToList();
			var n = enumerable.Count();
			while (n > 1)
			{
				n--;
				var k = Random.Range(0, n + 1);
				var value = enumerable[k];
				enumerable[k] = enumerable[n];
				enumerable[n] = value;
			}
		}

		// Credit: http://answers.unity3d.com/questions/461588/drawing-a-bounding-box-similar-to-box-collider.html
		public static void DrawDebugBox(Vector3 center, Vector3 extents, Color color)
		{
			var v3FrontTopLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z);  // Front top left corner
			var v3FrontTopRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z);  // Front top right corner
			var v3FrontBottomLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z);  // Front bottom left corner
			var v3FrontBottomRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z);  // Front bottom right corner
			var v3BackTopLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z);  // Back top left corner
			var v3BackTopRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z);  // Back top right corner
			var v3BackBottomLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z);  // Back bottom left corner
			var v3BackBottomRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z);  // Back bottom right corner

			Debug.DrawLine (v3FrontTopLeft, v3FrontTopRight, color);
			Debug.DrawLine (v3FrontTopRight, v3FrontBottomRight, color);
			Debug.DrawLine (v3FrontBottomRight, v3FrontBottomLeft, color);
			Debug.DrawLine (v3FrontBottomLeft, v3FrontTopLeft, color);
         
			Debug.DrawLine (v3BackTopLeft, v3BackTopRight, color);
			Debug.DrawLine (v3BackTopRight, v3BackBottomRight, color);
			Debug.DrawLine (v3BackBottomRight, v3BackBottomLeft, color);
			Debug.DrawLine (v3BackBottomLeft, v3BackTopLeft, color);
         
			Debug.DrawLine (v3FrontTopLeft, v3BackTopLeft, color);
			Debug.DrawLine (v3FrontTopRight, v3BackTopRight, color);
			Debug.DrawLine (v3FrontBottomRight, v3BackBottomRight, color);
			Debug.DrawLine (v3FrontBottomLeft, v3BackBottomLeft, color);
		}

		public static void DrawDebugPoint(Vector3 position, float size, Color color)
		{
			Debug.DrawLine(position, position + Vector3.up * size, color);
			Debug.DrawLine(position, position + Vector3.right * size, color);
			Debug.DrawLine(position, position + Vector3.down * size, color);
			Debug.DrawLine(position, position + Vector3.left * size, color);
			Debug.DrawLine(position, position + Vector3.forward * size, color);
			Debug.DrawLine(position, position + Vector3.back * size, color);
		}

		public static Vector3 MakeVectorComponentsUnitLength(Vector3 vector)
		{
			if (vector.x > 0) vector.x = 1;
			else vector.x = -1;

			if (vector.y > 0) vector.y = 1;
			else vector.y = -1;

			if (vector.z > 0) vector.z = 1;
			else vector.z = -1;

			return vector;
		}

		// Credit: http://answers.unity3d.com/questions/288594/how-do-i-make-rectcontains-accept-a-rect-with-nega.html
		public static Rect CreateRectFromPoints(Vector2 p1, Vector2 p2)
		{
			return new Rect
			{
				x = Mathf.Min(p1.x, p2.x),
				y = Mathf.Min(p1.y, p2.y),
				xMax = Mathf.Max(p1.x, p2.x),
				yMax = Mathf.Max(p1.y, p2.y)
			};
		}
	}

	public static class TransformExtensionMethods
	{
		public static void SetWorldScale(this Transform transform, Vector3 scale)
		{
			transform.localScale = scale;
			var ratio = new Vector3(transform.lossyScale.x / transform.localScale.x,
				transform.lossyScale.y / transform.localScale.y,
				transform.lossyScale.z / transform.localScale.z);
			transform.localScale = new Vector3(scale.x / ratio.x,
				scale.y / ratio.y,
				scale.z / ratio.z);
		}
	}
}