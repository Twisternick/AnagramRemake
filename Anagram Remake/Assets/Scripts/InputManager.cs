using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System.Linq;

public class InputManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField input;
    [SerializeField] private List<TMP_Text> showTexts;
    [SerializeField] private List<string> words = new List<string>();
    [SerializeField] private List<char> vowels, consonants;
    [SerializeField] private List<char> letterStack;
    [SerializeField] private List<TMP_Text> letterBank;

    private string lastText;
    // Start is called before the first frame update
    void Start()
    {
        input.Select();

        ClearShownList();
        ClearLetterBank();
        GetWordList();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ValidateWord();
        }
        if (Input.GetKeyUp(KeyCode.Return))
        {
            input.ActivateInputField();
        }
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (lastText.Length > 0)
            {
                letterStack.Add(lastText[lastText.Length - 1]);
                ShowLetterBank();
            }
        }
        lastText = input.text;
    }

    public void RandomizeLetters(bool isVowel)
    {
        if (letterStack.Count < 9)
        {
            if (isVowel)
            {
                int ran = Random.Range(0, vowels.Count);
                letterStack.Add(vowels[ran]);
            }
            else
            {
                // Consonants
                int ran = Random.Range(0, consonants.Count);
                letterStack.Add(consonants[ran]);
            }
        }
        ShowLetterBank();
    }
    public void ClearLetterBank()
    {
        foreach(var letter in letterBank)
        {
            letter.text = "";
        }
    }

    public void ShowLetterBank()
    {
        ClearLetterBank();
        for(int i = 0; i < letterStack.Count; i++)
        {
            letterBank[i].text = letterStack[i].ToString();
        }
    }


    public void ClearShownList()
    {
        foreach(var text in showTexts)
        {
            text.text = "";
        }
    }

    public void UpdateShownList()
    {
        ClearShownList();
        if (input.text.Length == 0)
        {
            return;
        }
        if (letterStack.Contains(input.text[input.text.Length - 1]))
        {
            letterStack.Remove(input.text[input.text.Length-1]);
            ShowLetterBank();
        }
        else if (input.text.Length != lastText.Length && !Input.GetKeyDown(KeyCode.Backspace))
        {
            input.text = input.text.Remove(input.text.Length-1);
        }
        for (int i = 0; i < input.text.Length; i++)
        {
            showTexts[i].text = input.text[i].ToString();
        }
    }

    public void ValidateWord()
    {
        print(words.IndexOf(input.text.ToUpper()));
        if (words.IndexOf(input.text.ToUpper()) != -1)
        {
            // Valid word
            print("Valid Word");
        }
        else
        {
            print("Invaild Word");
        }
        input.Select();
    }

    public void GetWordList()
    {
        words = new List<string>(File.ReadAllLines(@"C:\Users\NickV\Desktop\wordlist.txt"));
    }

    public void SaveWordList()
    {
        string[] lines = File.ReadAllLines(@"C:\Users\NickV\Desktop\pg29765.txt");
        foreach (string line in lines)
        {
            if (line.Length <= 9 && !string.IsNullOrEmpty(line))
            {
                if (IsAllUpper(line))
                {
                    if (!words.Contains(line))
                    {
                        words.Add(line);
                    }
                }
            }
        }
        File.WriteAllLines(@"C:\Users\NickV\Desktop\wordlist.txt", words.ToArray());
    }

    public bool IsAllUpper(string input)
    {
        foreach (var c in input)
        {
            if (!char.IsUpper(c))
                return false;
        }
        return true;
    }
}
