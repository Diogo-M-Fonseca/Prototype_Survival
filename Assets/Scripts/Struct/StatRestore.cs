using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct StatRestore
{
    [SerializeField] private RestoreStat _stat;
    [SerializeField, Min(0)] private int _amount;

    public RestoreStat Stat => _stat;
    public int Amount => _amount;
}
