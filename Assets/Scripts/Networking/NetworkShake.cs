using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkShake : MonoBehaviour
{
    private Camera camera;
    private Animator camAnim;

    void Start()
    {
        camera = FindObjectOfType<Camera>();
        camAnim = camera.GetComponent<Animator>();
    }

    public void CamShake()
    {
        camAnim.SetTrigger("shake");
    }
}

