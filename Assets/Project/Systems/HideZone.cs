using UnityEngine;

public class HideZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f); // °ëÍ¸Ã÷
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1f); // ß€Ô­
    }
}
