using UnityEngine;

public class Star : MonoBehaviour
{
    public int orderIndex;
    public bool collected;

    public void Collect()
    {
        collected = true;
        gameObject.SetActive(false);
    }

    public void Reset()
    {
        collected = false;
        gameObject.SetActive(true);
    }
}
