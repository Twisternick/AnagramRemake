using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using tehelee.networking.packets;
using testingui.networking.packets;
using Unity.Networking.Transport;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace tehelee.networking
{
    public class ServerGameManager : MonoBehaviour
    {
        public enum GameState
        {
            Waiting = -1,
            RoundOneGetLetters,
            RoundOneSentLetters,
            RoundOneSummary,
            RoundTwoGetLetters,
            RoundTwoSentLetters,
            RoundTwoSummary,

            RoundThreeGetLetters,
            RoundThreeSentLetters,
            RoundThreeSummary,
            RoundFourGetLetters,
            RoundFourSentLetters,
            RoundFourSummary,
            RoundFiveGetLetters,
            RoundFiveSentLetters,
            ScoreRecap
        }
        [SerializeField]
        private GameState _gameState;

        public GameState gameState
        {

            get { return _gameState; }
            set
            {
                print(value);
                _gameState = value;
                SetGameState();
            }
        }

        [Serializable]
        public class ServerLetter
        {
            public string letter;
            public int amount;
        }
        [Serializable]
        public class Player
        {
            public string name;
            public int id;
            public int score;
            public List<string> words;
        }

        public string currentBoardLetters;
        public string roundFiveAnagram;
        string[] vowels = new string[] { "A", "E", "I", "O", "U" };
        string[] consonants = new string[] { "B", "C", "D", "F", "G", "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "V", "W", "X", "Y", "Z" };

        public List<Player> clients = new List<Player>();

        public int twoScoreIndex, threeScoreIndex;

        public List<ServerLetter> serverLetters = new List<ServerLetter>();

        private float roundTimer = 120f;

        private string pickedAnagram;

        private List<string> anagrams = new List<string>();

        private List<string> words = new List<string>();

        void OnEnable()
        {
            Server.AddStartupListener(() =>
            {
                Server.instance.RegisterListener(PacketRegistry.Heartbeat, OnHeartbeat);
                Server.instance.RegisterListener(PacketRegistry.Word, ValidateWord);
                Server.instance.RegisterListener(PacketRegistry.GetLetter, GetLetterChoice);
                Server.instance.RegisterListener(PacketRegistry.GetClientID, GetClientIDFromConnection);
                Server.instance.RegisterListener(PacketRegistry.ShowLetterPlacement, OnLetterPlacement);
            });
            InvokeRepeating("SendHeartBeat", 1f, 1f);
            _gameState = GameState.Waiting;
            GetAnagramList();
        }

        public void SendHeartBeat()
        {
            Server.instance.Send(new Heartbeat() { time = Time.time });
        }

        public void OnRoundStart()
        {
            twoScoreIndex = UnityEngine.Random.Range(0, 9);
            do
            {
                threeScoreIndex = UnityEngine.Random.Range(0, 9);
            } while (threeScoreIndex == twoScoreIndex);

            currentBoardLetters = "";
            serverLetters.Clear();
            roundTimer = 120f;
            Server.instance.Send(new testingui.networking.packets.Round() { roundState = Round.RoundState.roundStart, counter = (int)gameState / 3, clientToChoose = 0 });
            //Server.instance.Send(new testingui.networking.packets.GetLetterPlacement() { networkId = -1 });
            if (gameState == GameState.RoundFiveGetLetters)
            {
                twoScoreIndex = -1;
                threeScoreIndex = -1;
                GetNineLetterAnagram();
            }
        }

        public void ResetGame()
        {
            clients.Clear();
        }

        public ReadResult GetClientIDFromConnection(NetworkConnection networkConnection, ref DataStreamReader dataStreamReader)
        {
            GetClientID getClientID = new GetClientID(ref dataStreamReader);

            if (gameState == GameState.Waiting)
            {
                gameState++;
            }

            Player player = new Player();
            player.name = "Player " + (clients.Count + 1);
            player.id = clients.Count;
            player.score = 0;
            player.words = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                player.words.Add("");
            }
            clients.Add(player);

            /*  print("GetClientIDFromConnection");
             print("Client Count" + clients.Count); */
            Server.instance.Send(new testingui.networking.packets.RecieveClientID() { networkId = (ushort)player.id });
            foreach (Player p in clients)
            {
                Server.instance.Send(new testingui.networking.packets.RecieveClientID() { networkId = (ushort)p.id });
            }

            return ReadResult.Consumed;
        }

        private ReadResult OnLetterPlacement(NetworkConnection networkConnection, ref DataStreamReader dataStreamReader)
        {
            ShowLetterPlacement showLetterPlacement = new ShowLetterPlacement(ref dataStreamReader);

            Server.instance.Send(new testingui.networking.packets.GetLetterPlacement() { networkId = showLetterPlacement.networkId, textLength = showLetterPlacement.textLength });

            return ReadResult.Consumed;
        }

        public ReadResult ValidateWord(NetworkConnection networkConnection, ref DataStreamReader dataStreamReader)
        {
            Word word = new Word(ref dataStreamReader);

            //StartCoroutine(GetWordFromDictionary(word.text, word.networkId));
            if (gameState == GameState.RoundFiveSentLetters)
            {
                if (word.text == pickedAnagram)
                {
                    //Server.instance.Send(new ValidWord() { networkId = word.networkId, isValid = true, score = 9 });
                    Player player = clients.Find(x => x.id == word.networkId);
                    player.score += 9;
                    player.words[((int)gameState - 1) / 3] = word.text;
                    /* foreach (Player player in clients)
                    {
                        if (player.id == word.networkId)
                        {
                            player.score += 9;
                            player.words[((int)gameState - 1) / 2] = word.text;
                        }
                        Server.instance.Send(new testingui.networking.packets.Score() { networkId = (ushort)player.id, score = player.score });
                    } */
                    gameState++;
                }
                return ReadResult.Consumed;
            }
            else
            {
                CheckWordFromDictionary(word.text, word.networkId, word.score);
            }
            return ReadResult.Consumed;
        }

        public ReadResult GetLetterChoice(NetworkConnection networkConnection, ref DataStreamReader dataStreamReader)
        {
            GetLetter getLetter = new GetLetter(ref dataStreamReader);

            GetServerLetter(getLetter.letterChoice);
            Server.instance.Send(new testingui.networking.packets.Letter() { networkId = getLetter.networkId, text = currentBoardLetters, doubleLetterIndex = twoScoreIndex, tripleLetterIndex = threeScoreIndex });

            return ReadResult.Consumed;
        }

        public void GetServerLetter(GetLetter.LetterChoice letterChoice)
        {
            if (serverLetters.Count == 9)
            {
                return;
            }
            ServerLetter serverLetter = new ServerLetter();
            if (letterChoice == GetLetter.LetterChoice.Vowel)
            {
                serverLetter.letter = vowels[UnityEngine.Random.Range(0, vowels.Length)];
            }
            else if (letterChoice == GetLetter.LetterChoice.Consonant)
            {
                serverLetter.letter = consonants[UnityEngine.Random.Range(0, consonants.Length)];
            }
            else
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    serverLetter.letter = vowels[UnityEngine.Random.Range(0, vowels.Length)];
                }
                else
                {
                    serverLetter.letter = consonants[UnityEngine.Random.Range(0, consonants.Length)];
                }
            }
            currentBoardLetters += serverLetter.letter;
            serverLetters.Add(serverLetter);
            if (serverLetters.Count == twoScoreIndex)
            {
                serverLetters[^1].amount = 2;
            }
            else if (serverLetters.Count == threeScoreIndex)
            {
                serverLetters[^1].amount = 3;
            }
            else
            {
                serverLetters[^1].amount = 1;
            }
            if (serverLetters.Count == 9)
            {
                switch (gameState)
                {
                    case GameState.RoundOneGetLetters:
                    case GameState.RoundTwoGetLetters:
                    case GameState.RoundThreeGetLetters:
                    case GameState.RoundFourGetLetters:
                    case GameState.RoundFiveGetLetters:
                        Server.instance.Send(new testingui.networking.packets.CanPlay() { canPlay = true });
                        gameState++;
                        break;

                }
                /* Server.instance.Send(new testingui.networking.packets.CanPlay() { canPlay = true });
                gameState++; */
            }
        }

        public IEnumerator GetWordFromDictionary(string word, ushort id)
        {
            //print("https://api.dictionaryapi.dev/api/v2/entries/en/" + word);
            using (UnityWebRequest webRequest = UnityWebRequest.Get("https://api.dictionaryapi.dev/api/v2/entries/en/" + word))
            {
                yield return webRequest.SendWebRequest();

                var text = webRequest.downloadHandler.text;

                /*    print ("Dictonary Text: " + text);

                    print("Tested Word: " + word);
     */

                bool valid = !text.Contains("\"title\":\"No Definitions Found\"");
                /* 
                                print("Valid: " + valid);

                                print("NETWORKD ID: " + id); */
                if (valid)
                {
                    clients.Find(x => x.id == id).words[((int)gameState - 1) / 3] = word;
                }
                Server.instance.Send(new ValidWord() { networkId = id, isValid = valid });
                if (clients.TrueForAll(x => x.words[((int)gameState - 1) / 3] != ""))
                {
                    gameState++;
                }


            }
        }

        public void CheckWordFromDictionary(string word, ushort id, int score)
        {
            switch (gameState)
            {
                case GameState.Waiting:
                case GameState.ScoreRecap:
                case GameState.RoundOneSummary:
                case GameState.RoundTwoSummary:
                case GameState.RoundThreeSummary:
                case GameState.RoundFourSummary:
                    return;
            }
            bool valid = words.Find(x => x == word.ToLower()) != null;
            Player player = clients.Find(x => x.id == id);
            if (valid)
            {
                player.words[((int)gameState - 1) / 3] = word;
                player.score += score;
            }
            else
            {
                //player.words[((int)gameState - 1) / 3] = " ";
            }
            Server.instance.Send(new ValidWord() { networkId = id, isValid = valid, score = player.score });
            if (gameState != GameState.RoundFiveSentLetters)
            {
                if (clients.TrueForAll(x => x.words[((int)gameState - 1) / 3] != ""))
                {
                    gameState++;
                }
            }
            else
            {
                if (valid)
                {
                    gameState++;
                }
            }
        }


        public ReadResult OnHeartbeat(NetworkConnection connection, ref DataStreamReader reader)
        {
            return ReadResult.Consumed;
        }

        public void SetGameState()
        {
            int highestScore;
            switch (gameState)
            {
                case GameState.Waiting:
                    return;

                case GameState.RoundOneGetLetters:
                case GameState.RoundTwoGetLetters:
                case GameState.RoundThreeGetLetters:
                case GameState.RoundFourGetLetters:
                case GameState.RoundFiveGetLetters:
                    OnRoundStart();
                    break;
                case GameState.RoundOneSentLetters:
                case GameState.RoundTwoSentLetters:
                case GameState.RoundThreeSentLetters:
                case GameState.RoundFourSentLetters:
                case GameState.RoundFiveSentLetters:

                    break;

                case GameState.RoundOneSummary:
                case GameState.RoundTwoSummary:
                case GameState.RoundThreeSummary:
                case GameState.RoundFourSummary:
                    Server.instance.Send(new testingui.networking.packets.Round() { roundState = Round.RoundState.roundEnd, counter = (int)gameState / 3, clientToChoose = 0 });
                    highestScore = clients.Max(x => x.score);
                    foreach (Player player in clients)
                    {
                        print("Player " + player.id + " Score: " + player.score);
                        Server.instance.Send(new testingui.networking.packets.Score() { networkId = player.id, score = player.score, round = (int)gameState / 3, winner = false, word = player.words[((int)gameState - 1) / 3] });
                    }

                    StartCoroutine(WaitForRecap());
                    break;

                case GameState.ScoreRecap:
                    Server.instance.Send(new testingui.networking.packets.Round() { roundState = Round.RoundState.roundEnd, counter = (int)gameState / 3, clientToChoose = 0 });
                    highestScore = clients.Max(x => x.score);
                    foreach (Player player in clients)
                    {
                        print("Player " + player.id + " Score: " + player.score);
                        Server.instance.Send(new testingui.networking.packets.Score() { networkId = player.id, score = player.score, round = (int)gameState / 3, winner = player.score == highestScore });
                    }
                    gameState = GameState.Waiting;
                    ResetGame();
                    break;
            }
        }

        void Update()
        {
            switch (gameState)
            {
                case GameState.Waiting:
                case GameState.ScoreRecap:
                case GameState.RoundOneSummary:
                case GameState.RoundTwoSummary:
                case GameState.RoundThreeSummary:
                case GameState.RoundFourSummary:
                case GameState.RoundOneGetLetters:
                case GameState.RoundTwoGetLetters:
                case GameState.RoundThreeGetLetters:
                case GameState.RoundFourGetLetters:
                case GameState.RoundFiveGetLetters:
                    return;
                case GameState.RoundOneSentLetters:
                case GameState.RoundTwoSentLetters:
                case GameState.RoundThreeSentLetters:
                case GameState.RoundFourSentLetters:
                case GameState.RoundFiveSentLetters:
                    if (roundTimer > 0)
                    {
                        roundTimer -= Time.deltaTime;
                    }
                    else
                    {
                        gameState++;
                    }
                    break;
            }
        }
        void FixedUpdate()
        {
            switch (gameState)
            {
                case GameState.Waiting:
                case GameState.ScoreRecap:
                case GameState.RoundOneSummary:
                case GameState.RoundTwoSummary:
                case GameState.RoundThreeSummary:
                case GameState.RoundFourSummary:
                case GameState.RoundOneGetLetters:
                case GameState.RoundTwoGetLetters:
                case GameState.RoundThreeGetLetters:
                case GameState.RoundFourGetLetters:
                case GameState.RoundFiveGetLetters:
                    return;
                case GameState.RoundOneSentLetters:
                case GameState.RoundTwoSentLetters:
                case GameState.RoundThreeSentLetters:
                case GameState.RoundFourSentLetters:
                case GameState.RoundFiveSentLetters:
                    Server.instance.Send(new testingui.networking.packets.Timer() { time = roundTimer });
                    break;
            }
            /* if (gameState != GameState.Waiting && gameState != GameState.ScoreRecap)
            {
                if ((int)gameState % 2 == 1)
                {

                }
            } */
        }

        private IEnumerator WaitForRecap()
        {
            yield return new WaitForSeconds(7f);
            gameState++;
        }

        public void GetAnagramList()
        {
            print(Application.persistentDataPath);
            if (File.Exists(Application.persistentDataPath + "/anagramlist.txt"))
            {
                anagrams = new List<string>(File.ReadAllLines(Application.persistentDataPath + "/anagramlist.txt"));
            }
            if (File.Exists(Application.persistentDataPath + "/english.txt"))
            {
                words = new List<string>(File.ReadAllLines(Application.persistentDataPath + "/english.txt"));
            }
        }

        public void GetNineLetterAnagram()
        {
            Server.instance.Send(new testingui.networking.packets.CanPlay() { canPlay = true });

            int r = UnityEngine.Random.Range(0, anagrams.Count);

            List<char> anagram = anagrams[r].ToList();
            print(anagrams[r]);
            pickedAnagram = anagrams[r].ToLower();
            anagram.Shuffle();
            for (int i = 0; i < 9; i++)
            {
                ServerLetter serverLetter = new ServerLetter();
                serverLetter.letter = anagram[i].ToString().ToLower();
                serverLetters.Add(serverLetter);
                currentBoardLetters += serverLetter.letter;
            }
            Server.instance.Send(new testingui.networking.packets.Letter() { networkId = 0, text = currentBoardLetters, doubleLetterIndex = -1, tripleLetterIndex = -1 });

            gameState++;
        }
    }
}