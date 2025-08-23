using UnityEngine;

public class SimpleCoin : MonoBehaviour
{
    public static int total = 0; // Toplanan toplam para
    public int value = 1;        // Bu coin'in değeri

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin")) return;

        total += value;          // Para artır
        Destroy(gameObject);     // Coini yok et
    }
}
