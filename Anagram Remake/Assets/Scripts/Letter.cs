using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Letter : MonoBehaviour
{
    public TMP_Text display;
    public Image background;

    public TMP_Text multiplier;
    private string l;
    public string letter
    {
        get
        {
            return l;
        }
        set
        {
            l = value;
            display.text = l;
        }
    }

    public bool used;

    private int v = 1;

    public int value
    {
        get
        {
            return v;
        }
        set
        {
            v = value;
            switch(v)
            {
                case 1:
                    multiplier.text = "";
                    // dont display and set default color
                    break;
                case 2:
                    multiplier.text = "x2";
                    // Change color and Set Multiplier Active and set Multiplier Text
                    break;
                case 3:
                    multiplier.text = "x3";
                    // Change color and Set Multiplier Active and set Multiplier Text
                    break;
            }
        }
    }

    public Vector2 startingPosition;

    private Vector3 velocity = Vector3.zero;

    private Coroutine moveTo;

    private void Start()
    {
        startingPosition = transform.position;
        value = 1;
    }


    public void ResetPosition()
    {
        MoveToPosition(startingPosition);
    }

    public void MoveToPosition(Vector3 position)
    {
        if (moveTo != null)
        {
            StopCoroutine(moveTo);
        }
        moveTo = StartCoroutine(LerpToPosition(position));
    }

    private IEnumerator LerpToPosition(Vector3 position)
    {
        float duration = 0.25f;
        float time = 0;
        Vector3 startPosition = transform.position;
        while (time < duration)
        {
            transform.position = Vector3.Lerp(startPosition, position, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = position;
    }
}
