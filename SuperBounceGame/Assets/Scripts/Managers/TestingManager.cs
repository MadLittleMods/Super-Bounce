using UnityEngine;
using System.Collections;

// This is just a class to use on testing scenes where we might need to init stuff ourselves
public class TestingManager : MonoBehaviour {

	public Player player;

	// Use this for initialization
	void Start () {
		if(this.player != null)
		{
			this.player.HandleSetup();
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
