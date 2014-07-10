using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SuperBounceController))]
public class SuperBounceControllerLabel : Editor {
	
	GUISkin editorSkin;
	
	GameObject targetGameObject; // The GameObject that this editor uses
	SuperBounceController scriptOfOurType; // The script we add the custom editor for
	RigidbodyCharacterDriver characterDriver;
	
	void OnEnable()
	{
		this.editorSkin = (GUISkin)(Resources.LoadAssetAtPath("Assets/Editor/EditorGUISkin.guiskin", typeof(GUISkin)));
		
		this.scriptOfOurType = (SuperBounceController)target;  
		this.targetGameObject = (GameObject)this.scriptOfOurType.gameObject;

		this.characterDriver = this.scriptOfOurType.characterDriver;
	}
	
	void OnSceneGUI ()
	{
		string velocityString = "";
		string horDistanceAwayString = "";
		string vertDistanceAwayString = "";

		if(this.characterDriver != null && this.characterDriver.rigidbody != null)
		{
			velocityString = "Velocity: " + (this.characterDriver.rigidbody.velocity.ToVerboseString(3, true));
		}

		if(this.scriptOfOurType != null)
		{
			bool inHorzRange = this.scriptOfOurType.debugHorizontalDistanceFromPoint < this.scriptOfOurType.horizontalDistanceLimit;
			horDistanceAwayString = "Hor. Dist: " + (inHorzRange ? "<color=#00ff00ff>" : "") + this.scriptOfOurType.debugHorizontalDistanceFromPoint + (inHorzRange ? "</color>" : "");
			bool inVertRange = this.scriptOfOurType.debugVerticalDistanceFromPoint < this.scriptOfOurType.verticalDistanceLimit;
			vertDistanceAwayString = "Vert. Dist: " + (inVertRange ? "<color=#00ff00ff>" : "") + this.scriptOfOurType.debugVerticalDistanceFromPoint + (inVertRange ? "</color>" : "");
		}

		Handles.Label(this.targetGameObject.transform.position, velocityString + "\n" + horDistanceAwayString + "\n" + vertDistanceAwayString, editorSkin.GetStyle("Label"));
	}
}
