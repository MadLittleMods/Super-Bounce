using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SuperBounceController : MonoBehaviour 
{
	public enum WhichCharacterDriver {
		RigidbodyCharacterDriver, CharacterDriver
	}

	public WhichCharacterDriver whichCharacterDriver;

	public RigidbodyCharacterDriver rigidbodyCharacterDriver;
	public CharacterDriver characterDriver;

	// Determines if we can bounce if we hit a mesh edge
	// Look into Halo 2 super bouncing if you are unsure what this exactly means
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

	public bool shouldPlayDebugSoundEffects = false;
	public AudioBase debugVSoundEffect;
	public AudioBase debugHSoundEffect;
	

	public float horizontalDistanceLimit = .1f;
	public float verticalDistanceLimit = .08f*2f + .01f;




	Vector3 debugBounceClosestPointOnEdge = Vector3.zero;
	[HideInInspector]
	public float debugHorizontalDistanceFromPoint = 0f;
	[HideInInspector]
	public float debugVerticalDistanceFromPoint = 0f;



	// Use this for initialization
	void Start () 
	{
		if(this.rigidbodyCharacterDriver != null)
		{
			this.rigidbodyCharacterDriver.OnGroundCollision += this.HandleCollision;

		}
		else
			Debug.LogWarning("Missing RigidBodyCharacterDriver in SuperBounceController", this);

		if(this.characterDriver != null)
		{
			this.characterDriver.OnGroundCollision += this.HandleCollision;
		}
		else
			Debug.LogWarning("Missing RigidBodyCharacterDriver in SuperBounceController", this);
	}
	
	// Update is called once per frame
	void Update ()
	{
		// For now a crouch just has to have the shift being held down
		this.HasCrouch = Input.GetKey(KeyCode.LeftShift);
		//this.HandleSuperBounce(new KinematicRigidbodyCharacterController.CollisionEventArgs());

	}

	public void HandleCollision(CharacterState characterState, RaycastHit hit)
	{
		this.debugBounceClosestPointOnEdge = Vector3.zero;

		//Debug.Log("Collision");

		/* */
		if(this.HasCrouch)
		{

			if(hit.collider != null && hit.collider.gameObject != null)
			{

				float playerMomentum = Mathf.Abs(characterState.Velocity.y);
				float momentumMinimum = this.whichCharacterDriver == WhichCharacterDriver.RigidbodyCharacterDriver ? (this.rigidbodyCharacterDriver != null ? this.rigidbodyCharacterDriver.CalculateJumpVerticalSpeed(this.rigidbodyCharacterDriver.JumpHeight) : 5f) : (this.characterDriver != null ? this.characterDriver.CalculateJumpVerticalSpeed(this.characterDriver.JumpHeight) : 5f);
				//Debug.Log(playerMomentum + " " + momentumMinimum);
				
				// Make sure we are going faster than a normal jump
				if(playerMomentum > momentumMinimum)
				{
					this.HandleSuperBounce(hit.collider.gameObject, characterState);
				}

			}


		}
		/* */

	}

	void HandleSuperBounce(GameObject go, CharacterState current)
	{
		//CharacterState currentState = new CharacterState(current);

		//Debug.Log("Handling Bounce");
		
		//Debug.Log(currentState.position.ToDebugString());
		
		// Keep track of how many bounces were made
		int numBouncesSoFar = 0;


		float playerMomentum = Mathf.Abs(current.Velocity.y);
		
		// Find the nearest vertices
		// Even this might contain multiple points, they are all in the same position
		Dictionary<MeshFilter, List<IndexPointPair>> verticeDictionary = EdgeDetection.NearestVerticesTo(go, current.Position, .2f);
		// Loop through all the nearest vertices
		foreach(KeyValuePair<MeshFilter, List<IndexPointPair>> meshPointPair in verticeDictionary)
		{
			foreach(IndexPointPair vert in meshPointPair.Value)
			{
				DebugGizmos.DrawWireSphere(vert.point, .2f, Color.magenta, 2f);

				// Loop through all of the connected edges to that vert
				var edges = EdgeDetection.ConnectedEdgesTo(meshPointPair.Key.gameObject, vert);
				foreach(Vector3[] edge in edges)
				{
					// Loop through each segment of the edge. Should be only 1 edge from 0index to 1index
					for(int edgeIndex = 0; edgeIndex < edge.Length-1; edgeIndex++)
					{
						Vector3 closestPointOnEdge = EdgeDetection.ClosestPointOnSegment(edge[edgeIndex], edge[edgeIndex+1], current.Position);
						this.debugBounceClosestPointOnEdge = closestPointOnEdge;
						
						//float distanceAway = Vector3.Distance(closestPointOnEdge, currentState.position);
						float horizontalDistanceAway = Vector3.Distance(Vector3.Scale(closestPointOnEdge, new Vector3(1f, 0f, 1f)), Vector3.Scale(current.Position, new Vector3(1f, 0f, 1f)));
						float verticalDistanceAway = Vector3.Distance(Vector3.Scale(closestPointOnEdge, new Vector3(0f, 1f, 0f)), Vector3.Scale(current.Position, new Vector3(0f, 1f, 0f)));
						//Debug.Log(Vector3.Scale(closestPointOnEdge, new Vector3(0f, 1f, 0f)).ToVerboseString() + " " + Vector3.Scale(currentState.position, new Vector3(0f, 1f, 0f)).ToVerboseString());
						//Debug.Log("h: " + horizontalDistanceAway + " v: " + verticalDistanceAway);
						this.debugHorizontalDistanceFromPoint = horizontalDistanceAway;
						this.debugVerticalDistanceFromPoint = verticalDistanceAway;

						// Some debug sound effects for determining whether you are in range of the edge
						if(this.shouldPlayDebugSoundEffects)
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
							DebugGizmos.DrawWireSphere(closestPointOnEdge, .2f, Color.green, 2f);

							//Debug.Log(playerMomentum);

							// Counteract any gravity
							// Only add it once
							float counteractNegativeYVelocity = Mathf.Abs(current.Velocity.y);


							//counteractNegativeYVelocity = (rigidbody.velocity.y < 0f ? Mathf.Abs(rigidbody.velocity.y) : 0f);



							// Add half as much as before for every edge we "hit"
							// This is so that we don't go super high just for hitting a junction
							Vector3 bounceVelocity = (-1f*Physics.gravity.normalized) * ((playerMomentum * Mathf.Pow(.5f, numBouncesSoFar)) + (numBouncesSoFar == 0 ? counteractNegativeYVelocity : 0f));

							if(this.whichCharacterDriver == WhichCharacterDriver.RigidbodyCharacterDriver)
							{
								rigidbody.velocity += bounceVelocity;
							}
							else if(this.whichCharacterDriver == WhichCharacterDriver.CharacterDriver)
							{
								//Debug.Log("current: " + current.Velocity + " bounce: " + bounceVelocity);
								this.characterDriver.AddVelocity(bounceVelocity);
							}
							
							numBouncesSoFar++;
							
						}
						else
							DebugGizmos.DrawWireSphere(closestPointOnEdge, .2f, Color.red, 2f);
						
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


	}


	void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawSphere(transform.position, .1f);

		// Draw the bounce point
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(this.debugBounceClosestPointOnEdge, .05f);
	}




}
