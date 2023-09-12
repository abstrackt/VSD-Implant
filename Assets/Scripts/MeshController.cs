using System.Collections.Generic;
using UnityEngine;

public class MeshController : MonoBehaviour
{
    public MeshFilter[] meshes;
    public float sphereSize;
    private GameObject _sphere;
    private Camera _camera;

    private int _startMeshIdx;
    private int _startVertIdx;
    private Vector3 _startMousePos;
    private Vector3 _startWorldPos;
    private Vector3 _startHitPos;
    private Vector3 _startNormal;
    private bool _held;

    private readonly List<Vector3[]> _vertices = new List<Vector3[]>();
    
    void Start()
    {
        _camera = Camera.main;
        _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere.transform.localScale = Vector3.one * 0.025f;

        for (int i = 0; i < meshes.Length; i++)
        {
            _vertices.Add(meshes[i].sharedMesh.vertices);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _startMousePos = Input.mousePosition;
            
            var ray = _camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit))
            {
                var minDist = float.MaxValue;
                var minVert = -1;
                var minMesh = -1;

                for (int j = 0; j < meshes.Length; j++)
                {
                    var p = meshes[j].transform.InverseTransformPoint(hit.point);
                    var verts = meshes[j].mesh.vertices;
                
                    for (int i = 0; i < verts.Length; i++)
                    {
                        var v = verts[i];

                        var dist = (p - v).sqrMagnitude;
                
                        if (dist < minDist)
                        {
                            minDist = dist;
                            minVert = i;
                            minMesh = j;
                        }
                    }
                }
            
                if (minVert != -1)
                {
                    var v = meshes[minMesh].mesh.vertices[minVert];
                    var n = meshes[minMesh].mesh.normals[minVert];
                    _startWorldPos = meshes[minMesh].transform.TransformPoint(v);
                    _startNormal = meshes[minMesh].transform.TransformVector(n);
                    _startMeshIdx = minMesh;
                    _startVertIdx = minVert;
                }
            }
        }
        
        var mouse = Input.mousePosition - _startMousePos;
        
        if (Input.GetMouseButton(0))
        {
            if (mouse != Vector3.zero)
            {
                var p1 = _camera.WorldToScreenPoint(_startWorldPos);
                var p2 = _camera.WorldToScreenPoint(_startWorldPos + _startNormal);

                var normal = (p2 - p1).normalized;

                for (int j = 0; j < meshes.Length; j++)
                {
                    var startPos = meshes[j].transform.InverseTransformPoint(_startWorldPos);
                    var startNormal = meshes[j].transform.InverseTransformVector(_startNormal);
                    
                    var delta = Vector3.Dot(normal, mouse * 0.01f) * startNormal;
                    
                    var mesh = meshes[j].mesh;
                    var verts = mesh.vertices;
            
                    for (int i = 0; i < mesh.vertexCount; i++)
                    {
                        var v = _vertices[j][i];
                        var r = (v - startPos).magnitude;
                        var ratio = Mathf.SmoothStep(1, 0, r / sphereSize);
                        verts[i] = v + delta * (ratio * 0.1f);
                    }

                    mesh.vertices = verts;
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                }
            }

            var p = meshes[_startMeshIdx].mesh.vertices[_startVertIdx];
            _sphere.transform.position = meshes[_startMeshIdx].transform.TransformPoint(p);
            _sphere.SetActive(true);
        }
        else
        {
            _sphere.SetActive(false);
        }

        if (Input.GetMouseButtonUp(0))
        {
            for (int i = 0; i < meshes.Length; i++)
            {
                _vertices[i] = meshes[i].mesh.vertices;
            }
        }
    }
}
