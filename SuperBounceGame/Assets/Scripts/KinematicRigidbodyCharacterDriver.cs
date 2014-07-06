using UnityEngine;
using UnityEditor;
using System.Collections;

[RequireComponent (typeof(KinematicRigidbodyCharacterController))]
public class KinematicRigidbodyCharacterDriver : MonoBehaviour 
{
	
	public float maxSurgeSpeed = 5f; // Forward, backward
	public float maxSwaySpeed = 5f; // Side to side
	public float jumpHeight = 4f;

	public float crouchMaxSurgeSpeed = 5f; // Forward, backward
	public float crouchMaxSwaySpeed = 5f; // Side to side
	public float crouchJumpHeight = 4f;

	// Height of player minus crouch minus a little bit of padding
	// We use this to size the box collider
	public float crouchHeight = 1.85f - .35f;

	public float MaxSurgeSpeed
	{
		get {
			return this.IsCrouching ? this.crouchMaxSurgeSpeed : this.maxSurgeSpeed;
		}
	}
	public float MaxSwaySpeed
	{
		get {
			return this.IsCrouching ? this.crouchMaxSwaySpeed : this.maxSwaySpeed;
		}
	}

	public float JumpHeight
	{
		get {
			return this.IsCrouching ? this.crouchJumpHeight : this.jumpHeight;
		}
	}
	
	// For animation
	public Animator animator;
	public PlayerAnimatorEventCatcher playerAnimatorEventCatcher;
	
	// So we can change the height of collider when we crouch
	public BoxCollider playerBoxCollider;

	public AudioBase jumpSoundEffect;
	public AudioBase footstepSoundEffect;


	// Used when you snapshot input, pause the game and want to step frame by frame
	[SerializeField]
	bool useSnapshotInputWhenPaused = false;
	
	KinematicRigidbodyCharacterController characterController;
	SuperBounceController superBounceController;
	
	[HideInInspector]
	public float debugJumpYStart;
	[HideInInspector]
	public float debugJumpYMax;
	
	[HideInInspector]
	public Vector3 debugPrevPosition;
	[HideInInspector]
	public float debugGroundSpeed;
	
	
	// We use these to smooth between values in certain framerate situations in the `Update()` loop
	public CharacterState currentState = new CharacterState();
	CharacterState previousState = new CharacterState();

	public bool IsCrouching
	{
		get;
		set;
	}



	private Vector3 initPlayerBoxColliderSize;
	private Vector3 initPlayerBoxColliderCenter;

	// Use this for initialization
	void Start () {
		this.characterController = GetComponent<KinematicRigidbodyCharacterController>();
		this.superBounceController = GetComponent<SuperBounceController>();

		// Attach the event
		if(this.playerAnimatorEventCatcher != null)
			this.playerAnimatorEventCatcher.OnPlayFootStepSound += this.PlayFootStepSound;

		this.ResetCharacterDriver();
	}
	
	// Use this to reset any built up forces
	[ContextMenu("Reset Character State")]
	void ResetCharacterDriver()
	{
		// Set the transition state
		this.currentState = new CharacterState(transform.position, Vector3.zero);
		this.previousState = this.currentState;

		// We use these for crouching
		this.initPlayerBoxColliderSize = this.playerBoxCollider.size;
		this.initPlayerBoxColliderCenter = this.playerBoxCollider.center;
	}
	
	float t = 0f;
	float dt = 0.01f;
	float currentTime = 0f;
	float accumulator = 0f;
	
	bool isFirstPhysicsFrame = true;
	
	// Update is called once per frame
	void Update () 
	{
		// Crouching
		this.IsCrouching = Input.GetAxis("Crouch") > .7f;
		if(this.animator)
		{
			this.animator.SetBool("IsCrouching", this.IsCrouching);
		}
		
		if(this.IsCrouching)
		{
			// Change to crouching height bounds
			Vector3 size = this.playerBoxCollider.size;
			this.playerBoxCollider.size = new Vector3(size.x, Mathf.Clamp(size.y - (3f*Time.deltaTime), this.crouchHeight, this.initPlayerBoxColliderSize.y), size.z); //Mathf.Lerp(bounds.y, this.crouchHeight, 5f*Time.deltaTime)
			
			// Center is halfway up the collider
			this.playerBoxCollider.center = new Vector3(this.initPlayerBoxColliderCenter.x, this.playerBoxCollider.size.y/2f, this.initPlayerBoxColliderCenter.z);
		}
		else
		{
			Vector3 size = this.playerBoxCollider.size;
			this.playerBoxCollider.size = new Vector3(size.x, Mathf.Clamp(size.y + (3f*Time.deltaTime), this.crouchHeight, this.initPlayerBoxColliderSize.y), size.z);
			//this.playerBoxCollider.size = this.initPlayerBoxColliderSize;
			this.playerBoxCollider.center = new Vector3(this.initPlayerBoxColliderCenter.x, this.playerBoxCollider.size.y/2f, this.initPlayerBoxColliderCenter.z);
		}




		/* * /
		// Fixed deltaTime rendering at any speed with smoothing
		// Technique: http://gafferongames.com/game-physics/fix-your-timestep/
		float frameTime = Time.time - currentTime;
		this.currentTime = Time.time;
		
		this.accumulator += frameTime;
		
		while (this.accumulator >= this.dt)
		{
			this.previousState = this.currentState;
			this.currentState = this.MoveUpdate(this.currentState, this.dt);
			
			//integrate(state, this.t, this.dt);
			Vector3 movementDelta = currentState.position - transform.position;
			this.characterController.Move(movementDelta);
			this.currentState = new CharacterState(transform.position, this.currentState.velocity);
			
			accumulator -= this.dt;
			this.t += this.dt;
		}
			
		
		// Reset it
		this.isFirstPhysicsFrame = true;
		/* */
	}

	void FixedUpdate()
	{
		/* * /
		this.currentState = this.MoveUpdate(this.currentState, Time.fixedDeltaTime);
		Vector3 movementDelta = currentState.position - transform.position;
		this.characterController.Move(movementDelta);
		this.currentState = new CharacterState(transform.position, this.currentState.velocity);
		/* */

		/* */
		this.currentState = this.MoveUpdate(this.currentState, Time.fixedDeltaTime);
		Noble.Log("Driver before CalculateMove: " + this.currentState);
		this.currentState = this.characterController.CalculateMove(this.currentState, this.superBounceController.HandleSuperBounce);
		Noble.Log("Driver after CalculateMove: " + this.currentState);
		transform.position = this.currentState.position;
		/* */

		//Debug.Log(this.currentState);

		this.isFirstPhysicsFrame = true;
	}

	float snapshotInputVertical = 0f;
	float snapshotInputHorizontal = 0f;

	CharacterState MoveUpdate(CharacterState state, float deltaTime)
	{
		CharacterState currentState = new CharacterState(state);

		// Save these so when we pause to debug
		// We can continue with those inputs
		if(EditorApplication.isPlaying)
		{
			//Debug.Log("Snapshot input");
			this.snapshotInputVertical = Input.GetAxis("Vertical");
			this.snapshotInputHorizontal = Input.GetAxis("Horizontal");
		}

		float inputVertical = EditorApplication.isPaused && this.useSnapshotInputWhenPaused ? this.snapshotInputVertical : Input.GetAxis("Vertical");
		float inputHorizontal = EditorApplication.isPaused && this.useSnapshotInputWhenPaused ? this.snapshotInputHorizontal :  Input.GetAxis("Horizontal");

		// Save this as we can save 1 calculation when we add the gravity
		bool isGrounded = this.characterController.isGroundedSmart(currentState.velocity);

		// Adjust mecanim parameters
		if(this.animator)
		{
			this.animator.SetFloat("SurgeSpeed", Input.GetAxis("Vertical"));
			this.animator.SetFloat("SwaySpeed", Input.GetAxis("Horizontal"));
			this.animator.SetBool("IsGrounded", isGrounded);
		}



		// We use this for a gizmo
		this.debugGroundSpeed = (Vector3.Scale(currentState.position, new Vector3(1, 0, 1)) - Vector3.Scale(this.debugPrevPosition, new Vector3(1, 0, 1))).magnitude / deltaTime;
		this.debugPrevPosition = currentState.position;
		
		/* */
		// Add the gravity
		if(isGrounded)
		{
			// Remove the gravity in the direction we hit (ground)
			currentState.velocity -= Vector3.Project(currentState.velocity, Physics.gravity.normalized);
		}

		currentState.velocity += Physics.gravity * deltaTime;
		/* */


		
		// Jumping
		// We have to determine if grounded again after gravity
		if(this.characterController.isGroundedSmart(currentState.velocity) && Input.GetButtonDown("Jump") && this.isFirstPhysicsFrame) 
		{
			//Debug.Log("Jump");

			// Jump the player
			currentState.velocity += -1f * Physics.gravity.normalized * this.CalculateJumpVerticalSpeed(this.jumpHeight);

			// Play the sound effect
			if(this.jumpSoundEffect != null)
				this.jumpSoundEffect.PlayOneShot();

			// Debugging jump height
			// Get the height the jump started at
			this.debugJumpYStart = currentState.position.y;
			this.debugJumpYMax = currentState.position.y; // Reset this
		}
		
		// Debugging jump height
		// GEt the new max so we can compare to where we started
		if(currentState.position.y > this.debugJumpYMax)
			this.debugJumpYMax = currentState.position.y;
		
		
		
		// Movement Surge and Sway
		// Elliptical player/character movement. See diagram for more info:
		// 		http://i.imgur.com/am2OYj1.png
		float angle =  Mathf.Atan2(inputVertical, inputHorizontal);
		float surgeSpeed = Mathf.Abs(inputVertical) * this.MaxSurgeSpeed * Mathf.Sin(angle); // Forward and Backward
		float swaySpeed = Mathf.Abs(inputHorizontal) * this.MaxSwaySpeed * Mathf.Cos(angle); // Left and Right
		
		
		
		// Get the camera directions without the Y component
		Vector3 cameraForwardNoY = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
		Vector3 cameraRightNoY = Vector3.Scale(Camera.main.transform.right, new Vector3(1, 0, 1)).normalized;

		currentState.instantVelocity = (cameraForwardNoY*surgeSpeed + cameraRightNoY*swaySpeed);

		Vector3 movementDelta = currentState.instantVelocity * deltaTime;
		movementDelta += currentState.velocity * deltaTime;
		
		currentState.position += movementDelta;
		
		// Set this so we don't get confused later on
		this.isFirstPhysicsFrame = false;
		
		return currentState;
	}
	



	// Played via animation event which we capture with `PlayerAnimatorEventCatcher`
	public void PlayFootStepSound()
	{
		if(this.footstepSoundEffect != null)
		{
			this.footstepSoundEffect.PlayOneShot();
		}
	}






	public float CalculateJumpVerticalSpeed(float targetJumpHeight)
	{
		// From the jump height and gravity we deduce the upwards speed 
		// for the character to reach at the apex.
		// See for formula: http://math.stackexchange.com/a/222585/60008
		return Mathf.Sqrt(2f * targetJumpHeight * Physics.gravity.magnitude);
	}



}