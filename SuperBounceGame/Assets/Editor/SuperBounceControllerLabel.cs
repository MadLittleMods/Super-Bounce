using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SuperBounceController))]
public class SuperBounceControllerLabel : Editor {
	
	GUISkin editorSkin;
	
	GameObject targetGameObject; // The GameObject that this editor uses
	SuperBounceController scriptOfOurType; // The script we add the custom editor for
	KinematicRigidbodyCharacterDriver characterDriver;
	
	void OnEnable()
	{
		this.editorSkin = (GUISkin)(Resources.LoadAssetAtPath("Assets/Editor/EditorGUISkin.guiskin", typeof(GUISkin)));
		
		this.scriptOfOurType = (SuperBounceController)target;  
		this.targetGameObject = (GameObject)this.scriptOfOurType.gameObject;

		this.characterDriver = this.scriptOfOurType.GetComponent<KinematicRigidbodyCharacterDriver>();
	}
	
	void OnSceneGUI ()
	{
		// Get gizmo string jump height
		string velocityString = "Velocity: " + (this.characterDriver.currentState.velocity);
		bool inHorzRange = this.scriptOfOurType.debugHorizontalDistanceFromPoint < this.scriptOfOurType.horizontalDistanceLimit;
		string horDistanceAwayString = "Hor. Dist: " + (inHorzRange ? "<color=#00ff00ff>" : "") + this.scriptOfOurType.debugHorizontalDistanceFromPoint + (inHorzRange ? "</color>" : "");
		bool inVertRange = this.scriptOfOurType.debugVerticalDistanceFromPoint < this.scriptOfOurType.verticalDistanceLimit;
		string vertDistanceAwayString = "Vert. Dist: " + (inVertRange ? "<color=#00ff00ff>" : "") + this.scriptOfOurType.debugVerticalDistanceFromPoint + (inVertRange ? "</color>" : "");

		Handles.Label(this.targetGameObject.transform.position, velocityString + "\n" + horDistanceAwayString + "\n" + vertDistanceAwayString, editorSkin.GetStyle("Label"));
	}
}
