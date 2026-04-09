using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            Vector3 directionToCamera = Camera.main.transform.position - transform.position;
            directionToCamera.y = 0f;

            if (directionToCamera.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);
        }
    }
}
