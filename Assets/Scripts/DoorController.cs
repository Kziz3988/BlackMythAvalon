using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour {
	public bool isRotating = false;
	const float deltaTime = 0.75f;

	void Start() {
		gameObject.tag = "DoorClosed";
		transform.Find("SM_Bld_Castle_Door_L").tag = "DoorClosed";
		transform.Find("SM_Bld_Castle_Door_R").tag = "DoorClosed";
	}

	//Animation of opening or closing doors
	public IEnumerator RotateDoor() {
		isRotating = true;
		float time = 0;
		Transform leftDoor = transform.Find("SM_Bld_Castle_Door_L");
		Transform rightDoor = transform.Find("SM_Bld_Castle_Door_R");
		gameObject.tag = "DoorMoving";
		leftDoor.tag = "DoorMoving";
		rightDoor.tag = "DoorMoving";
		float startAngle = leftDoor.localEulerAngles.y;
		float targetAngle = 90 - leftDoor.localEulerAngles.y;
		float deltaAngle = (Mathf.Abs(startAngle) < 1) ? 90 : -90;
		while(deltaTime - time > 0) {
			//Trigonometric interpolation
			leftDoor.localEulerAngles = new Vector3(0, startAngle + deltaAngle * Mathf.Sin(time / (2 * deltaTime) * Mathf.PI), 0);
			rightDoor.localEulerAngles = new Vector3(0, -startAngle - deltaAngle * Mathf.Sin(time / (2 * deltaTime) * Mathf.PI), 0);
			yield return null;
			time += Time.deltaTime;
		}
		leftDoor.localEulerAngles = new Vector3(0, targetAngle, 0);
		rightDoor.localEulerAngles = new Vector3(0, -targetAngle, 0);
		if(Mathf.Abs(startAngle - 0) < 1) { //Open the door
			GetComponent<MeshCollider>().isTrigger = true;
			gameObject.tag = "DoorOpened";
			leftDoor.tag = "DoorOpened";
			rightDoor.tag = "DoorOpened";
		}
		else { //Close the door
			GetComponent<MeshCollider>().isTrigger = false;
			gameObject.tag = "DoorClosed";
			leftDoor.tag = "DoorClosed";
			rightDoor.tag = "DoorClosed";
		}
		isRotating = false;
	}
}
