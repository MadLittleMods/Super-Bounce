﻿/*
 * Radius: Complete Unity Reference Project
 * 
 * Source: https://github.com/MadLittleMods/Radius
 * Author: Eric Eastwood, ericeastwood.com
 * 
 * File: GameManager.cs, May 2014
 */

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {

	public class GameActivityEventArgs : EventArgs
	{
		public int level;
		
		public GameActivityEventArgs(int level)
		{
			this.level = level;
		}
	}

	public delegate void LevelChangedEventHandler(MonoBehaviour sender, GameActivityEventArgs e);
	public LevelChangedEventHandler OnLevelChanged = delegate { };

	public delegate void GameStartedEventHandler(MonoBehaviour sender);
	public GameStartedEventHandler OnGameStarted = delegate { };

	public delegate void GameEndedEventHandler(MonoBehaviour sender);
	public GameEndedEventHandler OnGameEnded = delegate { };
	

	
	public enum GameState {
		notStarted, started, pause
	}

	[SerializeField]
	private PlayerManager playerManager;
	[SerializeField]
	private NetworkManager networkManager;
	[SerializeField]
	private ScoreManager scoreManager;

	private GameState _gameStatus;
	public GameState GameStatus
	{
		get {
			return this._gameStatus;
		}
		set {
			// If we are changing from not started to started
			if(this._gameStatus == GameState.notStarted && value == GameState.started)
				this.OnGameStarted(this); // fire the event

			// If we are changing from started to not started
			if(this._gameStatus == GameState.started && value == GameState.notStarted)
				this.OnGameEnded(this); // fire the event

			this._gameStatus = value;

			Debug.Log("Setting Game Status");
			// Send it to the rest of the clients
			if(Network.isServer && this.networkManager.IsConnected)
			{
				Debug.Log("RPC UpdateGameStatus: " + this.GameStatus);
				networkView.RPC("RPCUpdateGameStatus", RPCMode.OthersBuffered, (int)this.GameStatus);
			}
		}
	}


	public List<string> LevelList = new List<string>();

	private string _currentLoadedLevel;
	public string CurrentLoadedLevel
	{
		get {
			return this._currentLoadedLevel;
		}
		private set {
			this._currentLoadedLevel = value;
		}
	}

	private string _selectedLevel;
	public string SelectedLevel
	{
		get {
			return this._selectedLevel;
		}
		set {
			// Only set the new selected level if it is in the level list
			if(this.LevelList.Contains(value))
				this._selectedLevel = value;
		}
	}


	// When the time or score limit are reached, the game ends
	// Negative for unlimited time
	public float GameTimeLimit = 3f * 60f; // In Seconds
	public float ScoreLimit = 250f;

	// Useful to know..
	public float PlayerHeight = 1.85f;

	private float _currentGameTime;
	public float CurrentGameTime
	{
		get {
			return this._currentGameTime;
		}
		set {
			this._currentGameTime = value;
		}
	}

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad(this.gameObject);
		networkView.group = 1;

		// Give focus to the game
		//Screen.lockCursor = true;

		this.scoreManager.OnScoreUpdated += (sender, e) => {
			// Check if the score limit was reached.
			// Someone may have won
			if(e.ScoreData.value > this.ScoreLimit)
				this.EndGame();
		};

		this.OnGameStarted += (sender) => {
			// Reset the game time when the game starts
			this.CurrentGameTime = 0f;
		};

		this.OnGameEnded += (sender) => {
			// Switch to the empty scene once the game ends
			this.ChangeLevel("empty_scene");
		};
	}
	
	// Update is called once per frame
	void Update () {
		/* * /
		// Take care of mouse lock
		if (Input.GetKeyDown("escape"))
			Screen.lockCursor = false;
		else if (Input.GetMouseButtonDown(0))
			Screen.lockCursor = true;
		/* */

		// If the game is started:
		if(this.GameStatus == GameState.started)
		{
			// count the time
			this.CurrentGameTime += Time.deltaTime;

			// Check if the time limit was reached
			// Make sure the time limit is above 0. < 0 equals infinite time
			if(this.GameTimeLimit > 0 && this.CurrentGameTime >= this.GameTimeLimit)
				this.EndGame();
		}
	}

	[ContextMenu("End Game")]
	void EndGame()
	{
		this.GameStatus = GameState.notStarted;
	}


	public void ChangeLevel(string level)
	{
		// Create a random level prefix
		int levelPrefix = UnityEngine.Random.Range(100, 255);

		// You can only change everyones level if you are a server
		if(Network.isServer && this.networkManager.IsConnected)
			networkView.RPC("RPCChangeLevel", RPCMode.AllBuffered, level, levelPrefix);
		else
			this.RPCChangeLevel(level, levelPrefix);
	}

	[RPC]
	public void RPCChangeLevel(string level, int levelPrefix)
	{
		Debug.Log("Changing level: " + level);

		Network.SetLevelPrefix(levelPrefix);
		Network.SetSendingEnabled(0, false);
		Network.isMessageQueueRunning = false;
		Application.LoadLevel(level);

		// Update it even though we haven't loaded that level yet...
		this.CurrentLoadedLevel = level;

		// Check OnLevelWasLoaded() for what happens
	}

	public void OnLevelWasLoaded(int level)
	{	
		Network.isMessageQueueRunning = true;
		Network.SetSendingEnabled(0, true);

		// 1: empty level
		if(this.playerManager && level != 1)
		{
			this.playerManager.GatherSpawnPoints(); // Re-gather all spawnpoints from the level
			this.playerManager.SpawnPlayer("self"); // Spawn in your player

			// Make the camera follow the player we just spawned
			/* * /
			var conicCameraController = Camera.main.GetComponent<ConicCameraController>();
			if(conicCameraController != null)
			{
				conicCameraController.followVehicle = this.playerManager.GetPlayer("self").gameObject;
			}
			/* */

			Player selfPlayer = this.playerManager.GetPlayer("self");
			if(selfPlayer)
			{
				selfPlayer.HandleSetup();
			}
		}

		// Fire the event
		Debug.Log("Firing the level changed event");
		this.OnLevelChanged(this, new GameActivityEventArgs(level));
	}


	[RPC]
	public void RPCUpdateGameStatus(int gameStatus)
	{
		Debug.Log("gameStatus updated: " + (GameState)gameStatus);
		this.GameStatus = (GameState)gameStatus;
	}



}
