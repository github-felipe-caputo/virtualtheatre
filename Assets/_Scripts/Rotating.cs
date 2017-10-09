using UnityEngine;
using System.Collections;

public class Rotating : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        transform.Translate(new Vector3(10, 0, 0) * Time.deltaTime, Space.World);
        transform.Rotate(new Vector3(25, 30, 40) * Time.deltaTime);
        //transform.Rotate(new Vector3(25, 30, 40) * Time.deltaTime);
    }
}
