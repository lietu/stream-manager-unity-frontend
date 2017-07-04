using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestoreObject : MonoBehaviour {
    public GameObject restoreObject;
    public float restoreAt;

    private bool restored = false;
    
	void Update () {
		if (!this.restored && Time.time > this.restoreAt)
        {
            this.restoreObject.SetActive(true);
            this.restored = true;
        }
	}
}
