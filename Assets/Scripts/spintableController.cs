using UnityEngine;
using System.Collections;

public class spintableController : MonoBehaviour {

	public float rotationalSpeed = 100f;

	// Use this for initialization
	void Start () {
		//iTween.RotateBy(gameObject, iTween.Hash("y", rotationalSpeed, "looptype", iTween.LoopType.loop, "easetype", iTween.EaseType.linear));
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		GetComponentInChildren<Rigidbody>().AddTorque(0, rotationalSpeed, 0);
	}
}
