using UnityEngine;
using System.Collections;

public class MimicChildOf : MonoBehaviour {

	public Transform parentTransform;

	// If true, will attempt to scale the child accurately as the parent scales
	// Will not be accurate if starting rotations are different or irregular
	// Experimental
	public bool attemptChildScale = false;

	Vector3 startParentPosition;
	Quaternion startParentRotationQ;
	Vector3 startParentScale;

	Vector3 startChildPosition;
	Quaternion startChildRotationQ;
	Vector3 startChildScale;

	Matrix4x4 parentMatrix;


	Vector3 prevPosition;
	Quaternion prevRotationQ;


	bool hasInitialized = false;

	void Start () {
		this.Init();
	}

	public void Init()
	{
		if(parentTransform != null)
		{
			startParentPosition = parentTransform.position;
			startParentRotationQ = parentTransform.rotation;
			startParentScale = parentTransform.lossyScale;
			
			startChildPosition = transform.position;
			startChildRotationQ = transform.rotation;
			startChildScale = transform.lossyScale;
			
			// Keeps child position from being modified at the start by the parent's initial transform
			startChildPosition = DivideVectors(Quaternion.Inverse(parentTransform.rotation) * (startChildPosition - startParentPosition), startParentScale);
			
			
			prevPosition = transform.position;
			prevRotationQ = transform.rotation;

			this.hasInitialized = true;
		}
	}
	
	void LateUpdate () {

		if(this.hasInitialized)
		{

			parentMatrix = Matrix4x4.TRS(parentTransform.position, parentTransform.rotation, parentTransform.lossyScale);


			//startChildPosition += (transform.position - prevPosition); // Make sure that if you translate the object, we don't just jump back
			transform.position = parentMatrix.MultiplyPoint3x4(startChildPosition);

			//startChildRotationQ *= Quaternion.Inverse(prevRotationQ)*transform.rotation; // Make sure that if you rotate the object, we don't just jump back
			transform.rotation = (parentTransform.rotation * Quaternion.Inverse(startParentRotationQ)) * startChildRotationQ;

			// Incorrect scale code; it scales the child locally not gloabally; Might work in some cases, but will be inaccurate in others
			if (attemptChildScale) {
				transform.localScale = Vector3.Scale(startChildScale, DivideVectors(parentTransform.lossyScale, startParentScale));
			}

			// Scale code 2; I was working on to scale the child globally through it's local scale, but turned out to be impossible using localScale
			/*
			Vector3 modVec;

			float angleX = Mathf.Abs(Vector3.Angle(transform.right, parentTransform.right));

			modVec.x = Mathf.Abs(angleX - 90) / 90;

			float angleY = Mathf.Abs(Vector3.Angle(transform.up, parentTransform.up));

			modVec.y = Mathf.Abs(angleY - 90) / 90;

			float angleZ = Mathf.Abs(Vector3.Angle(transform.forward, parentTransform.forward));

			modVec.z = Mathf.Abs(angleZ - 90) / 90;

			transform.localScale = Vector3.Scale(startChildScale, Vector3.Scale(DivideVectors(parentTransform.lossyScale, startParentScale), modVec));
			*/

			prevPosition = transform.position;
			prevRotationQ = transform.rotation;

		}


	}

	public void SetRotationThis(Quaternion rot)
	{
		startChildRotationQ = rot;
	}

	public void RotateThis(Quaternion rot)
	{
		startChildRotationQ *= rot;
	}


	Vector3 DivideVectors(Vector3 num, Vector3 den) 
	{

		return new Vector3(num.x / den.x, num.y / den.y, num.z / den.z);

	}
}