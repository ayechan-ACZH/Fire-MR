using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisBounds : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    Renderer r = GetComponent<Renderer>();
    if (r == null) return;

    Bounds b = r.bounds;

    Vector3 c = b.center;
    Vector3 e = b.extents;

    Debug.DrawLine(c + new Vector3(-e.x, -e.y, -e.z), c + new Vector3(e.x, -e.y, -e.z), Color.red);
    Debug.DrawLine(c + new Vector3(e.x, -e.y, -e.z), c + new Vector3(e.x, -e.y, e.z), Color.red);
    // (Continue drawing all 12 edges if needed)
    }

}
