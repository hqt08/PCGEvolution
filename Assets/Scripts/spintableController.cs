using UnityEngine;
using System.Collections;

public class spintableController : MonoBehaviour {

	public float rotationalSpeed = 10f;

	// Use this for initialization
	void Start () {
		//iTween.RotateBy(gameObject, iTween.Hash("y", rotationalSpeed, "looptype", iTween.LoopType.loop, "easetype", iTween.EaseType.linear));
		//GetComponentInChildren<Rigidbody>().AddTorque(0, rotationalSpeed, 0);
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		GetComponentInChildren<Rigidbody>().angularVelocity = new Vector3(0,rotationalSpeed,0);
	}
}
