using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassthroughBrightness : MonoBehaviour
{


    [SerializeField] private OVRPassthroughLayer _passthroughLayer;

    public float brightness = 0f;
    // Start is called before the first frame update
    void Start()
    {
         _passthroughLayer.SetBrightnessContrastSaturation(brightness);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
