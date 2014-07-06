using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour 
{

	public KinematicRigidbodyCharacterDriver characterDriver;

	public Transform CameraSpawn;


	public PlayerModelExposer modelPlayerModelExposer;

	// Use this for initialization
	void Start () {
		// Save the object across levels
		DontDestroyOnLoad(this.gameObject);

		GameObject[] managerObjects = GameObject.FindGameObjectsWithTag("Manager");
		if(managerObjects.Length > 0)
		{
			GameObject managerObject = managerObjects[0];
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void HandleSetup()
	{
		var headMoveComponent = Camera.main.GetComponent<HeadMove>();

		if(headMoveComponent != null)
		{
			if(this.modelPlayerModelExposer != null)
			{
				headMoveComponent.headTransform = this.modelPlayerModelExposer.HeadObject.transform;
			}
			headMoveComponent.bodyTransform = transform;

			headMoveComponent.Init();
		}
		else
			Debug.LogWarning("Missing `HeadMove` script on camera for bounce player");


		var mimicChildComponent = Camera.main.GetComponent<MimicChildOf>();
		if(mimicChildComponent != null)
		{
			mimicChildComponent.parentTransform = transform;
			mimicChildComponent.Init();
		}
		else
			Debug.LogWarning("Missing `MimicChildOf` script on camera for bounce player");
	}


	void HandleAddModel(GameObject modelGO)
	{
		var modelAnimator = modelGO.GetComponentInChildren<Animator>();
		var modelPlayerAnimatorEventCatcher = modelGO.GetComponentInChildren<PlayerAnimatorEventCatcher>();
		modelPlayerModelExposer = modelGO.GetComponentInChildren<PlayerModelExposer>();


		// Put the camera in its place
		if(this.CameraSpawn != null)
		{
			Camera.main.transform.position = this.CameraSpawn.position;
			Camera.main.transform.rotation = this.CameraSpawn.rotation;
		}

		if(this.characterDriver)
		{
			this.characterDriver.animator = modelAnimator;
			this.characterDriver.playerAnimatorEventCatcher = modelPlayerAnimatorEventCatcher;
		}
	}
}
