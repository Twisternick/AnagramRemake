using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndScreen : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI winnerText;

    [SerializeField]
    private string winnerString = "Winner Winner Anagram Dinner!";

    [SerializeField]
    private float timeToWait = 0.5f;
    

    [SerializeField]
    private List<Transform> gearSpins = new List<Transform>();

    [SerializeField]
    private List<Transform> reverseGearSpins = new List<Transform>();



    void OnEnable()
    {
        //StartCoroutine(EndScreenCoroutine());
    }

    IEnumerator EndScreenCoroutine()
    {
        for (int i = 0; i < 1; i++)
        {
            
            yield return new WaitForSeconds(timeToWait);
            winnerText.text += winnerString;
            winnerText.text += "\n";
        }
    }

    void FixedUpdate()
    {
        foreach (Transform gear in gearSpins)
        {
            gear.Rotate(0, 0, 1);
        }
        foreach (Transform gear in reverseGearSpins)
        {
            gear.Rotate(0, 0, -1);
        }
    }

}
