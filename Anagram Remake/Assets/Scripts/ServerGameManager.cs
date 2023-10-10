using System;
using System.Collections;
using System.Collections.Generic;
using tehelee.networking.packets;
using testingui.networking.packets;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Networking;

namespace tehelee.networking
{
    public class ServerGameManager : MonoBehaviour
    {
        public enum GameState
        {
            Waiting,
            RoundOneGetLetters,
            RoundOneSentLetters,
            RoundTwo,
            RoundThree,
            RoundFour,
            RoundFive,
            ScoreRecap
        }

        private GameState _gameState;

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
            public string word;
        }

        public string currentBoardLetters;
        public string roundFiveAnagram;
        string[] vowels = new string[] { "A", "E", "I", "O", "U" };
        string[] consonants = new string[] { "B", "C", "D", "F", "G", "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "V", "W", "X", "Y", "Z" };

        public List<Player> clients = new List<Player>();

        public int twoScoreIndex, threeScoreIndex;

        public List<ServerLetter> serverLetters = new List<ServerLetter>();

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
            InvokeRepeating("SendHeartBeat", 2f, 2f);
            _gameState = GameState.RoundOneGetLetters;
        }

        public void SendHeartBeat()
        {
            Server.instance.Send(new Heartbeat() { time = Time.time });
        }

        public void OnRoundStart()
        {
            twoScoreIndex = UnityEngine.Random.Range(0, 9);
            while (threeScoreIndex == twoScoreIndex)
            {
                threeScoreIndex = UnityEngine.Random.Range(0, 9);
            }
            currentBoardLetters = "";
            serverLetters.Clear();
        }

        public ReadResult GetClientIDFromConnection(NetworkConnection networkConnection, ref DataStreamReader dataStreamReader)
        {
            GetClientID getClientID = new GetClientID(ref dataStreamReader);

            Player player = new Player();
            player.name = "Player " + (clients.Count + 1);
            player.id = clients.Count;
            player.score = 0;
            player.word = "";
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

            StartCoroutine(GetWordFromDictionary(word.text, word.networkId));

            return ReadResult.Consumed;
        }

        public ReadResult GetLetterChoice(NetworkConnection networkConnection, ref DataStreamReader dataStreamReader)
        {
            /* print("GetLetterChoice"); */
            GetLetter getLetter = new GetLetter(ref dataStreamReader);

            GetServerLetter(getLetter.letterChoice);
            Server.instance.Send(new testingui.networking.packets.Letter() { networkId = getLetter.networkId, text = currentBoardLetters });

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
            if (serverLetters.Count == 9 && _gameState == GameState.RoundOneGetLetters)
            {
                /*  print ("Swapping to RoundOneSentLetters"); */
                Server.instance.Send(new testingui.networking.packets.CanPlay() { canPlay = true });
                _gameState = GameState.RoundOneSentLetters;
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

                Server.instance.Send(new ValidWord() { networkId = id, isValid = valid });
            }
        }

        public ReadResult OnHeartbeat(NetworkConnection connection, ref DataStreamReader reader)
        {
            return ReadResult.Consumed;
        }
    }
}