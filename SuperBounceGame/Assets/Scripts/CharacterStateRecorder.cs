using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterStateRecorder : MonoBehaviour 
{
	public CharacterDriver characterDriver;

	List<Vector3> history = new List<Vector3>();

	bool playback = false;
	int index = -1;

	// Use this for initialization
	void Start () {
		this.playback = false;
	}

	void FixedUpdate() {
		if(!this.playback)
		{
			if(this.characterDriver != null)
				history.Add(this.characterDriver.currentState.Position);
		}
		else
		{
			this.characterDriver.EnableMovement = false;

			if(this.index >= 0)
			{
				transform.position = history[this.index];
			}
			this.index--;
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void StartPlayback()
	{
		this.playback = true;
		this.index = history.Count-1;
	}
}
