using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class DebugGizmosDraw : MonoBehaviour {
	
	public float fadeOutTime = 0.5f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		DebugGizmos.Update(Time.deltaTime);
	}

	void OnDrawGizmos()
	{

		foreach(DebugGizmos.TimedGizmo tGizmo in DebugGizmos.GizmoList)
		{
			float timeLeft = tGizmo.Timeout-tGizmo.CurrentTime;
			float alpha = timeLeft <= this.fadeOutTime ? timeLeft/this.fadeOutTime : 1f;

			tGizmo.GizmoAction(alpha);
		}
	}
}
