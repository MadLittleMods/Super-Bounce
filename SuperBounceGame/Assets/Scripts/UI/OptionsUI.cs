/*
 * Radius: Complete Unity Reference Project
 * 
 * Source: https://github.com/MadLittleMods/Radius
 * Author: Eric Eastwood, ericeastwood.com
 * 
 * File: OptionsUI.cs, May 2014
 */

using UnityEngine;
using System.Collections;
using Coherent.UI;
using Coherent.UI.Binding;

public class OptionsUI : MonoBehaviour {

	
	private CoherentUIView m_View;
	private bool viewReady = false;

	[SerializeField]
	private AudioManager audioManager;

	[SerializeField]
	private PlayerManager playerManager;

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad(this.gameObject);
		// Separate the UI into its own group 
		// so it doesn't get disabled when we change levels
		networkView.group = 1;

		this.m_View = GetComponent<CoherentUIView>();
		this.m_View.OnViewCreated += (view) => {this.viewReady = true;};
		this.m_View.OnViewDestroyed += () => {this.viewReady = false;};
		
		// Make the Coherent View receive input
		if(this.m_View != null)
			this.m_View.ReceivesInput = true;



		// Whenever something changes update the options
		if(this.audioManager != null)
		{
			this.audioManager.OnVolumeChange += (sender, e) => {
				this.m_View.View.TriggerEvent("updateOptions");
			};
		}

		if(this.playerManager != null)
		{
			this.playerManager.OnPlayerUpdated += (sender, e) => {
				this.m_View.View.TriggerEvent("updateOptions");
			};
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	[Coherent.UI.CoherentMethod("GUISetMasterVolume")]
	public void GUISetMasterVolume(string audioTypeString, float volume)
	{
		//Debug.Log("Master Volume set from gui");

		if(audioTypeString.ToLower() == "music")
			this.audioManager.SetMasterVolume(AudioBase.AudioType.Music, volume);
		else if(audioTypeString.ToLower() == "soundeffect")
			this.audioManager.SetMasterVolume(AudioBase.AudioType.SoundEffect, volume);
	}
	[Coherent.UI.CoherentMethod("GetMasterVolume")]
	public float GetMasterVolume(string audioTypeString)
	{
		// Return the master volume for that type
		return this.audioManager.GetMasterVolume(UtilityMethods.ParseEnum<AudioBase.AudioType>(audioTypeString));
	}


	[Coherent.UI.CoherentMethod("SetMouseSensitivity")]
	public void GUISetMouseSensitivity(float sensitivity)
	{

		if(this.playerManager != null)
		{
			this.playerManager.SetMouseSensitivity(sensitivity);

			/* * /
			var player = this.playerManager.GetPlayer("self");
			if(player != null)
			{
				var headMoveComponent = player.GetComponent<HeadMove>();
				//var headMoveComponent = Camera.main.GetComponent<HeadMove>();
				if(headMoveComponent != null)
				{
					headMoveComponent.sensitivityX = sensitivity;
					headMoveComponent.sensitivityY = sensitivity;
				}
			}
			/* */
		}
	}
	[Coherent.UI.CoherentMethod("GetMouseSensitivity")]
	public float GetMouseSensitivity()
	{
		if(this.playerManager != null)
		{
			return this.playerManager.GetMouseSensitivity();

			/* * /
			var player = this.playerManager.GetPlayer("self");
			if(player != null)
			{
				var headMoveComponent = player.GetComponent<HeadMove>();
				//var headMoveComponent = Camera.main.GetComponent<HeadMove>();
				if(headMoveComponent != null)
				{
					// Return the master volume for that type
					return headMoveComponent.sensitivityX;
				}
			}
			/* */
		}
		return 8f;

	}

	[Coherent.UI.CoherentMethod("SetMovemementInputSmoothing")]
	public void GUISetMovemementInputSmoothing(float inputSmoothing)
	{
		//Debug.Log("Setting inputSmoothing in options");

		if(this.playerManager != null)
		{
			//Debug.Log("Setting inputSmoothing in options and playerManager not null");
			this.playerManager.SetMovementInputSmoothing(inputSmoothing);

			/* * /
			var player = this.playerManager.GetPlayer("self");
			if(player != null)
			{
				var rigidbodyCharacterDriver = player.GetComponent<RigidbodyCharacterDriver>();
				if(rigidbodyCharacterDriver != null)
				{
					rigidbodyCharacterDriver.inputSmoothing = inputSmoothing;
				}
			}
			/* */

		}
	}
	[Coherent.UI.CoherentMethod("GetMovemementInputSmoothing")]
	public float GetMovemementInputSmoothing()
	{
		if(this.playerManager != null)
		{
			return this.playerManager.GetMovementInputSmoothing();

			/* * /
			var player = this.playerManager.GetPlayer("self");
			if(player != null)
			{
				var rigidbodyCharacterDriver = player.GetComponent<RigidbodyCharacterDriver>();
				if(rigidbodyCharacterDriver != null)
				{
					return rigidbodyCharacterDriver.inputSmoothing;
				}
			}
			/* */
		}

		return 0.1f;
	}
}
