using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EdgeDetection : MonoBehaviour {


	public GameObject point;
	public GameObject meshGameObject;

	// Use this for initialization
	void Start ()
	{
		/* * /
		for(int i = -15; i < 16; i++)
		{
			Debug.Log(i + ": " + UtilityMethods.mod(i, 3));
		}
		/* */
	}
	
	// Update is called once per frame
	void Update () {

	}

	



	/// <summary>
	/// Finds all vertices that are "Approximately" in the same spot, that are closest to the point.
	/// </summary>
	/// <param name="go">The gameobject with a MeshFilter with Mesh</param>
	/// <param name="point">The point to find nearet to</param>
	/// <returns>Array of <see cref="IndexPointPair" /> closest to the point</returns>
	public static Dictionary<MeshFilter, List<IndexPointPair>> NearestVerticesTo(GameObject go, Vector3 point)
	{
		//Debug.Log(point);

		Dictionary<MeshFilter, List<IndexPointPair>> nearestVertexDictionary = new Dictionary<MeshFilter, List<IndexPointPair>>();


		// Check for a point cache
		SuperBouncePointCache pointCache = go.GetComponent<SuperBouncePointCache>();
		// Check in a parent
		if(pointCache == null)
			pointCache = go.GetComponentInParent<SuperBouncePointCache>();


		// Keep track of the min
		float minDistanceSqr = Mathf.Infinity;

		foreach(MeshFilter meshFilter in go.GetComponentsInChildren<MeshFilter>())
		{
			Mesh mesh = null;
			if(meshFilter != null)
				mesh = meshFilter.mesh;

			// If point cache is still null then use the mesh
			List<IndexPointPair> pointPairList = (pointCache != null && pointCache.bouncePoints.Count > 0) ? pointCache.bouncePoints[meshFilter] : mesh.vertices.Select((p, index) => new IndexPointPair(index, meshFilter.gameObject.transform.TransformPoint(p))).ToList();

			if(mesh != null)
			{
				// scan all vertices to find nearest
				foreach(IndexPointPair pointPair in pointPairList)
				{
					// TODO: remove these random debug lines
					//if(pointPair.index == 5)
					//	Debug.Log(pointPair.point);

					Vector3 diff = point-pointPair.point;
					float distSqr = diff.sqrMagnitude;
					
					if (distSqr < minDistanceSqr)
					{
						//Debug.Log("clearing nearest: " + pointPair.index + ": " + pointPair.point);
						nearestVertexDictionary.Clear();

						minDistanceSqr = distSqr;

						//Debug.Log(nearestVertex + " " + distSqr);
					}

					if(Mathf.Approximately(distSqr, minDistanceSqr))
					{
						if(!nearestVertexDictionary.ContainsKey(meshFilter))
							nearestVertexDictionary[meshFilter] = new List<IndexPointPair>();

						//Debug.Log("adding nearest: " + pointPair.index + ": " + pointPair.point);
						// convert nearest vertex back to world space and add it to the list
						nearestVertexDictionary[meshFilter].Add(new IndexPointPair(pointPair));

					}
				}
				//Debug.Log("near: " + nearestVertex);
			}
		}
		

		return nearestVertexDictionary;
	}


	/// <summary>
	/// Finds all connected edges to the vert
	/// </summary>
	/// <param name="go">The gameobject with a MeshFilter with Mesh</param>
	/// <param name="vert">The <see cref="IndexPointPair" /> to find nearet to</param>
	/// <returns>List of connected edges to the vert</returns>
	public static List<Vector3[]> ConnectedEdgesTo(GameObject go, IndexPointPair vert)
	{
		MeshFilter meshFilter = go.GetComponent<MeshFilter>();
		Mesh mesh = null;
		if(meshFilter != null)
			mesh = meshFilter.mesh;

		List<Vector3[]> connectedEdgesList = new List<Vector3[]>();


		int[] asdf_array = new int[] {10, 9, 8, 7, 6};
		/* * /
		Array.FindAll(asdf_array, (index) => {
			Debug.Log(index);
			return false;
		});
		/* */
		/* * /
		int[] awfefwewaef = asdf_array.Where((value, index) => {
			Debug.Log(index);
			return false;
		}).ToArray();
		/* */

		if(mesh != null)
		{
			
			// Get all triangle indexes where that vert was used
			List<int> triangleIndexes = new List<int>();
			for(int i = 0; i <  mesh.triangles.Length; i++)
			{
				// If the triangle has the vertindex we are looking for
				if(vert.index == mesh.triangles[i])
				{
					// Add the triangle index to the list
					triangleIndexes.Add(i);
				}
			}

			//Debug.Log(triangleIndexes.ToDebugString());

			// Loop through all the instances where the vert was used
			foreach(int triIndex in triangleIndexes)
			{

				// Get the 0, 1, 2 offset postions
				int currIndex = UtilityMethods.mod(triIndex, 3);
				int prevIndex = UtilityMethods.mod(triIndex-1, 3);
				int nextIndex = UtilityMethods.mod(triIndex+1, 3);
				//Debug.Log("trioffset: " + prevIndex + " " + currIndex + " " + nextIndex);
				
				// (triIndex-currIndex) gets the start index of the triangle
				// And then we add the offset positins
				int prevTriIndex = (triIndex-currIndex) + prevIndex;
				int nextTriIndex = (triIndex-currIndex) + nextIndex;
				
				// Get the world coords of each point
				Vector3 prevPoint = go.transform.TransformPoint(mesh.vertices[mesh.triangles[prevTriIndex]]);
				Vector3 currPoint = go.transform.TransformPoint(mesh.vertices[mesh.triangles[triIndex]]);
				Vector3 nextPoint = go.transform.TransformPoint(mesh.vertices[mesh.triangles[nextTriIndex]]);

				connectedEdgesList.Add(new Vector3[]{ prevPoint, currPoint });
				connectedEdgesList.Add(new Vector3[]{ currPoint, nextPoint });

				//Debug.Log(go.transform.TransformPoint(mesh.vertices[mesh.triangles[25]]));

				//Debug.Log("triindex: " + prevTriIndex + " " + triIndex + " " + nextTriIndex);
				//Debug.Log("vertindex: " + mesh.triangles[prevTriIndex] + " " + mesh.triangles[triIndex] + " " + mesh.triangles[nextTriIndex]);
				//Debug.Log(currPoint);
			}
		}

		return connectedEdgesList;
	}
	

	/// <summary>
	/// Finds the closest point on the line segment A to B
	/// </summary>
	/// <param name="pointA">Starting point in line segment</param>
	/// <param name="pointB">Ending point in line segment</param>
	/// <param name="pointP">Point to find the closest to</param>
	/// <returns><see cref="Vector3" /> of the closest point to segment</returns>
	public static Vector3 ClosestPointOnSegment(Vector3 pointA, Vector3 pointB, Vector3 pointP)
	{
		Vector3 aToB = pointB - pointA;
		Vector3 aToP = pointP - pointA;

		Vector3 pointOnLine = pointA + Vector3.Project(aToP, aToB.normalized);

		// Clamp the point to the segment
		pointOnLine = pointA + Vector3.ClampMagnitude(pointOnLine-pointA, aToB.magnitude);
		pointOnLine = pointB + Vector3.ClampMagnitude(pointOnLine-pointB, aToB.magnitude);
		//Debug.Log(pointOnLine.magnitude);

		return pointOnLine;
	}


	void OnDrawGizmos()
	{
		if(enabled)
		{
			/* * /
			Gizmos.color = new Color(1f, 1f, 1f, .8f);
			Gizmos.DrawLine(objectA.transform.position, objectB.transform.position);

			Gizmos.color = new Color(1f, 0f, 0f, .8f);
			Gizmos.DrawSphere(this.ClosestPointOnSegment(objectA.transform.position, objectB.transform.position, point.transform.position), .25f);
			/* */

			/* */
			if(this.point != null && this.meshGameObject != null)
			{
				Gizmos.color = new Color(1f, .2f, .2f, 1f);
				Dictionary<MeshFilter, List<IndexPointPair>> verticeDictionary = this.NearestVerticesTo(this.meshGameObject, this.point.transform.position);

				foreach(KeyValuePair<MeshFilter, List<IndexPointPair>> meshPointsPair in verticeDictionary)
				{
					//Debug.Log(meshPointsPair.Value.ToDebugString());

					//Debug.Log(meshPointPair.Value.ToDebugString());
					int count = 1;
					foreach(IndexPointPair vert in meshPointsPair.Value)
					{
						// Draw the closest vertex
						Gizmos.color = new Color(1f, .2f, .2f, 1f);
						Gizmos.DrawWireSphere(vert.point, .08f + (.04f*count));
						//Debug.Log("point: " + vert.point);

						// Draw all of the connected edges to that vert
						var edges = this.ConnectedEdgesTo(meshPointsPair.Key.gameObject, vert);
						//Debug.Log(edges.Count);
						foreach(Vector3[] edge in edges)
						{
							for(int edgeIndex = 0; edgeIndex < edge.Length-1; edgeIndex++)
							{
								
								Gizmos.color = new Color(.1f, 1f, .1f, 1f);
								Gizmos.DrawLine(edge[edgeIndex]+new Vector3(0f, .01f * count, 0f), edge[edgeIndex+1]+new Vector3(0f, .01f * count, 0f));

								
								Vector3 closestPointOnEdge = this.ClosestPointOnSegment(edge[edgeIndex], edge[edgeIndex+1], this.point.transform.position);
								float distanceAway = Vector3.Distance(closestPointOnEdge, this.point.transform.position);
								//Debug.Log(distanceAway);
								if(distanceAway < .4f)
								{
									// Draw the closestpoint
									Gizmos.color = new Color(.1f, 1f, .1f, 1f);
									Gizmos.DrawSphere(closestPointOnEdge, .1f);
									// And just some conrast border
									Gizmos.color = new Color(0f, 0f, 0f, .3f);
									Gizmos.DrawWireSphere(closestPointOnEdge, .1f);
								}
							}
						}

						count++;
					}
				}

			}
			/* */
		}
	}
}
