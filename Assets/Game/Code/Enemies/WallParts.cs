using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallParts : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(Setactive(3));
    }
    void Update()
    {
        
    }

    IEnumerator Setactive(float time) {
        yield return new WaitForSeconds(time);
        gameObject.SetActive(false);
    }
}
