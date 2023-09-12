using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    public Camera cam;
    public GameObject gObj;
    
    void Update()
    {
        RotateCamera();
    }
    
    void RotateCamera()
    {
        if(Input.GetMouseButton(1))
        {
            cam.transform.RotateAround(gObj.transform.position, 
                cam.transform.up,
                -Input.GetAxis("Mouse X")*2f);

            cam.transform.RotateAround(gObj.transform.position, 
                cam.transform.right,
                Input.GetAxis("Mouse Y")*2f);
        }
    }
}
