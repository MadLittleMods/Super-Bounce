using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SuperBounceController : MonoBehaviour {

	KinematicRigidbodyCharacterDriver characterDriver;
	KinematicRigidbodyCharacterController characterController;

	bool hasCrouch = false;
	public bool HasCrouch
	{
		get {
			return this.hasCrouch;
		}
		private set {
			this.hasCrouch = value;
		}
	}
	

	public AudioBase bounceSoundEffect;

	public bool shouldPlayerDebugSoundEffects = false;
	public AudioBase debugVSoundEffect;
	public AudioBase debugHSoundEffect;
	

	public float horizontalDistanceLimit = .1f;
	public float verticalDistanceLimit = .1f;


	Vector3 debugBounceClosestPointOnEdge = Vector3.zero;
	public float debugHorizontalDistanceFromPoint = 0f;
	public float debugVerticalDistanceFromPoint = 0f;



	// Use this for initialization
	void Start () {
		this.characterDriver = GetComponent<KinematicRigidbodyCharacterDriver>();
		this.characterController = GetComponent<KinematicRigidbodyCharacterController>();

		this.horizontalDistanceLimit = .1f;
		this.verticalDistanceLimit = .01f + (this.characterController != null ? this.characterController.SkinWidth : 0f);

	}
	
	// Update is called once per frame
	void Update () 
	{
		// For now a crouch just has to have the shift being held down
		this.HasCrouch = Input.GetKey(KeyCode.LeftShift);
		//this.HandleSuperBounce(new KinematicRigidbodyCharacterController.CollisionEventArgs());

	}

	public CharacterState HandleSuperBounce(KinematicRigidbodyCharacterController.CollisionEventArgs e)
	{
		this.debugBounceClosestPointOnEdge = Vector3.zero;

		CharacterState beforeState = new CharacterState(e.BeforeCollisionState);
		CharacterState currentState = new CharacterState(e.AfterCollisionState);

		/* */
		if(this.HasCrouch)
		{
			if(e.Hit.collider != null && e.Hit.collider.gameObject != null)
			{

				float playerMomentum = Mathf.Abs(beforeState.velocity.y);
				float momentumMinimum = this.characterDriver != null ? this.characterDriver.CalculateJumpVerticalSpeed(this.characterDriver.JumpHeight) : 5f;
				//Debug.Log(playerMomentum + " " + this.characterDriver.CalculateJumpVerticalSpeed(this.characterDriver.JumpHeight));
				
				// Make sure we are going faster than a normal jump
				if(playerMomentum > momentumMinimum)
				{
					currentState = this.HandleEdgesSuperBounce(e.Hit.collider.gameObject, beforeState, currentState);
				}

			}

		}
		/* */


		return currentState;
	}

	CharacterState HandleEdgesSuperBounce(GameObject go, CharacterState before, CharacterState current)
	{
		CharacterState beforeState = new CharacterState(before);
		CharacterState currentState = new CharacterState(current);

		
		//Debug.Log(currentState.position.ToDebugString());
		
		// Keep track of how many bounces were made
		int numBouncesSoFar = 0;


		float playerMomentum = Mathf.Abs(beforeState.velocity.y);
		
		// Find the nearest vertices
		// Even this might contain multiple points, they are all in the same position
		Dictionary<MeshFilter, List<IndexPointPair>> verticeDictionary = EdgeDetection.NearestVerticesTo(go, currentState.position);
		// Loop through all the nearest vertices
		foreach(KeyValuePair<MeshFilter, List<IndexPointPair>> meshPointPair in verticeDictionary)
		{
			foreach(IndexPointPair vert in meshPointPair.Value)
			{
				// Loop through all of the connected edges to that vert
				var edges = EdgeDetection.ConnectedEdgesTo(meshPointPair.Key.gameObject, vert);
				foreach(Vector3[] edge in edges)
				{
					// Loop through each segment of the edge. Should be only 1 edge from 0index to 1index
					for(int edgeIndex = 0; edgeIndex < edge.Length-1; edgeIndex++)
					{
						Vector3 closestPointOnEdge = EdgeDetection.ClosestPointOnSegment(edge[edgeIndex], edge[edgeIndex+1], currentState.position);
						this.debugBounceClosestPointOnEdge = closestPointOnEdge;
						
						//float distanceAway = Vector3.Distance(closestPointOnEdge, currentState.position);
						float horizontalDistanceAway = Vector3.Distance(Vector3.Scale(closestPointOnEdge, new Vector3(1f, 0f, 1f)), Vector3.Scale(currentState.position, new Vector3(1f, 0f, 1f)));
						float verticalDistanceAway = Vector3.Distance(Vector3.Scale(closestPointOnEdge, new Vector3(0f, 1f, 0f)), Vector3.Scale(currentState.position, new Vector3(0f, 1f, 0f)));
						//Debug.Log(Vector3.Scale(closestPointOnEdge, new Vector3(0f, 1f, 0f)).ToVerboseString() + " " + Vector3.Scale(currentState.position, new Vector3(0f, 1f, 0f)).ToVerboseString());
						//Debug.Log("h: " + horizontalDistanceAway + " v: " + verticalDistanceAway);
						this.debugHorizontalDistanceFromPoint = horizontalDistanceAway;
						this.debugVerticalDistanceFromPoint = verticalDistanceAway;

						// Some debug sound effects for determining whether you are in range of the edge
						if(this.shouldPlayerDebugSoundEffects)
						{
							if(this.debugVSoundEffect != null && verticalDistanceAway < verticalDistanceLimit)
							{
								this.debugVSoundEffect.PlayOneShot();
							}

							if(this.debugHSoundEffect != null && horizontalDistanceAway < horizontalDistanceLimit)
							{
								this.debugHSoundEffect.PlayOneShot();
							}
						}

						//Debug.Log(distanceAway);
						if(horizontalDistanceAway < horizontalDistanceLimit && verticalDistanceAway < verticalDistanceLimit)
						{
							//Debug.Log("bounce");
							
							// Add half as much as before for every edge we "hit"
							// This is so that we don't go super high just for hitting a junction
							currentState.velocity += new Vector3(0f, playerMomentum * Mathf.Pow(.5f, numBouncesSoFar), 0f);
							
							numBouncesSoFar++;
							
						}
						
					}
				}
				
				
			}
		}
		
		
		// Play the sound if we bounced once
		if(numBouncesSoFar > 0)
		{
			// Play the bounce sound
			if(this.bounceSoundEffect != null)
			{
				this.bounceSoundEffect.PlayOneShot();
			}
		}


		return currentState;
	}


	void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawSphere(transform.position, .05f);

		// Draw the bounce point
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(this.debugBounceClosestPointOnEdge, .05f);
	}




}
