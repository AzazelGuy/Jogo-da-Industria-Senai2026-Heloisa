using UnityEngine;
using UnityEngine.UI;

public class AutoScrollMenuBackground : MonoBehaviour
{
    [SerializeField] private RawImage _img;
    [SerializeField] private float _x, _y;

    void Update()
    {
        // Movimentamos o UV Rect com base no tempo.
        // Usamos Time.unscaledDeltaTime para que o fundo continue 
        // se movendo mesmo se o jogo estiver pausado (timeScale = 0).
        _img.uvRect = new Rect(
            _img.uvRect.position + new Vector2(_x, _y) * Time.unscaledDeltaTime,
            _img.uvRect.size
        );
    }
}
