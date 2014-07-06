using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SuperBouncePointCache : MonoBehaviour 
{

	public bool drawGizmos = false;

	public Dictionary<MeshFilter, List<IndexPointPair>> bouncePoints = new Dictionary<MeshFilter, List<IndexPointPair>>();


	GameManager gameManager;

	// Use this for initialization
	void Start ()
	{
		// Grab the Game Manager
		GameObject[] managers = GameObject.FindGameObjectsWithTag("Manager");
		if(managers.Length > 0)
			this.gameManager = managers[0].GetComponent<GameManager>();

		this.RecalcBouncePoints();
	}

	void RecalcBouncePoints()
	{
		Vector3 playerOrientation = Vector3.up;
		float playerHeight = this.gameManager != null ? this.gameManager.PlayerHeight : 2f;

		foreach(MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>())
		{
			if(meshFilter != null && meshFilter.mesh != null)
			{
				//Debug.Log("vert: " + this.meshFilter.mesh.vertices.Length + " normal: " + this.meshFilter.mesh.normals.Length + " tris: " + this.meshFilter.mesh.triangles.Length);
				
				for(int normalIndex = 0; normalIndex < meshFilter.mesh.normals.Length; normalIndex++)
				{
					// If the angle of player to the surface is less than 90
					// about a Flat surface basically
					if(Vector3.Angle(playerOrientation, meshFilter.mesh.normals[normalIndex]) < 90)
					{
						Vector3 worldPoint = meshFilter.gameObject.transform.TransformPoint(meshFilter.mesh.vertices[normalIndex]);
						
						if(!Physics.Raycast(worldPoint, playerOrientation, playerHeight))
						{
							if(!this.bouncePoints.ContainsKey(meshFilter))
								this.bouncePoints[meshFilter] = new List<IndexPointPair>();

							// Convert to world space and add it to the list
							this.bouncePoints[meshFilter].Add(new IndexPointPair(normalIndex, worldPoint));
						
						}
					}
				}
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnDrawGizmos()
	{
		if(this.drawGizmos)
		{
			Gizmos.color = Color.red;
			foreach(KeyValuePair<MeshFilter, List<IndexPointPair>> entryPair in this.bouncePoints)
			{
				foreach(IndexPointPair pointPair in entryPair.Value)
				{
					Gizmos.DrawSphere(pointPair.point, .2f);
				}
			}
		}
	}
}
