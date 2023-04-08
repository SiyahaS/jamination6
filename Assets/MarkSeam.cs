using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkSeam : MonoBehaviour
{
    private FollowMouse _followMouse;
    public float rayLength = 10f;
    [Range(0.1f, 1f)]
    public float seamWidth = 0.3f;

    public Transform seamObject;
    private bool _shouldUpdate = false;
    private Vector2 _from;
    private Vector2 _to;
    private Transform _seam;
    private int _layerMask;

    // Start is called before the first frame update
    void Start()
    {
        _layerMask = LayerMask.GetMask("Sliceable", "Ground");
        _followMouse = GetComponent<FollowMouse>();
        StartCoroutine(IsDirty());
        StartCoroutine(CalculateSeam());
        StartCoroutine(RenderSeam());
    }

    private IEnumerator IsDirty()
    {
        var aimDirection = _followMouse.AimDirection;
        yield return null;
        while (true)
        {
            yield return new WaitUntil(() => aimDirection != _followMouse.AimDirection);
            aimDirection = _followMouse.AimDirection;
            _shouldUpdate = true;
        }
    }

    private IEnumerator CalculateSeam()
    {
        Vector3 aimDirection;
        while (true)
        {
            yield return new WaitUntil(() => _shouldUpdate);
            aimDirection = _followMouse.AimDirection;

            var pos = transform.position;

            var hit = Physics2D.Raycast(pos, aimDirection, rayLength,_layerMask);

            if (hit.collider != null && hit.collider.CompareTag("Sliceable"))
            {
                // TODO add collider point check using https://gist.github.com/sinbad/68cb88e980eeaed0505210d052573724
                // A better explanation https://stackoverflow.com/a/565282
                _from = hit.point;
                seamObject.parent = hit.collider.transform;
                switch (hit.collider)
                {
                    case PolygonCollider2D polygonCollider2D:
                        var path = polygonCollider2D.GetPath(0);
                        for (int i = 0; i < path.Length; i++)
                        {
                            var p1 = path[i];
                            var p2 = path[(i + 1) % path.Length];
                            var doesIntersect = LineUtil.RayLineSegmentIntersection2D(pos, aimDirection, p1, p2, out var intersectsAt);

                            if (doesIntersect)
                            {
                                _to = (_to - _from).magnitude < (intersectsAt - _from).magnitude ? intersectsAt : _to;
                            }
                        }
                        break;
                    default:
                        Debug.Log("Intersected with other than polygon collider: " + hit.collider.GetType());
                        break;
                }
                
                Debug.DrawLine(_from, _to, Color.green, 3f);

                var temp = _to - _from;
                seamObject.localScale = new Vector3(seamWidth, temp.magnitude);
                seamObject.up = temp;
                seamObject.position = _from;
            }

            if (aimDirection == _followMouse.AimDirection)
            {
                _shouldUpdate = false;
            }
        }
    }

    private IEnumerator RenderSeam()
    {
        yield return null;
    }
}
