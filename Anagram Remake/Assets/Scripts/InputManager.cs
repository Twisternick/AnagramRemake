using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using System;
using System.Threading;

public class InputManager : MonoBehaviour
{
    [SerializeField] private List<TMP_Text> letterBank;
    [SerializeField] private List<TMP_Text> showTexts;
    [SerializeField] private List<string> words = new List<string>();
    [SerializeField] private List<char> vowels, consonants;

    [SerializeField] private List<Letter> letters;
    [SerializeField] private List<Placeholders> placeholders;

    [SerializeField] private StandaloneInputModule inputModule;

    private int letterStackIndex;
    private int plhoin;
    public int placeHolderIndex
    {
        get { return plhoin; }
        set
        {
            plhoin = Mathf.Clamp(value, 0, 8);
            
        }
    }

    private Coroutine backSpaceCoroutine;
    // Start is called before the first frame update
    void Start()
    {
        ClearLetterBank();
        GetWordList();
        //ShowLetterBank();
    }


    public void GetInputString()
    {
        foreach(char c in Input.inputString)
        {
            int index = letters.FindIndex(x => x.letter == c.ToString() && x.used == false);
            if (index != -1)
            {
                // Add Letter to be shown
                placeholders[placeHolderIndex].letterIndex = index;
                letters[index].used = true;
                letters[index].MoveToPosition(placeholders[placeHolderIndex].transform.position + new Vector3(0f, 55f, 0f));
                placeHolderIndex++;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ValidateWord();
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            Backspace();
            if (placeHolderIndex >= 0)
            {
                if (backSpaceCoroutine != null)
                {
                    StopCoroutine(backSpaceCoroutine);
                }
                backSpaceCoroutine = StartCoroutine(CheckHold());
                
            }
        }
        else
        {
            GetInputString();
        }
        //lastText = input.text;
    }

    public void Backspace()
    {
        if (placeHolderIndex >= 0)
        {
            if (placeholders[placeHolderIndex].letterIndex > -1)
            {
                letters[placeholders[placeHolderIndex].letterIndex].used = false;
                letters[placeholders[placeHolderIndex].letterIndex].ResetPosition();
            }
            placeholders[placeHolderIndex].letterIndex = -1;
            placeHolderIndex--;    
        }
    }


    public IEnumerator CheckHold()
    {
        yield return new WaitForSeconds(inputModule.repeatDelay);
        while (Input.GetKey(KeyCode.Backspace))
        {
            Backspace();
            yield return new WaitForSeconds((1/inputModule.inputActionsPerSecond));
            
        }
    }

    public void ServerRandomLetters()
    {
        for (int i = 0; i < 9; i ++)
        {
            RandomizeLetters(Convert.ToBoolean(UnityEngine.Random.Range(0, 2)));
        }
    }

    public void RandomizeLetters(bool isVowel)
    {
        if (letterStackIndex < 9)
        {
            if (isVowel)
            {
                int ran = UnityEngine.Random.Range(0, vowels.Count);
                letters[letterStackIndex].letter = vowels[ran].ToString();
            }
            else
            {
                // Consonants
                int ran = UnityEngine.Random.Range(0, consonants.Count);
                letters[letterStackIndex].letter = consonants[ran].ToString();
            }
            letterStackIndex++;
        }
        if (letterStackIndex == 9)
        {
            // Set Two Letters values to be x2 and x3
            int silver = UnityEngine.Random.Range(0, letters.Count);
            int gold = UnityEngine.Random.Range(0, letters.Count);
            while (gold == silver)
            {
                gold = UnityEngine.Random.Range(0, letters.Count);
            }

            letters[silver].value = 2;
            letters[gold].value = 3;
        }
    }
    public void ClearLetterBank()
    {
        foreach(var letter in letters)
        {
            letter.display.text = "";
        }
    }

    public void ShuffleLetterBank()
    {
        // Rearrange parent order?
        letters.Shuffle();
    }


    public void ValidateWord()
    {
        //print(words.IndexOf(holdingText.ToUpper()));
        string test = "";
        foreach (Placeholders p in placeholders)
        {
            if (p.letterIndex >= 0)
            {
                test += letters[p.letterIndex].letter;
            }
        }
        
        if (words.IndexOf(test.ToUpper()) != -1)
        {
            // Valid word
            print("Valid Word");
        }
        else
        {
            print("Invaild Word");
        }
    }

    public void GetWordList()
    {
        if (File.Exists(Application.persistentDataPath + "/wordlist.txt"))
        {
            words = new List<string>(File.ReadAllLines(Application.persistentDataPath + "/wordlist.txt"));
        }
        else
        {
            SaveWordList();
        }
    }

    public void SaveWordList()
    {
        string[] lines = File.ReadAllLines(@"C:\Users\annic\Desktop\pg29765.txt");
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
        File.WriteAllLines(Application.persistentDataPath + "/wordlist.txt", words.ToArray());
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
public static class ThreadSafeRandom
{
    [ThreadStatic] private static System.Random Local;

    public static System.Random ThisThreadsRandom
    {
        get { return Local ?? (Local = new System.Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
    }
}

static class MyExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
