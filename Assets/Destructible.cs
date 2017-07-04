using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    public GameObject destroyedVersion;
    public bool restore = false;
    public float restoreAfterSec = 5F;
    public float breakJoules = 10F;
    
    private Vector3 initialTransform;
    private Quaternion initialRotation;
    private bool broken = false;
    private GameObject destroyedTemp;
    
    void OnEnable()
    {
        this.broken = false;
        if (this.destroyedTemp)
        {
            Destroy(this.destroyedTemp);
            this.destroyedTemp = null;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactEnergy = this.KineticEnergy(collision);
        Debug.Log("Hit with " + impactEnergy + "J of energy", this.gameObject);
        if (!this.broken && impactEnergy > this.breakJoules)
        {
            this.broken = true;
            this.Break(collision);
        }
    }

    float KineticEnergy(Collision collision)
    {
        // mass in kg, velocity in meters per second, result is joules
        return 0.5f * collision.collider.GetComponent<Rigidbody>().mass * Mathf.Pow(collision.relativeVelocity.magnitude, 2);
    }

    void Break(Collision collision)
    {
        Debug.Log("Breaking object", this.gameObject);

        var go = Instantiate(this.destroyedVersion, this.transform.position, this.transform.rotation);
        if (this.restore)
        {
            this.destroyedTemp = go;
            var ro = this.gameObject.transform.parent.gameObject.AddComponent<RestoreObject>();
            ro.restoreObject = this.gameObject;
            ro.restoreAt = Time.time + this.restoreAfterSec;
            this.gameObject.SetActive(false);
        }
        else
        {
            Destroy(this.gameObject);
        }

        var add = collision.relativeVelocity;
        var orb = collision.collider.GetComponent<Rigidbody>();
        orb.velocity = add;
        //Debug.Log("Added " + add.ToString() + " force to collider
    }
    
}
