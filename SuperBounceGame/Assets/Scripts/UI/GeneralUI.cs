/*
 * Radius: Complete Unity Reference Project
 * 
 * Source: https://github.com/MadLittleMods/Radius
 * Author: Eric Eastwood, ericeastwood.com
 * 
 * File: GeneralUI.cs, May 2014
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Coherent.UI;
using Coherent.UI.Binding;

public class GeneralUI : MonoBehaviour {
	
	
	private CoherentUIView m_View;
	private bool viewReady = false;

	[SerializeField]
	private GameManager gameManager;
	[SerializeField]
	private PlayerManager playerManager;

	bool isMenuUIUp = true;

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad(this.gameObject);
		// Separate the UI into its own group 
		// so it doesn't get disabled when we change levels
		networkView.group = 1;

		
		this.m_View = GetComponent<CoherentUIView>();


		// Add some instructions to maybe help them register the trial of coherent
		bool shouldShowRegistrationInstructionText = false;
		GameObject instructionGO = null;
		if(shouldShowRegistrationInstructionText)
		{
			instructionGO = new GameObject("CoherentInstructionGUIText");
			GUIText instructionGUIText = instructionGO.AddComponent<GUIText>();
			instructionGUIText.text = "Activate Coherent with registercoherent@superbouncegame.com";
			instructionGO.transform.position = new Vector3(0.01f, .93f, 0f);
		}

		if(this.m_View != null)
		{
			this.m_View.OnViewCreated += (view) => {
				this.viewReady = true;

				if(shouldShowRegistrationInstructionText && instructionGO != null)
				{
					// Once the coherent view is created, destroy the instructions
					Destroy(instructionGO);
				}
			};
			this.m_View.OnViewDestroyed += () => {
				this.viewReady = false;
			};
			
			// Make the Coherent View receive input
			this.m_View.ReceivesInput = true;
		}


		
		this.isMenuUIUp = true;
	}

	
	// Update is called once per frame
	void Update () {
		// Toggle the menu with the escaape key
		if (Input.GetKeyDown("escape"))
		{
			Debug.Log("Toggle menus");
			if(this.viewReady)
				this.m_View.View.TriggerEvent("toggleUI");

			// Toggle menuUpBool
			this.isMenuUIUp = !this.isMenuUIUp;


		}
	}

	[Coherent.UI.CoherentMethod("GUIOpenURLInBrowser")]
	public void GUIOpenURLInBrowser(string url)
	{
		Application.OpenURL(url);
	}
	
	[Coherent.UI.CoherentMethod("GUIGetLevelList")]
	public List<string> GUIGetLevelList()
	{
		return this.gameManager.LevelList;
	}

	[Coherent.UI.CoherentMethod("GUIGetPlayerData")]
	public Player.PlayerData GUIGetPlayerData(string guid)
	{
		Debug.Log("Returning PlayerData to the GUI");
		//return "asdf";
		return this.playerManager.GetPlayer(guid).GetPlayerData();
	}
	
	[Coherent.UI.CoherentMethod("GUIGetScoreMax")]
	public float GUIGetScoreMax()
	{
		return this.gameManager.ScoreLimit;
	}


	[Coherent.UI.CoherentMethod("GUIGetGameTime")]
	public float GUIGetGameTime()
	{
		return this.gameManager.GameTimeLimit - this.gameManager.CurrentGameTime;
	}


	[Coherent.UI.CoherentMethod("QuitGame")]
	public void QuitGame()
	{
		// Quit the game
		Application.Quit();
	}
}
