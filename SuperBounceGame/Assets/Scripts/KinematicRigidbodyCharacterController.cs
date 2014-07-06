using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System;

public class KinematicRigidbodyCharacterController : MonoBehaviour 
{

	public class CollisionEventArgs : EventArgs
	{
		public CharacterState BeforeCollisionState;
		public CharacterState AfterCollisionState;
		public RaycastHit Hit = new RaycastHit();

		public CollisionEventArgs()
		{
			
		}

		public CollisionEventArgs(CharacterState before, CharacterState after, RaycastHit hit)
		{
			this.BeforeCollisionState = before;
			this.AfterCollisionState = after;
			this.Hit = hit;
		}
	}
	public delegate void CollisionEventHandler(CollisionEventArgs e);
	// Having the `delegate { }` makes us able not have to check for null when firing the event
	public event CollisionEventHandler OnCollision = delegate { };




	// Basically a tolerance for collisions
	public float SkinWidth = .08f;

	// Consider using `isGroundedSmart()` below
	public bool isGrounded
	{
		get {
			/* */
			Vector3 beforeTestPosition = transform.position;
			
			// Move the rigidbody up the skin width so we can project down
			transform.position += -1f * Physics.gravity.normalized * this.SkinWidth;

			RaycastHit[] sweepResult = rigidbody.SweepTestAll(Physics.gravity.normalized, 2f*this.SkinWidth);

			// Set back the position so no on will know :P
			transform.position = beforeTestPosition;

			if(sweepResult.Length > 0)
				return true;
			/* */
			
			return false;
		}
	}

	// A little bit smarter isGrounded that takes into account your current velocity
	public bool isGroundedSmart(Vector3 currentVelocity)
	{
		// Checks to see whether you are going in the opposite direction of gravity; If you are: return false
		// To test this, stand against a block/wall that you can jump on, then jump
		// You shouldn't get a true when you reach the block/wall top
		if(Mathf.Approximately(Vector3.Angle(currentVelocity.normalized, Physics.gravity.normalized), 180f))
			return false;

		return this.isGrounded;
	}


	List<Vector3> debugPointOnBounds = new List<Vector3>();
	List<Vector3> debugCollisionPoint = new List<Vector3>();

	Vector3 debugLastMoveDeltaMovement = Vector3.zero;
	Vector3 debugSweepTestFromPosition = Vector3.zero;
	Vector3 debugSweepTestOffsetDestination = Vector3.zero;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void FixedUpdate () {

	}
	

	void OnDrawGizmos()
	{
		/* * /
		Gizmos.color = new Color(0f, 0f, 1f);
		if(this.debugPointOnBounds.Count > 0)
		{
			foreach(Vector3 point in this.debugPointOnBounds)
			{
				Gizmos.DrawSphere(point, .2f);
			}
		}
		/* */

		/* * /
		Gizmos.color = new Color(1f, 0f, 0f);
		if(this.debugCollisionPoint.Count > 0)
		{
			foreach(Vector3 point in this.debugCollisionPoint)
			{
				Gizmos.DrawSphere(point, .2f);
			}
		}
		/* */

		/* * /
		Gizmos.color = new Color(1f, 1f, 0f);
		Gizmos.DrawLine(transform.position, transform.position + this.debugLastMoveDeltaMovement*10f);
		/* */

		/* * /
		// Draw sweep cubes
		Gizmos.color = new Color(1f, 0f, 1f);
		Gizmos.DrawWireCube(this.debugSweepTestFromPosition, 1.001f * Vector3.one);
		Gizmos.color = new Color(1f, 1f, 0f);
		Gizmos.DrawWireCube(this.debugSweepTestFromPosition + this.debugSweepTestOffsetDestination, 1.001f * Vector3.one);
		/* */

	}



	public void Move(Vector3 deltaMovement)
	{
		CharacterState state = new CharacterState(transform.position + deltaMovement, Vector3.zero);
		state = this.CalculateMove(state);
		transform.position = state.position;
	}




	public CharacterState CalculateMove(CharacterState state, Func<CollisionEventArgs, CharacterState> collisionAction = null)
	{
		CharacterState initState = new CharacterState(state);
		CharacterState currentState = new CharacterState(state);
		//Debug.Log("Controller Start CalculateMove: " + currentState);

		Vector3 deltaMovement = currentState.position - transform.position;


		#if UNITY_EDITOR
		if(EditorApplication.isPlaying)
			this.debugLastMoveDeltaMovement = deltaMovement;
		#endif
		
		Vector3 beforeTestPosition = transform.position;
		// Move the rigidbody away from the way we are traveling so we make sure we go through the normals of the collider
		//transform.position += -1f * deltaMovement.normalized * this.SkinWidth;
		Vector3 skinOffset = Vector3.zero;
		skinOffset += Mathf.Abs(deltaMovement.x) > 0f ? -1f * Mathf.Sign(deltaMovement.x) * Vector3.right * this.SkinWidth : Vector3.zero;
		skinOffset += Mathf.Abs(deltaMovement.y) > 0f ? -1f * Mathf.Sign(deltaMovement.y) * Vector3.up * this.SkinWidth : Vector3.zero;
		skinOffset += Mathf.Abs(deltaMovement.z) > 0f ? -1f * Mathf.Sign(deltaMovement.z) * Vector3.forward * this.SkinWidth : Vector3.zero;
		
		// Move the rigidbody away from the way we are traveling so we make sure we go through the normals of the collider
		transform.position += skinOffset;

		#if UNITY_EDITOR
		if(EditorApplication.isPlaying)
			this.debugSweepTestFromPosition = transform.position;
		#endif

		Vector3 sweepTargetOffset = (deltaMovement + (-1f * 2f * skinOffset));
		Vector3 sweepDirection = sweepTargetOffset.normalized;
		float sweepDistance = sweepTargetOffset.magnitude;
		#if UNITY_EDITOR
		if(EditorApplication.isPlaying)
			this.debugSweepTestOffsetDestination = sweepDirection * sweepDistance;
		#endif
		
		RaycastHit[] hits = rigidbody.SweepTestAll(sweepDirection, sweepDistance);
		
		// Set back the position so no on will know :P
		transform.position = beforeTestPosition;

		#if UNITY_EDITOR
		if(EditorApplication.isPlaying)
		{
			//Debug.Log("isPlaying: " + Time.time);
			this.debugPointOnBounds = new List<Vector3>();
			this.debugCollisionPoint = new List<Vector3>();
		}
		#endif
		
		
		//Debug.Log("pos: " + transform.position.ToString("f3") + " deltaMovement: " + deltaMovement.magnitude.ToString("f3") + ":" + deltaMovement.ToString("f4") + " sweepTestFrom: " + this.debugSweepTestFromPosition.ToString("f3") + " sweepTestTo: " + (this.debugSweepTestFromPosition + this.debugSweepTestOffsetDestination).ToString("f3") + " : " + this.debugSweepTestOffsetDestination.magnitude.ToString("f3"));
		

		bool hasCollided = false;
		RaycastHit hasCollidedHit = new RaycastHit();
		foreach(RaycastHit hit in hits)
		{
			// Calculate the portion of the deltaMovement that is in the opposite direction of the normal
			Vector3 movementInCollisionDirection = Vector3.Project(deltaMovement, -1f * hit.normal);
			

			float trueHitDistance = Mathf.Clamp(hit.distance-(2f*this.SkinWidth), 0f, hit.distance);
			
			
			
			//Vector3 testOffsetInCollisionDirection = Vector3.Project(skinOffset, -1f * hit.normal);
			//Debug.Log("mycalc hitDistance: " + testOffsetInCollisionDirection.magnitude);
			
			
			// Just make sure we are moving far enough to actually hit
			// Optimization for mesh colliders as they can cause false positives
			bool isTrueHit = movementInCollisionDirection.magnitude > trueHitDistance;


			//Debug.Log(isTrueHit + " Hit::" + " hitNormal: " + hit.normal + " hitDistance: " + hit.distance + " trueHitDistance: " + trueHitDistance + " movementColDirectionDistance: " + movementInCollisionDirection.magnitude + " deltaMovementInColDirection: " + movementInCollisionDirection.ToVerboseString(4, true));
			

			// If true hit or we are within skin range
			// This allows for sliding in another direction other than the direction of collision
			if(isTrueHit || trueHitDistance < this.SkinWidth)
			{
				hasCollided = true;
				hasCollidedHit = hit;


				//Debug.Log("deltaMove: " + deltaMovement.ToDebugString());

				// Remove the movement in the direction we hit
				// We will move right up against the thing we hit a little later on
				deltaMovement -= movementInCollisionDirection;
				//Debug.Log("Removed " + movementInCollisionDirection.ToDebugString() + " velocity in opp. direciton of normal: " + hit.normal);

				// Remove the velocity in the direction we hit
				Vector3 velocityInCollisionDirection = Vector3.Project(Vector3.Project(currentState.velocity, hit.normal), currentState.velocity.normalized);
				//Debug.Log("vel: " + currentState.velocity.ToDebugString());
				//Debug.Log("velincoldir: " + velocityInCollisionDirection.ToDebugString());
				currentState.velocity -= velocityInCollisionDirection;
				//currentState.velocity -= Vector3.Project(currentState.velocity, hit.normal);




				#if UNITY_EDITOR
				if(EditorApplication.isPlaying)
					this.debugCollisionPoint.Add(hit.point);
				#endif
			}
			
			// Move ourselves to the plane we hit
			if(isTrueHit)
			{
				//Debug.Log(hit.point.ToVerboseString(6));

				// This will be a vector 0f or 1f. 
				// 1f if there the movement and collision was in that direction
				// 0f if nothing
				Vector3 collisionMask = new Vector3(movementInCollisionDirection.x != 0f ? 1f : 0f, movementInCollisionDirection.y != 0f ? 1f : 0f, movementInCollisionDirection.z != 0f ? 1f : 0f);
				Vector3 inverseCollisionMask = new Vector3(collisionMask.x > 0f ? 0f : 1f, collisionMask.y > 0f ? 0f : 1f, collisionMask.z > 0f ? 0f : 1f);

				//Debug.Log("mask: " + collisionMask + " inv: " + inverseCollisionMask);

				//deltaMovement += -1f * hit.normal * (trueHitDistance);
				// All movement in not a collidering direction so we don't have to worry about it
				Vector3 deltaInNonCollisionDirection = Vector3.Scale(deltaMovement, inverseCollisionMask);
				// The movement in the colliding direciton. We move right to the wall we collided with within skin distance
				Vector3 deltaInCollisionDirection = Vector3.Scale(hit.point - transform.position + Vector3.one*(Mathf.Clamp(this.SkinWidth-.001f, 0f, this.SkinWidth)), collisionMask);
				deltaMovement = deltaInNonCollisionDirection + deltaInCollisionDirection;
			
				if(collisionMask != Vector3.up)
				{
					Debug.Log(deltaMovement.ToDebugString());
					Debug.Log("mask: " + collisionMask + " inv: " + inverseCollisionMask);
				}
			}
		}

		
		
		//Debug.Log("pos: " + transform.position.ToString("f3") + " deltaMovement: " + deltaMovement.ToVerboseString(3, true) + " deltaMovementDistance: " + deltaMovement.magnitude.ToString("f4") + " sweepTestPos: " + debugSweepTestFromPosition.ToString("f3"));
		
		//Debug.Log("deltaMovement: " + deltaMovement.ToVerboseString(3, true));
		
		
		//Debug.Log("-------");


		// Update the position
		currentState.position = transform.position + deltaMovement;


		// If we collided then see what others want to do
		if(hasCollided)
		{
			// Fire the event
			/* * /
			Noble.Log("fired OnCollision event");
			this.OnCollision(new CollisionEventArgs(initState, currentState, hit));
			// if something has changed after we fired the event
			if(state.velocity != initState.velocity || state.position != initState.position)
			{
				// Update our state to the state change they made
				currentState = new CharacterState(state);
			}
			/* */
			if(collisionAction != null)
			{
				currentState = collisionAction(new CollisionEventArgs(initState, currentState, hasCollidedHit));
			}
		}


		//Debug.Log("Controller End CalculateMove: " + currentState);
		return currentState;
	}




}
