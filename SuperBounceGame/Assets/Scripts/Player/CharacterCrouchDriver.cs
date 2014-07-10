using UnityEngine;
using System.Collections;

public class CharacterCrouchDriver : MonoBehaviour 
{

	public bool EnableMovement = true;
	
	// Height of player minus crouch minus a little bit of padding
	// We use this to size the box collider
	public float crouchHeight = 1.85f - .35f;

	public float crouchAnimationSpeed = 3f;

	// So we can change the height of collider when we crouch
	public BoxCollider playerBoxCollider;
	public NetworkedAnimator networkedAnimator;

	public bool IsCrouching
	{
		get;
		set;
	}



	private Vector3 initPlayerBoxColliderSize;
	private Vector3 initPlayerBoxColliderCenter;
	public float initSizeCenterRatioY = 2f;


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
		if(this.EnableMovement)
		{
			// Crouching
			this.IsCrouching = Input.GetAxisRaw("Crouch") > .7f;
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
