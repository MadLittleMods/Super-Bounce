﻿/*
 * Radius: Complete Unity Reference Project
 * 
 * Source: https://github.com/MadLittleMods/Radius
 * Author: Eric Eastwood, ericeastwood.com
 * 
 * File: PlayerManager.cs, May 2014
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Coherent.UI;
using Coherent.UI.Binding;

public class PlayerManager : MonoBehaviour {

	public class PlayerActivityEventArgs : EventArgs
	{
		public Player.PlayerData PlayerData;
		
		public PlayerActivityEventArgs(Player.PlayerData pData)
		{
			this.PlayerData = pData;
		}
	}
	
	public delegate void PlayerJoinedEventHandler(MonoBehaviour sender, PlayerActivityEventArgs e);
	public event PlayerJoinedEventHandler OnPlayerJoined = delegate { };

	public delegate void PlayerLeftEventHandler(MonoBehaviour sender, PlayerActivityEventArgs e);
	public event PlayerLeftEventHandler OnPlayerLeft = delegate { };

	public delegate void PlayerUpdatedEventHandler(MonoBehaviour sender, PlayerActivityEventArgs e);
	public event PlayerUpdatedEventHandler OnPlayerUpdated = delegate { };


	public delegate void MouseSensitivityUpdatedEventHandler(MonoBehaviour sender, float sensitivity);
	public event MouseSensitivityUpdatedEventHandler OnMouseSensitivityUpdated = delegate { };

	public delegate void MovementInputSmoothingUpdatedEventHandler(MonoBehaviour sender, float inputSmoothing);
	public event MovementInputSmoothingUpdatedEventHandler OnMovementInputSmoothingUpdated = delegate { };



	public GameObject playerPrefab;

	List<Transform> spawnPointList = new List<Transform>();


	//List<Player> playerList = new List<Player>();
	private Dictionary<string, Player> playerList = new Dictionary<string, Player>();
	public ReadOnlyDictionary<string, Player> PlayerList
	{
		get {
			return new ReadOnlyDictionary<string, Player>(this.playerList);
		}
	}
	// You can use this to have special case guid aliased to things like "self" "player1" "player2"
	private Dictionary<string, string> aliasGuidList = new Dictionary<string, string>();


	// Use this for initialization
	void Start () {
		DontDestroyOnLoad(this.gameObject);
		networkView.group = 1;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void GenerateSelf()
	{
		// Generates the player gameobject for you

		//GameObject player = (GameObject)
		Network.Instantiate(this.playerPrefab, new Vector3(), new Quaternion(), 0);
	}

	public void AddPlayer(string guid, Player player)
	{
		this.AddPlayerSilent(guid, player);

		// Bind the player updated event to the player manager player update event
		// Yes we know a bit of a duplication but separation of power...
		player.OnPlayerUpdated += (updatedPlayer) => { 
			this.OnPlayerUpdated(this, new PlayerActivityEventArgs(updatedPlayer.GetPlayerData())); 
		};

		// Fire the join player event
		this.OnPlayerJoined(this, new PlayerActivityEventArgs(player.GetPlayerData()));
	}
	public void AddPlayerSilent(string guid, Player player)
	{
		// Same as addplayer but doesn't fire the event.
		Debug.Log("Added Player: " + guid);

		this.playerList[guid] = player;
	}

	public void RemovePlayer(string guid)
	{
		Player player;
		this.playerList.TryGetValue(guid, out player);
		if(player != null)
		{
			if(Network.isServer)
				Network.RemoveRPCs(player.networkView.viewID);

			Destroy(player.gameObject);
			this.playerList.Remove(guid);

			this.OnPlayerLeft(this, new PlayerActivityEventArgs(player.GetPlayerData()));
		}
		else
			Debug.LogWarning("Trying to remove player that is not in the list");
	}
	public void RemovePlayer(NetworkPlayer netPlayer)
	{
		this.RemovePlayer(netPlayer.guid);
	}
	public void RemoveAllPlayers()
	{
		List<string> guidList = new List<string>(this.playerList.Keys);

		foreach(string guid in guidList)
		{
			this.RemovePlayer(guid);
		}
	}

	public Player GetPlayer(string guid)
	{
		// Default to what we passed in
		// See if there is a alias we should use instead
		string newGuid = this.aliasGuidList.GetValueOrDefault(guid, guid);

		Player player;
		// Get the player
		this.playerList.TryGetValue(newGuid, out player);

		return player;
	}
	public void AddGuidAlias(string alias, string guid)
	{
		this.aliasGuidList[alias] = guid;
	}


	// The main SpawnPlayerMethod
	// Used to spawn yourself into the game and tell other clients about it.
	public void SpawnPlayer(string guid, Vector3 position, Quaternion rotation)
	{
		Debug.Log("Spawning Player: " + position);

		var player = this.GetPlayer(guid);
		if(player != null)
		{
			player.TeleportPlayer(position, rotation);
			//player.transform.position = position;
			//player.transform.rotation = rotation;
		}
		else
			Debug.LogWarning("Unable to spawn Player " + guid + " because it was null");
	}

	public void SpawnPlayer(string guid, Transform spawnTransform)
	{
		this.SpawnPlayer(guid, spawnTransform.position, spawnTransform.rotation);
	}

	public void SpawnPlayer(string guid)
	{
		if(this.spawnPointList.Count > 0)
			this.SpawnPlayer(guid, this.spawnPointList[UnityEngine.Random.Range(0, this.spawnPointList.Count)]);
		else
			this.SpawnPlayer(guid, Vector3.zero + new Vector3(0f, 1f, 0f), Quaternion.identity);
	}


	public void GatherSpawnPoints()
	{
		this.spawnPointList.Clear();
		GameObject[] points = GameObject.FindGameObjectsWithTag("SpawnPoint");
		Debug.Log("Num spawnpoints found: " + points.Length);
		foreach (GameObject point in points) {
			Debug.Log("Gathered Spawn Point: " + point.transform.position);
			this.spawnPointList.Add(point.transform);
		}
	}



	public Player[] GetAllPlayersOnSameTeam(Player comparePlayer)
	{

		var playerList = new List<Player>();

		if(comparePlayer.PlayerTeam != Player.Team.Individual)
		{
			foreach(KeyValuePair<string, Player> entry in this.playerList)
			{
				if(comparePlayer.PlayerTeam == entry.Value.PlayerTeam)
				{
					playerList.Add(entry.Value);
				}
			}

			return playerList.ToArray();
		}
		else
			return new Player[1] { comparePlayer };
	}


	[ContextMenu("Print Player List")]
	void DebugPlayerList()
	{
		Debug.Log(this.PlayerList.ToDebugString());
	}




	public void SetMouseSensitivity(float sensitivity) 
	{
		// Sanitize the data
		sensitivity = Mathf.Clamp(sensitivity, 0f, 100f);

		// Save it persistently
		PlayerPrefs.SetFloat("Player_MouseSensitivity", sensitivity);

		// Fire the event
		this.OnMouseSensitivityUpdated(this, sensitivity);
	}
	public float GetMouseSensitivity() 
	{
		return PlayerPrefs.GetFloat("Player_MouseSensitivity", 15f);
	}


	public void SetMovementInputSmoothing(float inputSmoothing) 
	{
		// Sanitize the data
		inputSmoothing = Mathf.Clamp(inputSmoothing, 0f, 3f);
		
		// Save it persistently
		PlayerPrefs.SetFloat("Player_MovementInputSmoothing", inputSmoothing);
		
		// Fire the event
		this.OnMovementInputSmoothingUpdated(this, inputSmoothing);
	}
	public float GetMovementInputSmoothing() 
	{
		return PlayerPrefs.GetFloat("Player_MovementInputSmoothing", .1f);
	}









}
