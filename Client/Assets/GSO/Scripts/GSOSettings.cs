using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GSO Settings")]
public class GSOSettings : ScriptableObject
{
    [SerializeField] public float maxVelocity = 100f;
    [SerializeField] public int targetFramerate = 60;
}
