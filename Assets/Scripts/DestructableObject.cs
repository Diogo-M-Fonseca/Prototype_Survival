using UnityEngine;

public class DestructableObject : MonoBehaviour, IDestroyable
{
    [SerializeField] private int _durability = 30;

    public void TakeDamage(int damage)
    {
        _durability -= damage;
        if (_durability <= 0)
        {
            DestroyObject();
        }
    }

    private void DestroyObject()
    {
        // Spawn base materials code here
        Debug.Log("DestructableObject destroyed");
        Destroy(gameObject);
    }
}
