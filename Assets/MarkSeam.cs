using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class MarkSeam : MonoBehaviour
{
    private FollowMouse _followMouse;

    public float rayLength = 10f;

    // [Range(0.1f, 1f)]
    public float seamWidth = 0.3f;

    public Transform seamObject;
    private bool _shouldUpdate = false;
    private Vector2 _from;
    private Vector2 _to;
    private Transform _seam;
    private int _layerMask;
    public GameObject go;
    private int i1;
    private int i2;

    // Start is called before the first frame update
    void Start()
    {
        _layerMask = LayerMask.GetMask("Sliceable", "Ground");
        _followMouse = GetComponent<FollowMouse>();
        StartCoroutine(IsDirty());
        StartCoroutine(CalculateSeam());
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

            var hit = Physics2D.Raycast(pos, aimDirection, rayLength, _layerMask);

            if (hit.collider != null && hit.collider.CompareTag("Sliceable"))
            {
                // TODO add collider point check using https://gist.github.com/sinbad/68cb88e980eeaed0505210d052573724
                // A better explanation https://stackoverflow.com/a/565282
                _from = hit.point;
                _to = _from;
                seamObject.parent = hit.collider.transform;

                switch (hit.collider)
                {
                    case PolygonCollider2D polygonCollider2D:
                        var path = polygonCollider2D.GetPath(0);
                        for (int i = 0; i < path.Length; ++i)
                        {
                            path[i] = RotateAroundPivot(path[i], hit.transform.position, hit.transform.rotation);
                        }

                        for (int i = 0; i < path.Length; i++)
                        {
                            var p1 = path[i];
                            var p2 = path[(i + 1) % path.Length];
                            var doesIntersect =
                                LineUtil.RayLineSegmentIntersection2D(pos, aimDirection, p1, p2, out var intersectsAt);

                            if (doesIntersect)
                            {
                                i1 = i2;
                                i2 = i;
                                _to = Vector2.Distance(_from, _to) < Vector2.Distance(_from, intersectsAt)
                                    ? intersectsAt
                                    : _to;
                            }
                        }

                        if (Input.GetMouseButtonDown(0) && i1 != -1 && i2 != -1)
                        {
                            // consider an array with length l such as [0, 1, 2, ..., n, n+1, n+2, ..., m, m+1, m+2, ..., l]
                            // nth and mth indexes are starting vertex of intersecting edges.
                            // so the first half is [0, .., n, m, .., l]
                            // and the second half is [n, n+1, .., m]
                            var hitObject = hit.transform.gameObject;
                            var hitSpriteRenderer = hitObject.GetComponentInChildren<SpriteRenderer>();
                            for (int i = 0; i < hitSpriteRenderer.sprite.uv.Length; i++)
                            {
                                var v1 = hitSpriteRenderer.sprite.uv[i];
                                var v2 = hitSpriteRenderer.sprite.uv[(i + 1) % hitSpriteRenderer.sprite.uv.Length];
                                Debug.DrawLine(v1, v2, Color.blue, 10f);
                            }
                            for (int i = 0; i < hitSpriteRenderer.sprite.vertices.Length; i++)
                            {
                                var v1 = hitSpriteRenderer.sprite.vertices[i];
                                var v2 = hitSpriteRenderer.sprite.vertices[(i + 1) % hitSpriteRenderer.sprite.vertices.Length];
                                var offset = new Vector2(3f, 0);
                                Debug.DrawLine(v1+offset, v2 + offset, Color.red, 10f);
                            }
                            for (int i = 0; i < hitSpriteRenderer.sprite.triangles.Length; i+=3)
                            {
                                var v1 = hitSpriteRenderer.sprite.vertices[i];
                                var v2 = hitSpriteRenderer.sprite.vertices[i+1];
                                var v3 = hitSpriteRenderer.sprite.vertices[i+2];
                                var offset = new Vector2(-3f, 0);
                                Debug.DrawLine(v1+offset, v2+offset, Color.magenta, 10f);
                                Debug.DrawLine(v2+offset, v3+offset, Color.magenta, 10f);
                                Debug.DrawLine(v3+offset, v1+offset, Color.magenta, 10f);
                            }


                            var leftHalf = new Vector2[i1 + 2 + path.Length - i2];
                            var rightHalf = new Vector2[-i1 + 2 + i2];
                            Array.Copy(path, 0, leftHalf, 0, i1+1);
                            leftHalf[i1 + 1] = _from;
                            leftHalf[i1 + 2] = _to;
                            if (path.Length > i2 + 2)
                            {
                                Array.Copy(path, i2 + 1, leftHalf, i1 + 3, path.Length - i2 - 1);
                            }

                            // Debug.Log(
                            //     $"i1: {i1}, i2: {i2}, 0..i1: [{path.Take(i1).Aggregate("", (acc, e) => $"{acc},{e}")}], _from: {_from}, _to: {_to}, i1+2..n: {path.Skip(i2 + 1).Take(path.Length - i2 - 1).Aggregate("", (acc, e) => $"{acc},{e}")}");
                            // Debug.Log($"Left Half: {leftHalf.Aggregate("", (acc, e) => $"{acc},{e}")}");

                            rightHalf[0] = _from;
                            Array.Copy(path, i1 + 1, rightHalf, 1, i2 - i1);
                            rightHalf[i2 - i1 + 1] = _to;
                            // Debug.Log(
                            //     $"i1: {i1}, i2: {i2}, _from: {_from}, 1..i1: [{path.Skip(i1 + 1).Take(i2 - i1).Aggregate("", (acc, e) => $"{acc},{e}")}], _to: {_to}");
                            // Debug.Log($"Right Half: {rightHalf.Aggregate("", (acc, e) => $"{acc},{e}")}");
                            //
                            // for (int i = 0; i < leftHalf.Length; i++)
                            // {
                            //     var v1 = leftHalf[i];
                            //     var v2 = leftHalf[(i + 1) % leftHalf.Length];
                            //     var offset = new Vector2(0.1f, 0);
                            //     var dot = Instantiate(go, v1*5, Quaternion.identity);
                            //     dot.GetComponent<SpriteRenderer>().color = Color.red;
                            //     dot.GetComponentInChildren<TextMeshPro>().text = $"{v1}\ni: {i}";
                            //     Debug.DrawLine(v1 + offset, v2 + offset, Color.red, 10f);
                            // }
                            //
                            // for (int i = 0; i < rightHalf.Length; i++)
                            // {
                            //     var v1 = rightHalf[i];
                            //     var v2 = rightHalf[(i + 1) % rightHalf.Length];
                            //     var offset = new Vector2(0.1f, 0);
                            //     var dot = Instantiate(go, v1*5, Quaternion.identity);
                            //     dot.GetComponent<SpriteRenderer>().color = Color.blue;
                            //     dot.GetComponentInChildren<TextMeshPro>().text = $"{v1}\ni: {i}";
                            //     Debug.DrawLine(v1 - offset, v2 - offset, Color.blue, 10f);
                            // }
                            //
                            // for (int i = 0; i < path.Length; i++)
                            // {
                            //     var v1 = path[i];
                            //     var v2 = path[(i + 1) % rightHalf.Length];
                            //     var offset = new Vector2(0.1f, 0);
                            //     var dot = Instantiate(go, v1*4, Quaternion.identity);
                            //     dot.GetComponent<SpriteRenderer>().color = Color.green;
                            //     dot.GetComponentInChildren<TextMeshPro>().text = $"{v1}\ni: {i}";
                            // }

                            var leftObject = new GameObject("left", typeof(Rigidbody2D), typeof(SortingGroup));
                            var leftPolygonCollider2D = leftObject.AddComponent<PolygonCollider2D>();
                            polygonCollider2D.tag = "Slicable";
                            leftPolygonCollider2D.points = leftHalf;
                            // var bounds = hit.collider.bounds;
                            leftObject.transform.position = hit.transform.position;
                            leftObject.transform.up = hit.transform.up;
                            // leftRenderer.sprite.OverrideGeometry(leftHalf, leftPolygonCollider2D.);
                            var rightObject = new GameObject("right", typeof(Rigidbody2D), typeof(SortingGroup));
                            var rightPolygonCollider2D = rightObject.AddComponent<PolygonCollider2D>();
                            rightPolygonCollider2D.points = rightHalf;
                            rightObject.transform.position = hit.transform.position;
                            rightObject.transform.up = hit.transform.up;
                            hit.transform.gameObject.SetActive(false);
                        }

                        break;
                    default:
                        Debug.Log("Intersected with other than polygon collider: " + hit.collider.GetType());
                        break;
                }

                // Debug.DrawRay(_from, Vector3.right * 0.1f, Color.blue, 1f);
                // Debug.DrawRay(_from, Vector3.left * 0.1f, Color.blue, 1f);
                // Debug.DrawRay(_from, Vector3.up * 0.1f, Color.blue, 1f);
                // Debug.DrawRay(_from, Vector3.down * 0.1f, Color.blue, 1f);
                // Debug.DrawRay(_to, Vector3.right * 0.1f, Color.red, 1f);
                // Debug.DrawRay(_to, Vector3.left * 0.1f, Color.red, 1f);
                // Debug.DrawRay(_to, Vector3.up * 0.1f, Color.red, 1f);
                // Debug.DrawRay(_to, Vector3.down * 0.1f, Color.red, 1f);
                // Debug.DrawLine(_from, _to, Color.green, 1f);

                var temp = _to - _from;
                seamObject.localScale = new Vector3(seamWidth, Vector2.Distance(_from, _to) + seamWidth);
                seamObject.up = temp;
                var t2 = RotateAroundPivot(_from + (_to - _from) / 2, hit.transform.position,
                    Quaternion.Inverse(hit.transform.rotation));
                seamObject.localPosition = t2;
            }
            else
            {
                seamObject.localScale = Vector3.zero;
                i1 = -1;
                i2 = -1;
            }

            if (aimDirection == _followMouse.AimDirection)
            {
                _shouldUpdate = false;
            }
        }
    }

    private void SplitShape(Vector2 from, Vector2 to)
    {
    }

    private Vector2 RotateAroundPivot(Vector2 point, Vector2 pivot, Quaternion angle)
    {
        Vector2 dir = point - pivot;
        dir = angle * dir;
        point = dir + pivot;
        return point;
    }
}