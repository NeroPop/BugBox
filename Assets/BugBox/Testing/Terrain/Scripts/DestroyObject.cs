using UnityEngine;

public class DestroyObject : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        //Destroy(other.gameObject);

        other.gameObject.SetActive(false); //disables instead of destroys to keep the object in the scene as reference
    }
}
