using UnityEngine;
using System;
using System.Collections;

public class CharacterCrouchDriver : MonoBehaviour 
{

	public class CrouchStateChangeEventArgs : EventArgs
	{
		public bool IsCrouching = false;
		
		public CrouchStateChangeEventArgs() {
		}
		
		public CrouchStateChangeEventArgs(bool isCrouching)
		{
			this.IsCrouching = isCrouching;
		}
	}
	public delegate void CrouchStateChangeEventHandler(CrouchStateChangeEventArgs e);
	// Having the `delegate { }` makes us able not have to check for null when firing the event
	public event CrouchStateChangeEventHandler OnCrouchStateChange = delegate { };


	public bool EnableMovement = true;
	
	// Height of player minus crouch minus a little bit of padding
	// We use this to size the box collider
	public float crouchHeight = 1.85f - .35f;

	public float crouchAnimationSpeed = 3f;

	// So we can change the height of collider when we crouch
	public BoxCollider playerBoxCollider;
	public NetworkedAnimator networkedAnimator;

	private bool _isCrouching = false;
	public bool IsCrouching
	{
		get {
			return this._isCrouching;
		}
		set {
			// Fire the event if it is something different
			if(this._isCrouching != value)
			{
				this.OnCrouchStateChange(new CrouchStateChangeEventArgs(value));
			}

			this._isCrouching = value;
		}
	}



	private Vector3 initPlayerBoxColliderSize;
	private Vector3 initPlayerBoxColliderCenter;
	float initSizeCenterRatioY = 2f;


	// Use this for initialization
	void Start () 
	{
		if(this.playerBoxCollider != null)
		{
			// We use these for crouching
			this.initPlayerBoxColliderSize = this.playerBoxCollider.size;
			this.initPlayerBoxColliderCenter = this.playerBoxCollider.center;
			this.initSizeCenterRatioY = this.playerBoxCollider.size.y/this.playerBoxCollider.center.y;
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(enabled && this.EnableMovement)
		{
			// Crouching
			this.IsCrouching = Input.GetAxisRaw("Crouch") > .5f;
			if(this.networkedAnimator)
			{
				this.networkedAnimator.SetBool("IsCrouching", this.IsCrouching);
			}

			if(this.IsCrouching)
			{
				// Shrink the collider to crouchHeight size
				if(this.playerBoxCollider != null)
				{
					// Change to crouching height bounds
					Vector3 size = this.playerBoxCollider.size;
					this.playerBoxCollider.size = new Vector3(size.x, Mathf.Clamp(size.y - (this.crouchAnimationSpeed*Time.deltaTime), this.crouchHeight, this.initPlayerBoxColliderSize.y), size.z); //Mathf.Lerp(bounds.y, this.crouchHeight, 5f*Time.deltaTime)
					
				}
			}
			else
			{
				// Return to initial crouchHeight size
				if(this.playerBoxCollider != null)
				{
					Vector3 size = this.playerBoxCollider.size;
					this.playerBoxCollider.size = new Vector3(size.x, Mathf.Clamp(size.y + (this.crouchAnimationSpeed*Time.deltaTime), this.crouchHeight, this.initPlayerBoxColliderSize.y), size.z);
				}
			}

			// Make the bottom of the collider at the root of the transform
			if(this.playerBoxCollider != null)
			{
				// Center is halfway up the collider
				this.playerBoxCollider.center = new Vector3(this.initPlayerBoxColliderCenter.x, this.playerBoxCollider.size.y/this.initSizeCenterRatioY, this.initPlayerBoxColliderCenter.z);
			}
		}


	}
}
