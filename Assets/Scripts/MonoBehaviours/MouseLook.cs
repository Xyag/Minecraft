/*
*   SCRIPT TAKEN FROM http://wiki.unity3d.com/index.php/SmoothMouseLook
*   ORIGINAL AUTHOR: asteins
*
*   I modified the script to remove the smoothing part and the modified locking to only the x/y axis.
*   Since I remove the smoothing part, I changed the name to just MouseLook. The original name is SmoothMouseLook
*/


using UnityEngine;
public class MouseLook : MonoBehaviour
{
    public float sensitivityX = 15F;
    public float sensitivityY = 15F;

    public enum Axes { X, Y, XandY }
    public Axes LookDirection;

    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    private float rotationX = 0F;
    private float rotationY = 0F;
    private Quaternion originalRotation;

    void Update()
    {

        rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
        rotationX += Input.GetAxis("Mouse X") * sensitivityX;

        rotationX = ClampAngle(rotationX, minimumX, maximumX);
        rotationY = ClampAngle(rotationY, minimumY, maximumY);
        Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);
        Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);

        switch (LookDirection)
        {
            case Axes.X:
                transform.localRotation = originalRotation * xQuaternion;
                break;
            case Axes.Y:
                transform.localRotation = originalRotation * yQuaternion;
                break;
            case Axes.XandY:
                transform.localRotation = originalRotation * xQuaternion * yQuaternion;
                break;
        }

    }

    void Start()
    {
        originalRotation = transform.localRotation;
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        angle = angle % 360;
        if ((angle >= -360F) && (angle <= 360F))
        {
            if (angle < -360F)
            {
                angle += 360F;
            }
            if (angle > 360F)
            {
                angle -= 360F;
            }
        }
        return Mathf.Clamp(angle, min, max);
    }
}