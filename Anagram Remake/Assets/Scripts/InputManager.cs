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
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine.Networking;
using testingui.networking.packets;
using Unity.Netcode;

namespace tehelee.networking
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private Client client;

        private int networkId = -1;

        [Serializable]
        public class Opponent 
        {
            public int networkId;
            public int letterCount;
            public List<GameObject> letterPlaceholders = new List<GameObject>();
        }


        [SerializeField] private List<TMP_Text> letterBank;
        [SerializeField] private List<TMP_Text> showTexts;
        [SerializeField] private List<string> words, anagrams = new List<string>();
        [SerializeField] private List<char> vowels, consonants;

        [SerializeField] private List<Letter> letters;
        [SerializeField] private List<Placeholders> placeholders;

        [SerializeField] private List<Opponent> opponents = new List<Opponent>();
        [SerializeField] private GameObject opponentLetterPrefab;

        [SerializeField] private StandaloneInputModule inputModule;
        [SerializeField] private TextAsset wordSource;

        [SerializeField] private TextMeshProUGUI clientIDText;

        private int letterStackIndex;
        private int plhoin;
        public int placeHolderIndex
        {
            get { return plhoin; }
            set
            {
                plhoin = Mathf.Clamp(value, 0, 9);
            }
        }

        private Coroutine backSpaceCoroutine;

        public bool canPlay;


        // Start is called before the first frame update
        void OnEnable()
        {
            /*             Packet.Register(client, typeof(Packets.Word));
                        Packet.Register(client, typeof (Packets.Letter));
                        client.RegisterListener(typeof(Packets.Round), OnRoundStart); */
            Client.AddStartupListener(() =>
            {
                Client.instance.RegisterListener(PacketRegistry.Round, OnRoundStart);
                Client.instance.RegisterListener(PacketRegistry.ValidWord, OnValidateWord);
                Client.instance.RegisterListener(PacketRegistry.Letter, RecieveLetter);
                Client.instance.RegisterListener(PacketRegistry.CanPlay, OnCanPlay);
                Client.instance.RegisterListener(PacketRegistry.RecieveClientID, OnRecieveClientID);
                Client.instance.RegisterListener(PacketRegistry.GetLetterPlacement, OnOpponentLetterPlacement);
            }
            );
            ClearLetterBank();
            //GetWordList();
            //ShowLetterBank();
        }
        void Start()
        {
            Client.instance.Send(new testingui.networking.packets.GetClientID() { });
        }

        private ReadResult OnRecieveClientID(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            RecieveClientID recieveClientID = new RecieveClientID(ref reader);
            if (networkId == -1)
            {
                
                networkId = recieveClientID.networkId;
                clientIDText.text = "Client ID: " + networkId;
                /* print("Client Recieved Network Connection id: " + recieveClientID.networkId);
                print("Client Network Id: " + networkId); */
                return ReadResult.Consumed;
            }
            else
            {
                if (networkId == recieveClientID.networkId)
                {
                    return ReadResult.Skipped;
                }
                if (opponents.Find(x => x.networkId == recieveClientID.networkId) != null)
                {
                    return ReadResult.Skipped;
                }
                Opponent opponent = new Opponent();
                opponent.networkId = recieveClientID.networkId;
                opponent.letterCount = 0;
                opponent.letterPlaceholders = new List<GameObject>();
                GameObject opp = Instantiate(opponentLetterPrefab,  opponentLetterPrefab.transform.parent);
                opp.SetActive(true);
                opp.GetComponentInChildren<TextMeshProUGUI>().text = "Opponent " + opponent.networkId;
                foreach(var child in opp.GetComponentsInChildren<RectTransform>(true))
                {
                    if (child.name == opp.name ||  child.GetComponent<TextMeshProUGUI>() != null)
                    {
                        continue;
                    }
                    opponent.letterPlaceholders.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
                opponents.Add(opponent);
                return ReadResult.Processed;
            }
        }

        private ReadResult OnRoundStart(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            //Packets.Round round = new Packets.Round(ref reader);
            Round round = new Round(ref reader);

            /* print("On Round Start"); */
            if (round.roundState == Round.RoundState.roundStart)
            {
                canPlay = true;
            }

            return ReadResult.Processed;
        }

        private ReadResult OnOpponentLetterPlacement(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            GetLetterPlacement getLetterPlacement = new GetLetterPlacement(ref reader);

            if (getLetterPlacement.networkId != networkId)
            {
                // Show the opponents letter count placed
                /* print(getLetterPlacement.networkId);
                print(getLetterPlacement.textLength);
                print(opponents.Count); */
                Opponent opp = opponents.Find(x=> x.networkId == getLetterPlacement.networkId);
                foreach(var letter in opp.letterPlaceholders)
                {
                    letter.SetActive(false);
                }
                for(int i = 0; i < getLetterPlacement.textLength; i++)
                {
                    opp.letterPlaceholders[i].SetActive(true);
                }
                return ReadResult.Processed;
            }
            else
            {
                return ReadResult.Skipped;
            }
        }

        private ReadResult OnValidateWord(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            ValidWord validWord = new ValidWord(ref reader);
            /* print("Valid Word Network Id: " + validWord.networkId);
            print("Client Word is Valid: " + validWord.isValid); */
            if (validWord.networkId == networkId)
            {
                canPlay = !validWord.isValid;
                /* print("Client Can Play: " + canPlay); */
                return ReadResult.Consumed;
            }

           /*  print("Valid Word: " + validWord.isValid);
 */
            return ReadResult.Skipped;
        }


        private ReadResult OnCanPlay(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            CanPlay canPlayPacket = new CanPlay(ref reader);
            if (!canPlayPacket.canPlay)
            {
                if (canPlayPacket.networkId == networkConnection.InternalId)
                {
                    canPlay = false;
                    return ReadResult.Processed;
                }
            }
            canPlay = canPlayPacket.canPlay;

            /* print("Can Play: " + canPlayPacket.canPlay); */

            return ReadResult.Processed;
        }
        public void GetInputString()
        {
            foreach (char c in Input.inputString)
            {
                int index = letters.FindIndex(x => x.letter.ToLower() == c.ToString() && x.used == false);
                if (index != -1)
                {
                    // Add Letter to be shown
                    placeholders[placeHolderIndex].letterIndex = index;
                    letters[index].used = true;
                    letters[index].MoveToPosition(placeholders[placeHolderIndex].transform.position + new Vector3(0f, 55f, 0f));
                    placeHolderIndex++;
                    Client.instance.Send(new testingui.networking.packets.ShowLetterPlacement() { networkId = (ushort)networkId, textLength = placeHolderIndex });

                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!canPlay)
            {
                return;
            }

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
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                ShuffleLetterBank();
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
                placeHolderIndex--;
                if (placeholders[placeHolderIndex].letterIndex > -1)
                {
                    letters[placeholders[placeHolderIndex].letterIndex].used = false;
                    letters[placeholders[placeHolderIndex].letterIndex].ResetPosition();
                }
                placeholders[placeHolderIndex].letterIndex = -1;
                //client.Send(new Packets.Letter() { networkId = client.networkId, text = "0" });
            }
            /*else
            {
                if (placeholders[placeHolderIndex].letterIndex > -1)
                {
                    letters[placeholders[placeHolderIndex].letterIndex].used = false;
                    letters[placeholders[placeHolderIndex].letterIndex].ResetPosition();
                }
                placeholders[placeHolderIndex].letterIndex = -1;
                placeHolderIndex--;
            }*/
        }


        public void GetVowel()
        {
            /* print("GetVowel");
            print("Network Id: " + networkId);
            print("GetLetter.LetterChoice.Vowel: " + PacketRegistry.GetLetter); */
            Client.instance.Send(new testingui.networking.packets.GetLetter() { networkId = (ushort)networkId, letterChoice = GetLetter.LetterChoice.Vowel });
        }

        public void GetConsonant()
        {
            Client.instance.Send(new testingui.networking.packets.GetLetter() { networkId = (ushort)networkId, letterChoice = GetLetter.LetterChoice.Consonant });
        }

        public void LetServerChoose()
        {
            Client.instance.Send(new testingui.networking.packets.GetLetter() { networkId = (ushort)networkId, letterChoice = GetLetter.LetterChoice.ServerChoice });
        }

        public ReadResult RecieveLetter(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            testingui.networking.packets.Letter letter = new testingui.networking.packets.Letter(ref reader);
            /* print("Network Connection Client internal id: " + networkConnection.InternalId);
            print("Letter Network Id: " + letter.networkId); */

            for (int i = 0; i < letter.text.Length; i++)
            {
                letters[i].letter = letter.text[i].ToString().ToLower();
            }


            return ReadResult.Processed;
        }



        public IEnumerator CheckHold()
        {
            yield return new WaitForSeconds(inputModule.repeatDelay);
            while (Input.GetKey(KeyCode.Backspace))
            {
                Backspace();
                yield return new WaitForSeconds((1 / inputModule.inputActionsPerSecond));
            }
        }

        public void ServerRandomLetters()
        {
            if (!canPlay)
            {
                return;
            }
            for (int i = 0; i < 9; i++)
            {
                RandomizeLetters(Convert.ToBoolean(UnityEngine.Random.Range(0, 2)));
            }
        }

        public void GetNineLetterAnagram()
        {
            if (!canPlay)
            {
                return;
            }
            int r = UnityEngine.Random.Range(0, anagrams.Count);
           /*  print(anagrams[r]); */
            List<char> anagram = anagrams[r].ToList();
            anagram.Shuffle();
            for (int i = 0; i < 9; i++)
            {
                letters[i].letter = anagram[i].ToString().ToLower();
            }
            letterStackIndex = 9;
        }

        public void RandomizeLetters(bool isVowel)
        {
            if (!canPlay)
            {
                return;
            }
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

        }
        public void ClearLetterBank()
        {
            foreach (var letter in letters)
            {
                letter.display.text = "";
            }
        }

        public void ShuffleLetterBank()
        {
            if (!canPlay)
            {
                return;
            }
            // Rearrange parent order?
            int n = letters.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                Letter value = letters[k];
                Vector2 tempPosition = letters[k].startingPosition;
                Vector2 otherPosition = letters[n].startingPosition;
                letters[k] = letters[n];
                letters[n] = value;
                letters[k].startingPosition = tempPosition;
                letters[n].startingPosition = otherPosition;
            }
            foreach (Letter letter in letters)
            {
                letter.ResetPosition();
            }
        }


        public void ValidateWord()
        {
            if (!canPlay)
            {
                return;
            }
            //print(words.IndexOf(holdingText.ToUpper()));
            string test = "";
            foreach (Placeholders p in placeholders)
            {
                if (p.letterIndex >= 0)
                {
                    test += letters[p.letterIndex].letter.ToLower();
                }
            }
           /*  print(test); */
            Client.instance.Send(new testingui.networking.packets.Word() { text = test, networkId = (ushort)this.networkId });
            //StartCoroutine(GetWordFromDictionary(test));
            /*
            if (words.IndexOf(test.ToUpper()) != -1)
            {
                // Valid word
                print("Valid Word");
            }
            else
            {
                print("Invaild Word");
            }*/
        }

        public IEnumerator GetWordFromDictionary(string word)
        {
            //print("https://api.dictionaryapi.dev/api/v2/entries/en/" + word);
            using (UnityWebRequest webRequest = UnityWebRequest.Get("https://api.dictionaryapi.dev/api/v2/entries/en/" + word))
            {
                yield return webRequest.SendWebRequest();

                var text = webRequest.downloadHandler.text;

                //print(text);
                if (!text.Contains("\"title\":\"No Definitions Found\""))
                {
                    // Valid word
                    /* print("Valid Word"); */
                    //client.Send(new Packets.Word() { networkId = client.networkId, text = word });
                }
                else
                {
                    /* print("Invaild Word"); */
                }

            }
        }

        public void GetWordList()
        {
            if (File.Exists(Application.persistentDataPath + "/wordlist.txt") && File.Exists(Application.persistentDataPath + "/anagramlist.txt"))
            {
                words = new List<string>(File.ReadAllLines(Application.persistentDataPath + "/wordlist.txt"));
                anagrams = new List<string>(File.ReadAllLines(Application.persistentDataPath + "/anagramlist.txt"));
            }
            else
            {
                SaveWordList();
            }
        }

        public void SaveWordList()
        {
            //string[] lines = File.ReadAllLines(@"C:\Users\NickV\Desktop\pg29765.txt");
            string[] lines = wordSource.text.Split();
            foreach (string line in lines)
            {
                if (line.Length < 9 && !string.IsNullOrEmpty(line))
                {
                    if (IsAllUpper(line))
                    {
                        if (!words.Contains(line))
                        {
                            words.Add(line);
                        }
                    }
                }
                else if (line.Length == 9 && !string.IsNullOrEmpty(line))
                {
                    if (IsAllUpper(line))
                    {
                        if (!anagrams.Contains(line))
                        {
                            anagrams.Add(line);
                        }
                    }
                }
            }
            File.WriteAllLines(Application.persistentDataPath + "/wordlist.txt", words.ToArray());
            File.WriteAllLines(Application.persistentDataPath + "/anagramlist.txt", anagrams.ToArray());
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
}