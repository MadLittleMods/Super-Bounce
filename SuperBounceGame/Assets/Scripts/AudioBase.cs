/*
 * Radius: Complete Unity Reference Project
 * 
 * Source: https://github.com/MadLittleMods/Radius
 * Author: Eric Eastwood, ericeastwood.com
 * 
 * File: AudioBase.cs, May 2014
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioBase : MonoBehaviour 
{

	public enum AudioType {
		SoundEffect,
		Music
	}

	public string Name = "Audio Clip";

	public AudioType audioType;
	public List<AudioClip> soundEffect;
	int clipIndex = 0;
	
	public bool playOnAwake = false;
	public bool loop = false;

	[Range(0, 1)]
	public float volume = 1f;
	public float pitch = 1f;


	private AudioManager audioManager;

	private AudioSource audioSource;

	// Use this for initialization
	void Start () 
	{
		// Grab the Player Manager
		this.audioManager = GameObject.FindGameObjectsWithTag("Manager")[0].GetComponent<AudioManager>();

		// Listen for a master volume change and adjust it
		this.audioManager.OnVolumeChange += (sender, e) => {
			if(e.audioType == this.audioType)
				this.audioSource.volume = e.volume * this.volume;
		};

		GameObject go = new GameObject ("Audio: " +  this.soundEffect[0].name);
		go.transform.position = gameObject.transform.position;
		go.transform.parent = gameObject.transform;

		this.clipIndex = 0;

		this.audioSource = go.AddComponent<AudioSource>();
		this.audioSource.clip = this.soundEffect[this.clipIndex];
		this.audioSource.volume = this.audioManager.GetMasterVolume(this.audioType) * this.volume;
		this.audioSource.pitch = this.pitch;
		this.audioSource.loop = this.loop;
		if(this.playOnAwake)
			this.Play();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void Play() {
		this.audioSource.clip = this.soundEffect[UtilityMethods.mod(this.clipIndex, this.soundEffect.Count)];
		this.audioSource.Play();

		this.clipIndex++;
	}
	public void Stop() {
		this.audioSource.Stop();
	}
	public void Pause() {
		this.audioSource.Pause();
	}

	public void PlayOneShot() {
		// Play the sound effect
		this.audioSource.PlayOneShot(soundEffect[UtilityMethods.mod(this.clipIndex, this.soundEffect.Count)], this.audioManager.GetMasterVolume(this.audioType) * volume);

		this.clipIndex++;
	}
}
