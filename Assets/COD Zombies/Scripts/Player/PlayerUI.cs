using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    public Slider hpBar;
    public TMP_Text nameText;
    public TMP_Text scoreText;

    // Solo una referencia global para la UI local

    public void UpdateUI(string playerName, float hpPercent, int score)
    {
        nameText.text = playerName;
        hpBar.value = hpPercent;
        scoreText.text = "Puntos: " + score;
    }
}
