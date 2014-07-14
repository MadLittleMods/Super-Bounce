using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CharacterGroundedController : MonoBehaviour 
{

	public float SkinWidth = .08f;

	// Consider using `isGroundedSmart()` below
	public RaycastHit[] isGroundedAll
	{
		get {
			Vector3 beforeTestPosition = transform.position;
			
			// Move the rigidbody up the skin width so we can project down
			transform.position += -1f * Physics.gravity.normalized * this.SkinWidth;

			float movementInGravity = 2f*this.SkinWidth + .01f;
			RaycastHit[] sweepResult = rigidbody.SweepTestAll(Physics.gravity.normalized, movementInGravity);
			
			// TODO: look into if we should filter `sweepresult` based on this
			Vector3 movementInCollisionDirection = new Vector3(0f, movementInGravity, 0f);
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
		Vector3 velocityProjectedOntoGravity = Vector3.Project(currentVelocity, Physics.gravity.normalized);
		float angleBetweenProjectedAndGravity = Vector3.Angle(velocityProjectedOntoGravity, Physics.gravity.normalized);
		//Debug.Log((Mathf.Approximately(angleBetweenProjectedAndGravity, 180f) && velocityProjectedOntoGravity.sqrMagnitude > .08f) + ": " + angleBetweenProjectedAndGravity + " - " + velocityProjectedOntoGravity+":"+velocityProjectedOntoGravity.sqrMagnitude);
		//if(Mathf.Approximately(Vector3.Angle(currentVelocity.normalized, Physics.gravity.normalized), 180f))

		// If the velocity is in the opposite direction of gravity and it is above the noise, then we are not grounded
		if(Mathf.Approximately(angleBetweenProjectedAndGravity, 180f) && velocityProjectedOntoGravity.sqrMagnitude > .08f)
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
	
	}
	
	// Update is called once per frame
	void Update () 
	{

	}


}
