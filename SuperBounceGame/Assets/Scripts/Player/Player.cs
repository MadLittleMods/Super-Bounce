/*
 * Radius: Complete Unity Reference Project
 * 
 * Source: https://github.com/MadLittleMods/Radius
 * Author: Eric Eastwood, ericeastwood.com
 * 
 * File: Player.cs, May 2014
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour 
{
	public struct PlayerData
	{
		public bool IsMine;
		public string guid;
		public string Gamertag;
		public Team Team;
		public string TeamString {
			get {
				return this.Team.ToString();
			}
		}
		public Dictionary<string, float> TeamColor;
		public Dictionary<string, float> PersonalColor;
	}

	public enum Team {
		None, Individual, Red, Blue
	}

	public delegate void PlayerUpdatedEventHandler(Player sender);
	public event PlayerUpdatedEventHandler OnPlayerUpdated = delegate { };


	public class TeleportEventArgs : EventArgs
	{
		public Vector3 position;
		public Quaternion rotation;
		
		public TeleportEventArgs()
		{
			
		}
		
		public TeleportEventArgs(Vector3 position, Quaternion rotation)
		{
			this.position = position;
			this.rotation = rotation;
		}
	}
	public delegate void TeleportEventHandler(TeleportEventArgs e);
	// Having the `delegate { }` makes us able not have to check for null when firing the event
	public event TeleportEventHandler OnTeleport = delegate { };




	private Team playerTeam = Team.None;
	public Team PlayerTeam
	{
		get {
			return playerTeam;
		}
		set {
			this.playerTeam = value;

			if(this.playerInitialized)
				this.OnPlayerUpdated(this);

			this.ChangeTeams(this.ThisTeamToColor());
		}
	}

	private Color personalColor = new Color(0, 0, 0, 1); // Personalized color even if you are on a team (just like halo)
	public Color PersonalColor
	{
		get {
			return personalColor;
		}
		set {
			this.personalColor = new Color(Mathf.Clamp(value.r, 0, 1), Mathf.Clamp(value.g, 0, 1), Mathf.Clamp(value.b, 0, 1), Mathf.Clamp(value.a, 0, 1));

			if(this.playerInitialized)
				this.OnPlayerUpdated(this);
			
			this.ChangeTeams(this.ThisTeamToColor());
		}
	}


	private string gamertag;
	public string Gamertag
	{
		get {
			return this.gamertag;
		}
		set {
			this.gamertag = value;

			if(this.playerInitialized)
				this.OnPlayerUpdated(this);

			this.UpdateGamertag(this.Gamertag);
		}
	}

	
	public bool forceNetworkViewToBeMineToMove = true;
	public bool forceGameManagerStatusToMove = true;

	public GameObject[] objectsToChangeTeamColor;

	public Transform CameraSpawn;
	
	public RigidbodyCharacterDriver rigidbodyCharacterDriver;
	public CharacterDriver characterDriver;
	public CharacterCrouchDriver characterCrouchDriver;
	public HeadMove headMoveComponent;
	public MimicChildOf headCameraMimicChildOfComponent;

	public PlayerModelExposer modelPlayerModelExposer;
	public NetworkedAnimator networkedAnimator;


	PlayerManager playerManager;
	GameManager gameManager;


	// We just use this to keep track
	private string _guid;
	public string guid
	{
		get {
			return _guid;
		}
		private set {
			this._guid = value;
			
			if(this.playerInitialized)
				this.OnPlayerUpdated(this);
		}
	}


	private bool _isMine = false;
	public bool IsMine
	{
		get {
			return _isMine;
		}
		private set {
			this._isMine = value;

			if(this.playerInitialized)
				this.OnPlayerUpdated(this);
		}
	}

	// We use this to tell we if put the initial values in place
	// Without triggering a bunch of events
	private bool playerInitialized;

	// Use this for initialization
	void Start () {
		// Save the object across levels
		DontDestroyOnLoad(this.gameObject);

		GameObject[] managerObjects = GameObject.FindGameObjectsWithTag("Manager");
		if(managerObjects.Length > 0)
		{
			GameObject managerObject = managerObjects[0];
			
			// Grab the Player Manager
			this.playerManager = managerObject.GetComponent<PlayerManager>();
			this.gameManager = managerObject.GetComponent<GameManager>();

			if(this.playerManager != null)
			{
				//Debug.Log("Hooking sensitivity and input smoothing on player");
				// Change the sensitivity when the setting is changed
				this.playerManager.OnMouseSensitivityUpdated += (sender, sensitivity) => {
					if(this.headMoveComponent != null)
					{
						this.headMoveComponent.sensitivityX = sensitivity;
						this.headMoveComponent.sensitivityY = sensitivity;
					}
				};

				// Change the input smoothing when the setting is changed
				this.playerManager.OnMovementInputSmoothingUpdated += (sender, inputSmoothing) => {
					if(this.rigidbodyCharacterDriver != null)
					{
						//Debug.Log("Updating input smoothing on player component");
						this.rigidbodyCharacterDriver.inputSmoothing = inputSmoothing;
					}
					if(this.characterDriver != null)
					{
						this.characterDriver.inputSmoothing = inputSmoothing;
					}
				};
			}

			if(this.gameManager != null)
			{
				// When the game ends reset the mecanim
				this.gameManager.OnGameEnded += (sender) => {
					// Set to false when we start
					// We set it back to true at the end
					if(this.networkedAnimator != null)
					{
						this.networkedAnimator.SetBool("IsInit", false);
						if(this.headMoveComponent != null)
						{
							this.headMoveComponent.EnableInput = false;
							this.headMoveComponent.Reset();
						}
					}
				};
			}
		}


		if(this.headMoveComponent != null)
		{
			// Whenever we move the head update the mimic-childs
			// This is to avoid script execution order madness and rage
			this.headMoveComponent.OnMove += (e) => {
				if(this.headCameraMimicChildOfComponent != null)
				{
					this.headCameraMimicChildOfComponent.UpdatePosition();
				}
			};
			
			if(this.rigidbodyCharacterDriver != null)
			{
				// Attach the character driver
				this.rigidbodyCharacterDriver.OnVelocityChange += (e) => {
					//Debug.Log(e.Velocity.ToVerboseString(3, true));
					this.headMoveComponent.characterMovementVelocity = e.Velocity;
				};
			}
			if(this.characterDriver != null)
			{
				this.characterDriver.OnVelocityChange += (e) => {
					this.headMoveComponent.characterMovementVelocity = e.Velocity;
				};
			}
		}

		// Hook the crouching driver into our character movement driver
		if(this.characterCrouchDriver != null)
		{
			if(this.rigidbodyCharacterDriver != null)
			{
				this.characterCrouchDriver.OnCrouchStateChange += (e) => {
					this.rigidbodyCharacterDriver.IsCrouching = e.IsCrouching;
				};
			}

			if(this.characterDriver != null)
			{
				this.characterCrouchDriver.OnCrouchStateChange += (e) => {
					this.characterDriver.IsCrouching = e.IsCrouching;
				};
			}
		}
		
		
		if(networkView == null && this.forceNetworkViewToBeMineToMove)
			Debug.LogWarning("Missing NetworkView component on: " + gameObject);


		if(networkView.isMine) {
			Debug.Log("Start run for my player object");
			this.IsMine = true;
			this.Gamertag = "User" + UnityEngine.Random.Range(0, 9) + UnityEngine.Random.Range(0, 9) + UnityEngine.Random.Range(0, 9);

			this.PersonalColor = new HSBColor(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(.8f, 1f), UnityEngine.Random.Range(.8f, 1f), 1).ToColor();
			this.PlayerTeam = Team.Individual;
		}

		this.playerInitialized = true;

		// Fire the event
		this.OnPlayerUpdated(this);
	}


	// Update is called once per frame
	void Update () {
		bool enableMovement = true;
		// Only control the player if the networkView belongs to you
		//Debug.Log((!this.forceNetworkViewToBeMineToMove) + " (" + (networkView != null) + " " + networkView.isMine + ")");
		enableMovement &= !this.forceNetworkViewToBeMineToMove || (networkView != null && networkView.isMine);
		// If the game has started
		enableMovement &= !this.forceGameManagerStatusToMove || (this.gameManager != null && this.gameManager.GameStatus == GameManager.GameState.started);

		if(this.headMoveComponent != null)
		{
			this.headMoveComponent.EnableInput = enableMovement;
		}

		// Set whether we can move
		if(this.rigidbodyCharacterDriver != null)
		{
			this.rigidbodyCharacterDriver.EnableMovement = enableMovement;
		}
		if(this.characterCrouchDriver != null)
		{
			this.characterCrouchDriver.EnableMovement = enableMovement;
		}
		
		if(this.characterDriver != null)
		{
			this.characterDriver.EnableMovement = enableMovement;
		}

	}

	// Used for when you get spawned into the game and need to attach the camera to ourselves, etc
	public void HandleSetup()
	{
		Debug.Log("Handle Setup");

		// Set to false when we start
		// We set it back to true at the end
		if(this.networkedAnimator != null)
		{
			this.networkedAnimator.SetBool("IsInit", false);
		}


		// Put the camera in its place
		if(this.CameraSpawn != null)
		{
			Camera.main.transform.position = this.CameraSpawn.position;
			Camera.main.transform.rotation = this.CameraSpawn.rotation;
		}

		/* * /
		// Reset the head move 
		if(this.headMoveComponent != null)
		{
			this.headMoveComponent.Reset();
			this.headMoveComponent.EnableInput = true;
			
			this.headMoveComponent.Init();
		}
		else
			Debug.LogWarning("Missing `HeadMove` script on camera for bounce player");
		/* */

		// Setup the camera parent relationship
		var cameraMimicChildComponent = Camera.main.GetComponent<MimicChildOf>();
		if(cameraMimicChildComponent != null)
		{
			this.headCameraMimicChildOfComponent = cameraMimicChildComponent;

			if(this.modelPlayerModelExposer != null)
			{
				cameraMimicChildComponent.parentTransform = this.modelPlayerModelExposer.HeadObject.transform;
			}
			cameraMimicChildComponent.Init();
		}
		else
			Debug.LogWarning("Missing `MimicChildOf` script on camera for bounce player");





		if(this.rigidbodyCharacterDriver != null)
		{
			// We need this for input direction
			this.rigidbodyCharacterDriver.CameraTransform = Camera.main.transform;
		}
		if(this.characterDriver != null)
		{
			this.characterDriver.CameraTransform = Camera.main.transform;
		}


		// Start up the mecanim again
		if(this.networkedAnimator != null)
		{
			this.networkedAnimator.SetBool("IsInit", true);
		}


	}

	public void TeleportPlayer(Vector3 position, bool maintainVelocity = false)
	{
		this.TeleportPlayer(position, transform.rotation, maintainVelocity);
	}
	public void TeleportPlayer(Vector3 position, Quaternion rotation, bool maintainVelocity = false)
	{
		


		transform.position = position;
		transform.rotation = rotation;

		/* * /
		// Set to false when we start
		// We set it back to true at the end
		if(this.networkedAnimator != null)
		{
			this.networkedAnimator.SetBool("IsInit", false);
			if(this.headMoveComponent != null)
			{
				transform.rotation = Quaternion.identity;
				
				this.headMoveComponent.EnableInput = false;
				this.headMoveComponent.Reset();
				
				this.headMoveComponent.Init();
			}
		}
		/* */


		if(this.rigidbodyCharacterDriver != null)
		{
			if(!maintainVelocity && !this.rigidbodyCharacterDriver.rigidbody.isKinematic)
				this.rigidbodyCharacterDriver.rigidbody.velocity = Vector3.zero;
		}
		if(this.characterDriver != null)
		{
			this.characterDriver.currentState.Position = position;

			if(!maintainVelocity)
				this.characterDriver.currentState.Velocity = Vector3.zero;
		}

		// Fire the event
		this.OnTeleport(new TeleportEventArgs(transform.position, transform.rotation));
	}



	void OnNetworkInstantiate(NetworkMessageInfo info) {
		Debug.Log("New Player Instantiated. guid: " + networkView.owner.guid + "viewId: " + networkView.viewID);
		
		// Add players to managers
		// Only the server can spread players across the space
		// Because only the server knows the actual guid
		// Ask the server for the guid when we are created
		if(Network.isServer)
			this.RPC_Player_AddPlayer(networkView.owner.guid);
		else
			networkView.RPC("RPC_RequestPlayerInit", RPCMode.Server);
	}

	[RPC]
	public void RPC_RequestPlayerInit(NetworkMessageInfo info)
	{
		if(Network.isServer)
			networkView.RPC("RPC_Player_AddPlayer", info.sender, networkView.owner.guid);
	}

	[RPC]
	public void RPC_Player_AddPlayer(string guid)
	{
		Debug.Log("RPC_Player_AddPlayer: " + guid);

		// We need to find the player manager because this may be called before start maybe
		GameObject[] managerObjects = GameObject.FindGameObjectsWithTag("Manager");
		if(managerObjects.Length > 0)
		{
			GameObject managerObject = managerObjects[0];
			
			// Grab the Player Manager
			this.playerManager = managerObject.GetComponent<PlayerManager>();
		}

		this.guid = guid;

		if(this.playerManager)
		{
			this.playerManager.AddPlayer(guid, this);

			if(networkView.isMine)
				this.playerManager.AddGuidAlias("self", guid);
		}
		else
			Debug.LogWarning("Couldn't add player: " + guid + " to the PlayerManager");
	}

	private void ChangeTeams(Color color)
	{
		// Main ChangeTeams()

		HSBColor hsbColor = HSBColor.FromColor(color);

		HSBColor darkHsbColor = hsbColor;
		darkHsbColor.b -= .2f;

		HSBColor complimentHsbColor = hsbColor;
		complimentHsbColor.h += .14f;
		complimentHsbColor.h %= 1f;

		for(int i = 0; i < this.objectsToChangeTeamColor.Length; i++)
		{
			if(this.objectsToChangeTeamColor[i].renderer)
			{
				this.objectsToChangeTeamColor[i].renderer.material.color = darkHsbColor.ToColor();
				this.objectsToChangeTeamColor[i].renderer.material.SetColor("_SpecColor", complimentHsbColor.ToColor());
				this.objectsToChangeTeamColor[i].renderer.material.SetColor("_RimColor", complimentHsbColor.ToColor());
			}
		}

		// Only push it out if it your own to do
		if(networkView.isMine)
			networkView.RPC("RPCChangeTeams", RPCMode.OthersBuffered, (int)this.PlayerTeam, new Vector3(color.r, color.g, color.b));
	}

	[RPC]
	public void RPCChangeTeams(int team, Vector3 color)
	{
		this.PersonalColor = new Color(color.x, color.y, color.z, 1);
		this.PlayerTeam = (Team)team;
	}


	public void UpdateGamertag(string gamertag)
	{
		// Only push it out if it your own to do
		if(networkView.isMine)
			networkView.RPC("RPCUpdateGamertag", RPCMode.OthersBuffered, gamertag);
	}
	[RPC]
	public void RPCUpdateGamertag(string gamertag)
	{
		Debug.Log("Updating gamertag: " + gamertag);
		this.Gamertag = gamertag;
	}


	public static Color TeamToColor(Team team)
	{
		Color color = new Color(.5f, .5f, .5f, 1f);

		if(team == Team.None)
			color = new Color(.5f, .5f, .5f, 1f);
		else if(team == Team.Red)
			color = new Color(1f, 0f, .549f, 1f);
		else if(team == Team.Blue)
			color = new Color(0f, .549f, 1f, 1f);

		return color;
	}

	public Color ThisTeamToColor()
	{
		Color color = new Color(.5f, .5f, .5f, 1f);

		if(this.PlayerTeam == Team.Individual)
			color = this.PersonalColor;
		else
			color = Player.TeamToColor(this.PlayerTeam);

		return color;
	}

	public PlayerData GetPlayerData()
	{
		PlayerData pData = new PlayerData();
		pData.IsMine = this.IsMine;
		pData.guid = guid;
		pData.Gamertag = this.gamertag;
		pData.Team = this.PlayerTeam;
		pData.TeamColor = new ColorData(this.ThisTeamToColor()).ToDictionary();
		pData.PersonalColor = new ColorData(this.PersonalColor).ToDictionary();

		return pData;
	}
}
