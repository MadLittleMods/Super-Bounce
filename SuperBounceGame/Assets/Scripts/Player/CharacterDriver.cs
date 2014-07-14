/*
 * Radius: Complete Unity Reference Project
 * 
 * Source: https://github.com/MadLittleMods/Radius
 * Author: Eric Eastwood, ericeastwood.com
 * 
 * File: CharacterDriver.cs, June 2014
 */

using UnityEngine;
using System;
using System.Collections;

[RequireComponent (typeof(CharacterController))]
public class CharacterDriver : MonoBehaviour {

	public class VelocityChangeEventArgs : EventArgs
	{
		public Vector3 Velocity;
		
		public VelocityChangeEventArgs() {
		}
		
		public VelocityChangeEventArgs(Vector3 velocity)
		{
			this.Velocity = velocity;
		}
	}
	public delegate void VelocityChangeEventHandler(VelocityChangeEventArgs e);
	// Having the `delegate { }` makes us able not have to check for null when firing the event
	public event VelocityChangeEventHandler OnVelocityChange = delegate { };


	public delegate void GroundCollisionChangeEventHandler(CharacterState characterState, RaycastHit hit);
	// Having the `delegate { }` makes us able not have to check for null when firing the event
	public event GroundCollisionChangeEventHandler OnGroundCollision = delegate { };



	public bool EnableMovement = true;

	public float inputSmoothing = .1f; // Time in seconds to transition to the target input value

	public CharacterGroundedController characterGroundedController;

	// Optional
	public NetworkedAnimator networkedAnimator;
	public PlayerAnimatorEventCatcher playerAnimatorEventCatcher;
	public CharacterController characterController;

	public float maxSurgeSpeed = 5f; // Forward, backward
	public float maxSwaySpeed = 5f; // Side to side
	public float jumpHeight = 4f;

	public float crouchMaxSurgeSpeed = 5f; // Forward, backward
	public float crouchMaxSwaySpeed = 5f; // Side to side
	public float crouchJumpHeight = 4f;

	// Optional
	public Transform CameraTransform;

	// Optional
	public AudioBase jumpSoundEffect;
	public AudioBase footstepSoundEffect;

	
	GameManager gameManager;


	
	float inputVerticalSmoothed = 0f;
	float inputHorizontalSmoothed = 0f;
	
	// We use these to smooth between values in certain framerate situations in the `Update()` loop
	public CharacterState currentState = new CharacterState();
	CharacterState previousState = new CharacterState();

	CollisionFlags previousCollisionFlags;

	[HideInInspector]
	public float debugJumpYStart;
	[HideInInspector]
	public float debugJumpYMax;

	[HideInInspector]
	public Vector3 debugPrevPosition;
	[HideInInspector]
	public float debugGroundSpeed;




	// --------------------------------------
	
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
	
	
	public bool IsCrouching
	{
		get;
		set;
	}


	public bool IsGroundedSmart(Vector3 currentVelocity)
	{
		// Checks to see whether you are going in the opposite direction of gravity; If you are: return false
		// To test this, stand against a block/wall that you can jump on, then jump
		// You shouldn't get a true when you reach the block/wall to jumping from the ground
		Vector3 velocityProjectedOntoGravity = Vector3.Project(currentVelocity, Physics.gravity.normalized);
		float angleBetweenProjectedAndGravity = Vector3.Angle(velocityProjectedOntoGravity, Physics.gravity.normalized);

		if(Mathf.Approximately(angleBetweenProjectedAndGravity, 180f) && velocityProjectedOntoGravity.sqrMagnitude > .08f)
		{
			return false;
		}

		return this.characterController.isGrounded;
	}


	// Use this for initialization
	void Start () 
	{
		this.previousCollisionFlags = CollisionFlags.None;

		// Attach the footstep event
		if(this.playerAnimatorEventCatcher != null)
			this.playerAnimatorEventCatcher.OnPlayFootStepSound += this.PlayFootStepSound;


		// Grab the Player Manager
		GameObject[] managers = GameObject.FindGameObjectsWithTag("Manager");
		if(managers.Length > 0)
			this.gameManager = managers[0].GetComponent<GameManager>();


		this.ResetCharacterDriver();
	}

	// Use this to reset any built up forces
	[ContextMenu("Reset Character State")]
	void ResetCharacterDriver()
	{
		// Set the transition state
		this.currentState = new CharacterState(transform.position, Vector3.zero);
		this.previousState = this.currentState;
	}

	float t = 0f;
	float dt = 0.01f;
	float currentTime = 0f;
	float accumulator = 0f;

	bool isFirstPhysicsFrame = true;

	// Update is called once per frame
	void Update () 
	{
		float inputVertical = Input.GetAxisRaw("Vertical");
		float inputHorizontal = Input.GetAxisRaw("Horizontal");
		
		float vSign = inputVertical > this.inputVerticalSmoothed ? 1f : -1f;
		this.inputVerticalSmoothed = Mathf.Clamp(this.inputVerticalSmoothed + vSign*(Time.deltaTime/this.inputSmoothing), vSign > 0 ? this.inputVerticalSmoothed : inputVertical, vSign > 0 ? inputVertical : this.inputVerticalSmoothed);
		float hSign = inputHorizontal > this.inputHorizontalSmoothed ? 1f : -1f;
		this.inputHorizontalSmoothed = Mathf.Clamp(this.inputHorizontalSmoothed + hSign*(Time.deltaTime/this.inputSmoothing), hSign > 0 ? this.inputHorizontalSmoothed : inputHorizontal, hSign > 0 ? inputHorizontal : this.inputHorizontalSmoothed);



		// Fixed deltaTime rendering at any speed with smoothing
		// Technique: http://gafferongames.com/game-physics/fix-your-timestep/
		float frameTime = Time.time - currentTime;
		this.currentTime = Time.time;
		
		this.accumulator += frameTime;
		
		while (this.accumulator >= this.dt)
		{
			// Only control the player if the networkView belongs to you
			if(enabled && this.EnableMovement)
			{
				this.previousState = this.currentState;
				this.currentState = this.MoveUpdate(this.currentState, this.dt);


				//integrate(state, this.t, this.dt);
				Vector3 movementDelta = currentState.Position - transform.position;
				CollisionFlags moveCollisionFlags = this.characterController.Move(movementDelta);
				this.currentState = new CharacterState(transform.position, this.currentState.Velocity);

				/* */
				// Fire the ground collision event
				if((moveCollisionFlags == CollisionFlags.Below || moveCollisionFlags == CollisionFlags.CollidedBelow) && (this.previousCollisionFlags != CollisionFlags.Below || this.previousCollisionFlags != CollisionFlags.CollidedBelow))
				{
					//Debug.Log("hit ground: " + transform.position.ToDebugString());
					
					if(this.characterGroundedController != null)
					{
						RaycastHit[] groundHits = this.characterGroundedController.isGroundedSmartAll(this.currentState.Velocity + this.currentState.InstantVelocity);
						
						if(groundHits.Length > 0)
						{
							// Fire the event
							// We use previous velocity before we collided and the latest input movement velocity
							this.OnGroundCollision(new CharacterState(currentState.Position, this.previousState.Velocity + this.currentState.InstantVelocity), groundHits[0]);
						}
					}
					
					//Physics.RaycastAll(transform.position, Physics.gravity.normalized, this.characterController.
					//this.OnGroundCollision(new GroundCollisionEventArgs(transform.position, currentState.Velocity + inputMovementVelocity, isGroundedResults));
				}
				/* */

				this.previousCollisionFlags = moveCollisionFlags;
			}


			accumulator -= this.dt;
			this.t += this.dt;
			
		}
		


		// Reset it
		this.isFirstPhysicsFrame = true;
	}

	CharacterState MoveUpdate(CharacterState state, float deltaTime)
	{
		CharacterState currentState = new CharacterState(state);

		// Adjust mecanim parameters
		if(this.networkedAnimator != null)
		{
			this.networkedAnimator.SetFloat("SurgeSpeed", this.inputVerticalSmoothed);
			this.networkedAnimator.SetFloat("SwaySpeed", this.inputHorizontalSmoothed);
			if(this.characterController != null)
			{
				this.networkedAnimator.SetBool("IsGrounded", this.IsGroundedSmart(currentState.Velocity));
			}
			else
				Debug.LogWarning("Missing CharacterController in CharacterDriver");
		}

		// We use this for a gizmo
		this.debugGroundSpeed = (Vector3.Scale(currentState.Position, new Vector3(1, 0, 1)) - Vector3.Scale(this.debugPrevPosition, new Vector3(1, 0, 1))).magnitude / deltaTime;
		this.debugPrevPosition = currentState.Position;


		// Add the gravity
		if(this.IsGroundedSmart(currentState.Velocity))
		{
			// Remove the gravity in the direction we hit (ground)
			currentState.Velocity -= Vector3.Project(currentState.Velocity, Physics.gravity.normalized);
		}

		currentState.Velocity += Physics.gravity * deltaTime;

		// Jumping
		if(this.IsGroundedSmart(currentState.Velocity) && Input.GetButtonDown("Jump") && this.isFirstPhysicsFrame) 
		{
			// Play the jumping sound effect
			if(this.jumpSoundEffect)
				this.jumpSoundEffect.PlayOneShot();

			// This is to make sure we jump up
			// Even if there is any counteracting force like gravity
			Vector3 counteractYVelocity = -1f*Vector3.Project(currentState.Velocity, Physics.gravity.normalized);

			// Jump the player
			// Jump in the opposite direciton of gravity
			currentState.Velocity += ((-1f * Physics.gravity.normalized)*this.CalculateJumpVerticalSpeed(this.JumpHeight)) + counteractYVelocity;

			// Debugging jump height
			// Get the height the jump started at
			this.debugJumpYStart = currentState.Position.y;
			this.debugJumpYMax = currentState.Position.y; // Reset this

		}

		// Debugging jump height
		// GEt the new max so we can compare to where we started
		if(currentState.Position.y > this.debugJumpYMax)
			this.debugJumpYMax = currentState.Position.y;
		


		// Movement Surge and Sway
		// Elliptical player/character movement. See diagram for more info:
		// 		http://i.imgur.com/am2OYj1.png
		float angle =  Mathf.Atan2(this.inputVerticalSmoothed, this.inputHorizontalSmoothed);
		float surgeSpeed = Mathf.Abs(this.inputVerticalSmoothed) * this.MaxSurgeSpeed * Mathf.Sin(angle); // Forward and Backward
		float swaySpeed = Mathf.Abs(this.inputHorizontalSmoothed) * this.MaxSwaySpeed * Mathf.Cos(angle); // Left and Right


		Vector3 characterForward = transform.forward;
		Vector3 characterRight = transform.right;
		if(this.CameraTransform != null)
		{
			// Get the camera directions without the Y component
			Vector3 cameraForwardNoY = Vector3.Scale(this.CameraTransform.forward, new Vector3(1f, 0f, 1f)).normalized;
			Vector3 cameraRightNoY = Vector3.Scale(this.CameraTransform.right, new Vector3(1f, 0f, 1f)).normalized;
			
			characterForward = cameraForwardNoY;
			characterRight = cameraRightNoY;
		}


		Vector3 inputMovementVelocity = (characterForward*surgeSpeed + characterRight*swaySpeed);
		currentState.InstantVelocity = inputMovementVelocity;

		Vector3 movementDelta = inputMovementVelocity * deltaTime;
		movementDelta += currentState.Velocity * deltaTime;

		currentState.Position += movementDelta;

		// Set this so we don't get confused later on
		this.isFirstPhysicsFrame = false;

		// Fire the event
		this.OnVelocityChange(new VelocityChangeEventArgs(currentState.Velocity + inputMovementVelocity));





		return currentState;
	}

	public void AddVelocity(Vector3 velocity)
	{
		//Debug.Log("added velocity to character: " + velocity);
		this.currentState.Velocity += velocity;
	}



	// Played via animation event which we capture with `PlayerAnimatorEventCatcher`
	// Look in `Start()` for how we attach to an event
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
