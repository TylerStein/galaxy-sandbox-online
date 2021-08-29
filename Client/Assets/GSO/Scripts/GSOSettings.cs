using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GSO Settings")]
public class GSOSettings : ScriptableObject
{
    [SerializeField] public float maxVelocity = 100f;
    [SerializeField] public int targetFramerate = 60;
    [SerializeField] public float massSizeMultiplier = 10;
    [SerializeField] public float timeScaleMultiplier = 3.75f;
    [SerializeField] public float gravityConstant = 2f;
    [SerializeField] public float bounds = 100f;
    [SerializeField] public float absorbRate = 0.15f;
}
