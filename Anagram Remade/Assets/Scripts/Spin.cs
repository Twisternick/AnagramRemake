using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{
    
    [SerializeField]
    private float speed = 1f;

    private void FixedUpdate()
    {
        transform.Rotate(0, 0, speed);
    }
}
