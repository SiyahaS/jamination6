using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    private Camera _camera;

    private Vector3 _aimDirection;
    public Vector3 AimDirection => _aimDirection;

    private void Start()
    {
        _camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        var pos = _camera.ScreenToWorldPoint(Input.mousePosition);
        pos.z = transform.position.z;
        _aimDirection = (pos - transform.position).normalized;
        transform.up = pos;
    }
}
