using UnityEngine;
using System.Collections;

public class HeadMove : MonoBehaviour {

	public Transform headTransform; // Used to rotate the head
	public Transform bodyTransform; // Used to rotate the body when head rotation limit reached

	public KinematicRigidbodyCharacterDriver characterDriver;

	public enum RotationAxes { 
		MouseXAndY, MouseX, MouseY 
	}
	public bool EnableInput = true;
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15f;
	public float sensitivityY = 15f;

	
	public float rotationMinimumX = -360f;
	public float rotationMaximumX = 360f;
	
	public float rotationMinimumY = -60f;
	public float rotationMaximumY = 60f;



	float rotationX = 0f;
	float rotationY = 0f;

	Quaternion headOriginalRotation;
	// We only store this so that when we reset when can truely reset
	Quaternion bodyOriginalRotation;


	private bool hasInitialized = false;

	// Use this for initialization
	void Start () {
		this.Init();
	}

	public void Init()
	{
		this.rotationX = 0f;
		this.rotationY = 0f;

		if(this.headTransform != null)
		{
			this.headOriginalRotation = this.headTransform.rotation;

			// We only initialize if we have a head as the body is optional
			this.hasInitialized = true;
		}
		else
			Debug.LogWarning("Missing headTransform in HeaderMove script");


		if(this.bodyTransform != null)
		{
			this.bodyOriginalRotation = this.bodyTransform.rotation;
		}
	}

	public void Reset()
	{
		this.rotationX = 0f;
		this.rotationY = 0f;

		if(this.headTransform != null && this.headOriginalRotation != null)
		{
			// Reset the head to the original
			this.headTransform.rotation = this.headOriginalRotation;
		}

		if(this.bodyTransform != null && this.bodyOriginalRotation != null)
		{
			this.bodyTransform.rotation = this.bodyOriginalRotation;
		}
	}
	
	// Has to be LateUpdate because of Mecanim
	void LateUpdate () 
	{
		if(this.hasInitialized && this.headTransform != null && this.bodyTransform != null)
		{


			if(this.EnableInput)
			{
				this.rotationX += Input.GetAxis("Mouse X") * sensitivityX;
				this.rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

				float excessXRotation = this.ClampExcess(this.rotationX, this.rotationMinimumX, this.rotationMaximumX);
				// Rotate the body when we reach the rotation limits
				if (axes == RotationAxes.MouseXAndY || axes == RotationAxes.MouseX)
					this.bodyTransform.localEulerAngles = new Vector3(this.bodyTransform.localEulerAngles.x, this.bodyTransform.localEulerAngles.y + excessXRotation, this.bodyTransform.localEulerAngles.z);
				
				this.rotationX = Mathf.Clamp(this.rotationX, this.rotationMinimumX, this.rotationMaximumX);
				this.rotationY = Mathf.Clamp(this.rotationY, this.rotationMinimumY, this.rotationMaximumY);
				//Debug.Log(this.rotationX + " " + this.rotationY);
			}

			Quaternion xQuaternion = Quaternion.AngleAxis(this.rotationX, Vector3.up);
			Quaternion yQuaternion = Quaternion.AngleAxis(this.rotationY, -Vector3.right);

			// We want parent rotation
			Quaternion parentRotation = this.bodyTransform.rotation;
			// Move the head
			this.headTransform.rotation = this.headOriginalRotation * parentRotation * xQuaternion * yQuaternion;
		
		
		
		
		
			// When we start to move forward, make sure we run in the direction our head is pointing
			/* */
			if(this.characterDriver)
			{
				Vector3 moveVelocity = this.characterDriver.currentState.velocity + this.characterDriver.currentState.instantVelocity;
				
				if(Mathf.Abs(moveVelocity.x) > 0 || Mathf.Abs(moveVelocity.z) > 0)
				{
					// Make the body rotate to where our head is pointing
					/* */
					Vector3 headForwardDirection = Vector3.Scale(this.headTransform.forward, new Vector3(1f, 0f, 1f));
					Quaternion qTo = Quaternion.LookRotation(headForwardDirection);
					this.bodyTransform.rotation = qTo;
					/* */
					
					
					// Counter-act the body by rotating the head back into place
					/* */
					Vector3 bodyForwardDirection = Vector3.Scale(this.bodyTransform.forward, new Vector3(1f, 0f, 1f));
					Quaternion qTo2 = Quaternion.LookRotation(bodyForwardDirection) * Quaternion.AngleAxis(rotationY, -Vector3.right);
					this.headTransform.rotation = qTo2;
					/* */
					
					// Also reset the look rotation of the head
					// So we do not jump back after a new loop
					rotationX = 0f;
				}
			}
			/* */



		}
	
	}


	void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;

		//Vector3 afds = this.headTransform.localRotation
		//Gizmos.DrawLine(this.headTransform.position
	}

	
	// Returns the excess from min or max
	float ClampExcess(float value, float min, float max)
	{
		if(value < min)
			return value - min;
		else if(value > max)
			return value - max;
		
		return 0;
	}
}
