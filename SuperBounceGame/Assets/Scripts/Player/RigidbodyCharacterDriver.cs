using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyCharacterDriver : MonoBehaviour 
{


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

	public class GroundCollisionEventArgs : EventArgs
	{
		public CharacterState CharacterState;
		public RaycastHit Hit;
		
		public GroundCollisionEventArgs() {
		}
		
		public GroundCollisionEventArgs(Vector3 position, Vector3 velocity, RaycastHit hit)
		{
			this.CharacterState = new CharacterState(position, velocity);
			this.Hit = hit;
		}
	}
	public delegate void GroundCollisionChangeEventHandler(GroundCollisionEventArgs e);
	// Having the `delegate { }` makes us able not have to check for null when firing the event
	public event GroundCollisionChangeEventHandler OnGroundCollision = delegate { };
	

	public bool EnableMovement = true;
	
	public float inputSmoothing = .1f; // Time in seconds to transition to the target input value

	// Basically a tolerance for collisions
	public float SkinWidth = .08f;

	// Optional
	public NetworkedAnimator networkedAnimator;
	public PlayerAnimatorEventCatcher playerAnimatorEventCatcher;
	// Optional
	public Transform CameraTransform;

	public float maxSurgeSpeed = 5f; // Forward, backward
	public float maxSwaySpeed = 5f; // Side to side
	public float jumpHeight = 4f;
	
	public float crouchMaxSurgeSpeed = 5f; // Forward, backward
	public float crouchMaxSwaySpeed = 5f; // Side to side
	public float crouchJumpHeight = 4f;

	
	public AudioBase jumpSoundEffect;
	public AudioBase footstepSoundEffect;




	float inputVerticalSmoothed = 0f;
	float inputHorizontalSmoothed = 0f;

	Vector3 lastRigidBodyVelocity = Vector3.zero;



	[HideInInspector]
	public float debugJumpYStart;
	[HideInInspector]
	public float debugJumpYMax;


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

	// Consider using `isGroundedSmart()` below
	public RaycastHit[] isGroundedAll
	{
		get {
			Vector3 beforeTestPosition = transform.position;
			
			// Move the rigidbody up the skin width so we can project down
			transform.position += -1f * Physics.gravity.normalized * this.SkinWidth;
			
			RaycastHit[] sweepResult = rigidbody.SweepTestAll(Physics.gravity.normalized, 2f*this.SkinWidth);

			// TODO: look into if we should filter `sweepresult` based on this
			Vector3 movementInCollisionDirection = new Vector3(0f, 2f*this.SkinWidth, 0f);
			List<RaycastHit> actualHits = new List<RaycastHit>();
			foreach(RaycastHit hit in sweepResult)
			{
				bool isTrueHit = movementInCollisionDirection.magnitude >= hit.distance;
				if(isTrueHit)
					actualHits.Add(hit);

				//if(!isTrueHit)
				//	Debug.LogWarning("False isGrounded hit - movement: " + movementInCollisionDirection.magnitude + " hitDist: " + hit.distance);
			

			}

			// Set back the position so no on will know :P
			transform.position = beforeTestPosition;
			
			return actualHits.ToArray();
		}
	}
	public bool isGrounded
	{
		get {
			RaycastHit[] sweepResult = this.isGroundedAll;

			if(sweepResult.Length > 0)
				return true;
			
			return false;
		}
	}

	
	// A little bit smarter isGrounded that takes into account your current velocity
	public RaycastHit[] isGroundedSmartAll(Vector3 currentVelocity)
	{
		// Checks to see whether you are going in the opposite direction of gravity; If you are: return false
		// To test this, stand against a block/wall that you can jump on, then jump
		// You shouldn't get a true when you reach the block/wall top
		if(Mathf.Approximately(Vector3.Angle(rigidbody.velocity.normalized, Physics.gravity.normalized), 180f))
		{
			//Debug.Log("not grounded2");
			return new RaycastHit[0];
		}
		
		return this.isGroundedAll;
	}
	public bool isGroundedSmart(Vector3 currentVelocity)
	{
		RaycastHit[] sweepResult = this.isGroundedSmartAll(currentVelocity);

		if(sweepResult.Length > 0)
			return true;
		
		return false;
	}



	// Use this for initialization
	void Start () {
		// Attach the event
		if(this.playerAnimatorEventCatcher != null)
			this.playerAnimatorEventCatcher.OnPlayFootStepSound += this.PlayFootStepSound;

		this.ResetCharacterDriver();
	}
	
	// Use this to reset any built up forces
	[ContextMenu("Reset Character State")]
	void ResetCharacterDriver()
	{
		rigidbody.velocity = Vector3.zero;

	}
	

	void Update()
	{
		if(this.EnableMovement)
		{
			// Jumping
			if(this.isGrounded && Input.GetButtonDown("Jump")) 
			{
				// Countact any gravity
				float counteractNegativeYVelocity = (rigidbody.velocity.y < 0f ? Mathf.Abs(rigidbody.velocity.y) : 0f);

				// Jump the player
				rigidbody.velocity += -1f * Physics.gravity.normalized * (this.CalculateJumpVerticalSpeed(this.JumpHeight) + + counteractNegativeYVelocity);

				// Play the sound effect
				if(this.jumpSoundEffect != null)
					this.jumpSoundEffect.PlayOneShot();

				// Debugging jump height
				// Get the height the jump started at
				this.debugJumpYStart = transform.position.y;
				this.debugJumpYMax = transform.position.y; // Reset this
			}
			
			
			// Debugging jump height
			// GEt the new max so we can compare to where we started
			if(transform.position.y > this.debugJumpYMax)
				this.debugJumpYMax = transform.position.y;

		}
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		this.lastRigidBodyVelocity = rigidbody.velocity;

		if(this.EnableMovement)
		{
			this.Chug(Time.fixedDeltaTime);
			
		}
	}

	void Chug(float deltaTime)
	{
		float inputVertical = Input.GetAxisRaw("Vertical");
		float inputHorizontal = Input.GetAxisRaw("Horizontal");

		float vSign = inputVertical > this.inputVerticalSmoothed ? 1f : -1f;
		this.inputVerticalSmoothed = Mathf.Clamp(this.inputVerticalSmoothed + vSign*(Time.deltaTime/this.inputSmoothing), vSign > 0 ? this.inputVerticalSmoothed : inputVertical, vSign > 0 ? inputVertical : this.inputVerticalSmoothed);
		float hSign = inputHorizontal > this.inputHorizontalSmoothed ? 1f : -1f;
		this.inputHorizontalSmoothed = Mathf.Clamp(this.inputHorizontalSmoothed + hSign*(Time.deltaTime/this.inputSmoothing), hSign > 0 ? this.inputHorizontalSmoothed : inputHorizontal, hSign > 0 ? inputHorizontal : this.inputHorizontalSmoothed);


		// Save this as we can save 1 calculation when we add the gravity
		RaycastHit[] isGroundedResults = this.isGroundedSmartAll(rigidbody.velocity);
		bool isGrounded = isGroundedResults.Length > 0;
		
		// This is not done in `OnCollisionEnter`
		// Fire the ground collision event
		//if(isGrounded)
		//	this.OnGroundCollision(new GroundCollisionEventArgs(transform.position, rigidbody.velocity, isGroundedResults));

		// Adjust mecanim parameters
		if(this.networkedAnimator != null)
		{
			this.networkedAnimator.SetFloat("SurgeSpeed", this.inputVerticalSmoothed);
			this.networkedAnimator.SetFloat("SwaySpeed", this.inputHorizontalSmoothed);
			this.networkedAnimator.SetBool("IsGrounded", isGrounded);
		}


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
		
		Vector3 movementVelocity = surgeSpeed*characterForward + swaySpeed*characterRight;
		
		rigidbody.velocity = Vector3.Scale(rigidbody.velocity, new Vector3(0f, 1f, 0f)) + movementVelocity;
		//rigidbody.AddForce(Vector3.forward * speed, ForceMode.Force);

		// Fire the event
		this.OnVelocityChange(new VelocityChangeEventArgs(rigidbody.velocity));
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

	void OnCollisionEnter(Collision collision) 
	{
		DebugGizmos.DrawSphere(transform.position, .05f, Color.white, 4f);

		rigidbody.velocity = Vector3.zero;

		//Debug.Log(collision.contacts.Length);

		bool shouldDebugAllContactPoints = true;

		bool isGroundHit = false;
		RaycastHit groundHit = new RaycastHit();
		foreach (ContactPoint contact in collision.contacts) 
		{
			DebugGizmos.DrawSphere(contact.point, .05f, Color.grey, 2f);

			//Debug.Log(transform.position);
			// Get a point at the character y level but at the same xz position of the hit
			Vector3 origin = Vector3.Scale(contact.point, new Vector3(1f, 0f, 1f)) + Vector3.Scale(transform.position + (-1f * Physics.gravity.normalized * this.SkinWidth), new Vector3(0f, 1f, 0f));
			RaycastHit[] hits = Physics.RaycastAll(origin, Physics.gravity.normalized, 2f*this.SkinWidth);

			//DebugGizmos.DrawWireSphere(origin, .02f, Color.black, 2f);
			foreach(RaycastHit hit in hits)
			{
				//Debug.Log("hit");

				float angle = Vector3.Angle(new Vector3(5f, 1f, 0f), Physics.gravity.normalized);
				//Debug.Log(angle);
				if(angle > 90f && (angle <= 180f || Mathf.Approximately(angle, 180f)))
				{
					groundHit = hit;
					isGroundHit = true;

					DebugGizmos.DrawSphere(hit.point, .02f, Color.yellow, 2f);

					if(!shouldDebugAllContactPoints)
						break;

				}

				//Vector3.Angle(hit.normal, Physics.gravity.normalized);
			}


			// If we already found it in the inner loop, break out of this one
			if(isGroundHit && !shouldDebugAllContactPoints)
				break;
		}

		// Fire the event
		if(isGroundHit)
			this.OnGroundCollision(new GroundCollisionEventArgs(transform.position, this.lastRigidBodyVelocity, groundHit));

		//Debug.Log("collision: " + collision.gameObject);
	}



	public float CalculateJumpVerticalSpeed(float targetJumpHeight)
	{
		// From the jump height and gravity we deduce the upwards speed 
		// for the character to reach at the apex.
		// See for formula: http://math.stackexchange.com/a/222585/60008
		return Mathf.Sqrt(2f * targetJumpHeight * Physics.gravity.magnitude);
	}


}
