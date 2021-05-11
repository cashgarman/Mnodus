using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	public class GLRenderer : MonoBehaviour
	{
		public struct Line
		{
			public Vector3[] verts;
			public Material material;
		}

		public static GLRenderer instance;

		public List<Line> lines = new List<Line>();

		public void Awake()
		{
			if (instance == null)
				instance = this;
		}

		public void OnPostRender()
		{
			GL.PushMatrix();
			GL.LoadProjectionMatrix(Camera.main.projectionMatrix);
			GL.MultMatrix(Matrix4x4.TRS(new Vector3(0, 0, (Camera.main.transform.position - transform.position).magnitude), Quaternion.Inverse(Camera.main.transform.rotation), Vector3.one));

			foreach(var line in lines)
			{
				line.material.SetPass(0);

				GL.Begin(GL.LINES);
				GL.Color(new Color(1, 1, 1, 1));
				for (var i = 0; i < line.verts.Length - 1; ++i)
				{
					var vert = line.verts[i];
					var nextVert = line.verts[i + 1];

					if (i == 0)
					{
						GL.Vertex(vert);
						GL.Vertex(nextVert);
					}
					else
					{
						var prevVert = line.verts[i - 1];

						if (i == line.verts.Length - 2)
						{
							GL.Vertex(prevVert);
							GL.Vertex(vert);
						}
						else
						{
							// Make the line continuous
							GL.Vertex(prevVert);
							GL.Vertex(vert);
							GL.Vertex(vert);
							GL.Vertex(nextVert);
						}
					}
				}
				GL.End();
			}

			GL.PopMatrix();
		}

		public void AddLine(Vector3[] verts, Material material)
		{
			lines.Add(new Line { verts = verts, material = material });
		}
	}
}
