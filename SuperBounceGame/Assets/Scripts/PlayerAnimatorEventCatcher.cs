using UnityEngine;
using System.Collections;

public class PlayerAnimatorEventCatcher : MonoBehaviour {

	public delegate void PlayFootStepSoundEventHandler();
	// Having the `delegate { }` makes us able not have to check for null when firing the event
	public event PlayFootStepSoundEventHandler OnPlayFootStepSound = delegate { };

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void PlayFootStepSound()
	{
		// Fire the event
		this.OnPlayFootStepSound();
	}
}
