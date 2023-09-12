using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;

public class ImplantControlNet : MonoBehaviour
{

    [Header("Bones")]
    public Transform lowerDiscBone;
    public Transform[] connectorBones;
    public Transform upperDiscBone;
    public Transform[] lowerDiscRadialBones;
    public Transform[] upperDiscRadialBones;

    [Header("Params")] 
    [Range(0.4f, 1)] public float upperDiscHeight = 1f;
    [Range(0.4f, 1)] public float lowerDiscHeight = 1f;
    [Range(1, 2)] public float upperDiscRadius = 1f;
    [Range(1, 2)] public float lowerDiscRadius = 1f;
    [Range(0.05f, 2)] public float connectorHeight = 0.2f;

    [Header("UI")] 
    public Slider lowerHeightSlider;
    public Slider upperHeightSlider;
    public Slider lowerRadiusSlider;
    public Slider upperRadiusSlider;
    public Slider connectorSlider;

    private Vector3 _startMousePos;
    private Vector3 _startWorldPos;

    private Camera _camera;
    private GameObject _currentHandle = null;
    private Vector3 _originalVec;
    private Vector3 _originalPos;
    private float _originalZ;
    private bool _radial;

    private VectorLine _line;
    private VectorLine _points;

    private Dictionary<GameObject, Transform> _handleToRadialBone = new();
    private Dictionary<GameObject, Transform> _handleToPivot = new();
    private List<Transform> _transforms;

    void Start()
    {
        _camera = Camera.main;

        _transforms = new List<Transform>
        {
            lowerDiscBone,
            connectorBones[0],
            connectorBones[1],
            connectorBones[2],
            upperDiscBone
        };

        _line = new VectorLine("Axis", _transforms.Select(x => x.position).ToList(), 2f, LineType.Continuous);
        _points = new VectorLine("Points", _transforms.Select(x => x.position).ToList(), 10f, LineType.Points);

        for (int i = 1; i < _transforms.Count; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = _transforms[i];
            sphere.transform.position = _transforms[i].position;
            sphere.transform.localScale = Vector3.one*0.05f;
            _handleToPivot.Add(sphere, _transforms[i - 1]);
            var mr = sphere.GetComponent<MeshRenderer>();
            mr.enabled = false;
        }

        for (int i = 0; i < lowerDiscRadialBones.Length; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = lowerDiscRadialBones[i].GetChild(0).position;
            sphere.transform.parent = lowerDiscRadialBones[i];
            sphere.transform.localScale = Vector3.one*0.05f;
            _handleToRadialBone.Add(sphere, lowerDiscRadialBones[i]);
            var mr = sphere.GetComponent<MeshRenderer>();
            mr.enabled = false;
        }
        
        for (int i = 0; i < upperDiscRadialBones.Length; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = upperDiscRadialBones[i].GetChild(0).position;
            sphere.transform.parent = upperDiscRadialBones[i];
            sphere.transform.localScale = Vector3.one*0.05f;
            _handleToRadialBone.Add(sphere, upperDiscRadialBones[i]);
            var mr = sphere.GetComponent<MeshRenderer>();
            mr.enabled = false;
        }
        
        lowerHeightSlider.value = lowerDiscHeight;
        upperHeightSlider.value = upperDiscHeight;
        lowerRadiusSlider.value = lowerDiscRadius;
        upperRadiusSlider.value = upperDiscRadius;
        connectorSlider.value = connectorHeight;
    }

    void Update()
    {
        lowerDiscHeight = lowerHeightSlider.value;
        upperDiscHeight = upperHeightSlider.value;
        lowerDiscRadius = lowerRadiusSlider.value;
        upperDiscRadius = upperRadiusSlider.value;
        connectorHeight = connectorSlider.value;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        
        var lower = new Vector3(lowerDiscRadius, lowerDiscHeight, lowerDiscRadius);
        lowerDiscBone.localScale = lower;

        var pos = upperDiscBone.localPosition;
        pos.y = connectorHeight / connectorBones.Length;
        upperDiscBone.localPosition = pos;

        for (int i = 0; i < connectorBones.Length; i++)
        {
            pos = connectorBones[i].localPosition;
            if (i == 0)
            {
                pos.z = lowerDiscBone.localPosition.z + connectorHeight / connectorBones.Length;
                pos.y = 0;
            }
            else
            {
                pos.y = connectorHeight / connectorBones.Length;
                pos.z = 0;
            }

            connectorBones[i].localPosition = pos;
        }

        var upper = new Vector3(upperDiscRadius, upperDiscHeight, upperDiscRadius);
        upperDiscBone.localScale = upper;

        _line.points3 = _transforms.Select(x => x.position).ToList();
        _points.points3 = _transforms
            .Select(x => x.position)
            .Concat(upperDiscRadialBones.Select(x => x.GetChild(0).position))
            .Concat(lowerDiscRadialBones.Select(x => x.GetChild(0).position))
            .ToList();

        _line.Draw();
        _points.Draw();

        if (Input.GetMouseButtonDown(0))
        {
            _startMousePos = Input.mousePosition;

            var ray = _camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit))
            {
                _startMousePos = Input.mousePosition;
                _currentHandle = hit.transform.gameObject;
                var handlePos = hit.transform.position;
                if (_handleToPivot.ContainsKey(_currentHandle))
                {
                    _radial = false;
                    _originalVec = handlePos - _handleToPivot[_currentHandle].position;
                    _originalZ = _camera.WorldToScreenPoint(handlePos).z;
                }

                if (_handleToRadialBone.ContainsKey(_currentHandle))
                {
                    _radial = true;
                    _originalVec = handlePos - _handleToRadialBone[_currentHandle].position;
                    _originalPos = _handleToRadialBone[_currentHandle].position;
                }
            }
        }

        var from = _startMousePos;
        from.z = _originalZ;

        var to = Input.mousePosition;
        to.z = _originalZ;

        var delta = _camera.ScreenToWorldPoint(to) - _camera.ScreenToWorldPoint(from);

        if (Input.GetMouseButton(0) && _currentHandle != null && delta != Vector3.zero)
        {
            if (!_radial)
            {
                var newVec = _originalVec + delta * 0.1f;
                var rotation = Quaternion.FromToRotation(_originalVec.normalized, newVec.normalized);

                _handleToPivot[_currentHandle].rotation = rotation;
            }
            else
            {
                var scale = Vector3.Dot(_originalVec.normalized, delta) * 0.1f;

                _handleToRadialBone[_currentHandle].position = _originalPos + scale * _originalVec;
            }
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            _currentHandle = null;
        }
    }
}
