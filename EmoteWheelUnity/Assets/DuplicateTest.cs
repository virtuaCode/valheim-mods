using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuplicateTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var child = transform.GetChild(0);

        for(int i = 0; i < 2000; i++)
        {
            Instantiate(child, transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
