using UnityEngine;

[CreateAssetMenu(fileName = "RandomizedStarmap", menuName = "ScriptableObjects/RandomizedStarmap")]
public class RandomizedStarmap : ScriptableObject
{
    public int numberOfLayers;
    public Vector3 starfieldSize;
    public float maxNodeDistance;
    public float minNodeDistance;

    public float maxConnectionDistance;
}