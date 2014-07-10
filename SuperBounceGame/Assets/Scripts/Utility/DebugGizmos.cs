using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public static class DebugGizmos
{
	public class TimedGizmo
	{
		public float Timeout = 1f;
		public Action<float> GizmoAction;

		float currentTime = 0f;
		public float CurrentTime
		{
			get {
				return currentTime;
			}
			set {
				currentTime = value;
			}
		}

		public TimedGizmo(float timeout, Action<float> gizmoAction)
		{
			this.Timeout = timeout;
			this.GizmoAction = gizmoAction;
		}
	}

	public static List<TimedGizmo> GizmoList = new List<TimedGizmo>();

	public static void DrawSphere(Vector3 position, float radius, Color color, float timeout = 1f)
	{
		GizmoList.Add(new TimedGizmo(timeout, (alpha) => {
			Gizmos.color = GetGizmoColor(color, alpha);
			Gizmos.DrawSphere(position, radius);
		}));
	}

	public static void DrawWireSphere(Vector3 position, float radius, Color color, float timeout = 1f)
	{
		GizmoList.Add(new TimedGizmo(timeout, (alpha) => {
			Gizmos.color = GetGizmoColor(color, alpha);
			Gizmos.DrawWireSphere(position, radius);
		}));
	}

	// A quick helper method
	static Color GetGizmoColor(Color color, float alpha)
	{
		return new Color(color.r, color.g, color.b, color.a * alpha);
	}

	public static void Update(float deltaTime)
	{
		for(int i = 0; i < GizmoList.Count; i++)
		{
			var tGizmo = GizmoList[i];

			tGizmo.CurrentTime += deltaTime;

			// Remove any that have expired
			if(tGizmo.CurrentTime > tGizmo.Timeout)
				GizmoList.Remove(tGizmo);
		}
	}
}
