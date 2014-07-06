using UnityEngine;

public class CharacterState
{
	public Vector3 position = Vector3.zero;
	
	public Vector3 velocity = Vector3.zero;

	// Like input added every frame
	// This is just a nice variable to see if the character moved
	public Vector3 instantVelocity = Vector3.zero;
	
	public CharacterState()
	{
		
	}
	
	public CharacterState(CharacterState s)
	{
		this.position = s.position;
		this.velocity = s.velocity;
		this.instantVelocity = s.instantVelocity;
	}
	
	public CharacterState(Vector3 position, Vector3 velocity)
	{
		this.position = position;
		
		this.velocity = velocity;
	}
	
	public static CharacterState Lerp(CharacterState from, CharacterState to, float t)
	{
		return new CharacterState(Vector3.Lerp(from.position, to.position, t), Vector3.Lerp(from.velocity, to.velocity, t));
	}
	
	public static implicit operator string(CharacterState s)
	{
		return s.ToString();
	}
	
	
	public override string ToString()
	{
		return "p: " + this.position + " v: " + this.velocity;
	}
}