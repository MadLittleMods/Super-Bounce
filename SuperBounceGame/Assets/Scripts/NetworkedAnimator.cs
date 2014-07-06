using UnityEngine;
using System.Collections;

public class NetworkedAnimator : MonoBehaviour {

	public Animator animator;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	
	[RPC]
	public void SetBool(string name, bool value)
	{
		if(this.animator != null)
		{
			this.animator.SetBool(name, value);

			if(networkView.isMine)
				networkView.RPC("SetBool", RPCMode.OthersBuffered, name, value);
		}
	}

	[RPC]
	public void SetFloat(string name, float value)
	{
		if(this.animator != null)
		{
			this.animator.SetFloat(name, value);

			if(networkView.isMine)
				networkView.RPC("SetFloat", RPCMode.OthersBuffered, name, value);
		}
	}

	[RPC]
	public void SetInteger(string name, int value)
	{
		if(this.animator != null)
		{
			this.animator.SetInteger(name, value);
			
			if(networkView.isMine)
				networkView.RPC("SetInt", RPCMode.OthersBuffered, name, value);
		}
	}
}
