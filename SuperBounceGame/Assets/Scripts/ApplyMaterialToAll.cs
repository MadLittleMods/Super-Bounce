using UnityEngine;
using System.Collections;

public class ApplyMaterialToAll : MonoBehaviour {

	public Color color = Color.white;
	public bool useColor = false;

	public Material material;

	// Use this for initialization
	void Start () {
		this.ApplyMaterial();
	}

	[ContextMenu("Apply Material")]
	void ApplyMaterial()
	{


		if(this.material != null)
		{
			MeshRenderer[] mRenderers = GetComponentsInChildren<MeshRenderer>();

			foreach(MeshRenderer renderer in mRenderers)
			{
				renderer.material = this.material;

				if(this.useColor && this.color != null)
					renderer.material.color = this.color;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
