using UnityEngine;

public class Respawn : MonoBehaviour
{
    private Rigidbody _rb;
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Respawn"))
        {
            Vector3 currentEuler = _rb.rotation.eulerAngles;
            
            if (Mathf.Abs(currentEuler.z) > 60f && Mathf.Abs(currentEuler.z) < 300f)
            {
                _rb.position += Vector3.up * 5;
                _rb.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
            }
        }
    }
}
