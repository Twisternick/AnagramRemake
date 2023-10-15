using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using Unity.Networking.Transport;
using testingui.networking.packets;
using System.Threading;
using tehelee.networking.packets;
using UnityEngine.AI;

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

            public string name;
            public int letterCount;
            public List<GameObject> letterPlaceholders = new List<GameObject>();

            public TextMeshProUGUI scoreText;

            public int score;
        }

        [SerializeField] private List<Letter> letters;
        [SerializeField] private List<Placeholders> placeholders;

        [SerializeField] private List<Opponent> opponents = new List<Opponent>();
        [SerializeField] private GameObject opponentLetterPrefab, winnerEndScreen, loserEndScreen;
        [SerializeField] private TextMeshProUGUI loserPlayerScorePrefab, winnerPlayerScorePrefab;

        [SerializeField] private StandaloneInputModule inputModule;

        [SerializeField] private TextMeshProUGUI clientIDText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [SerializeField] private Image timerImage;

        [SerializeField] private TextMeshProUGUI roundText;
        private int phIn;
        public int placeHolderIndex
        {
            get { return phIn; }
            set
            {
                phIn = Mathf.Clamp(value, 0, 9);
            }
        }

        private int potientalScore, score;

        private List<TextMeshProUGUI> scoreTexts = new List<TextMeshProUGUI>();

        private Coroutine backSpaceCoroutine;

        public bool canPlay;
        [SerializeField]
        private float tickUpTime;

        private bool winner = false, playAgain = false;


        // Start is called before the first frame update
        void OnEnable()
        {
            Client.AddStartupListener(() =>
            {
                Client.instance.RegisterListener(PacketRegistry.Round, OnRoundStart);
                Client.instance.RegisterListener(PacketRegistry.ValidWord, OnValidateWord);
                Client.instance.RegisterListener(PacketRegistry.Letter, RecieveLetter);
                Client.instance.RegisterListener(PacketRegistry.CanPlay, OnCanPlay);
                Client.instance.RegisterListener(PacketRegistry.RecieveClientID, OnRecieveClientID);
                Client.instance.RegisterListener(PacketRegistry.GetLetterPlacement, OnOpponentLetterPlacement);
                Client.instance.RegisterListener(PacketRegistry.Timer, OnTimer);
                Client.instance.RegisterListener(PacketRegistry.Heartbeat, SendHeartBeat);
                Client.instance.RegisterListener(PacketRegistry.Score, OnScore);
            }
            );
            ClearLetterBank();
            playAgain = true;
        }
        void Start()
        {
            Client.instance.Send(new testingui.networking.packets.GetClientID() { });
        }

        private ReadResult SendHeartBeat(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            Heartbeat heartbeat = new Heartbeat(ref reader);
            Client.instance.Send(new Heartbeat() { });
            return ReadResult.Processed;
        }

        private ReadResult OnRecieveClientID(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            RecieveClientID recieveClientID = new RecieveClientID(ref reader);
            if (playAgain)
            {
                if (networkId == -1)
                {
                    
                    networkId = recieveClientID.networkId;
                    clientIDText.text = "Client ID: " + networkId;
                    winnerEndScreen.SetActive(false);
                    loserEndScreen.SetActive(false);
                    for (int i = scoreTexts.Count - 1; i >= 0; i--)
                    {
                        Destroy(scoreTexts[i].gameObject);
                    }
                    scoreTexts.Clear();
                    ResetBoardState();
                    roundText.text = "Round: 1/5";
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
                    opponent.name = "Player " + (opponent.networkId + 1);
                    opponent.letterCount = 0;
                    opponent.letterPlaceholders = new List<GameObject>();
                    GameObject opp = Instantiate(opponentLetterPrefab, opponentLetterPrefab.transform.parent);
                    opp.SetActive(true);
                    opp.GetComponentInChildren<TextMeshProUGUI>().text = "Opponent " + opponent.networkId;
                    foreach (var child in opp.GetComponentsInChildren<RectTransform>(true))
                    {
                        if (child.name == opp.name || child.GetComponent<TextMeshProUGUI>() != null)
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
            else
            {
                return ReadResult.Skipped;
            }
        }

        private ReadResult OnRoundStart(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            Round round = new Round(ref reader);

            if (round.roundState == Round.RoundState.roundStart && networkId != -1)
            {
                canPlay = true;
                roundText.text = "Round: " + (round.counter + 1) + "/5";
                ResetBoardState();
            }

            return ReadResult.Processed;
        }

        private ReadResult OnTimer(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            testingui.networking.packets.Timer timer = new testingui.networking.packets.Timer(ref reader);
            timerImage.fillAmount = timer.time / 120f;
            return ReadResult.Processed;
        }

        private ReadResult OnScore(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            testingui.networking.packets.Score scorePacket = new testingui.networking.packets.Score(ref reader);
            print("Score: " + scorePacket.score);
            print("Winner: " + scorePacket.winner);
            print("Network ID: " + scorePacket.networkId);
            print("My Network ID: " + networkId);
            if (scorePacket.networkId == networkId)
            {
                canPlay = false;
                playAgain = false;
                winner = scorePacket.winner;
                StartCoroutine(TickUpScore(score, scorePacket.score * 10, scorePacket.winner));
                networkId = -1;
            }
            else if (networkId == -1)
            {
                Opponent opp = opponents.Find(x => x.networkId == scorePacket.networkId);
                opp.score = scorePacket.score;
                if (winner)
                {
                    return ReadResult.Processed;
                }
                if (opp.scoreText == null)
                {
                    TextMeshProUGUI scoreText = Instantiate(loserPlayerScorePrefab, loserPlayerScorePrefab.transform.parent);
                    scoreText.gameObject.SetActive(true);
                    scoreText.text = opp.name + ": " + (opp.score * 10);
                    print("In OnScore is null: " + opp.name + ": " + (opp.score * 10));
                    scoreTexts.Add(scoreText);
                    opp.scoreText = scoreText;
                }
                else
                {
                    print("In OnScore not null: " + opp.name + ": " + (opp.score * 10));
                    opp.scoreText.text = opp.name + ": " + (opp.score * 10);
                }
                // Need to figure out how to set the end screen after someone finishes the anagram or doesn't
                // Also need to figure out how to finish a round if the timer runs out
            }
            else
            {
                Opponent opp = opponents.Find(x => x.networkId == scorePacket.networkId);
                opp.score = scorePacket.score;
            }

            return ReadResult.Processed;
        }

        private ReadResult OnOpponentLetterPlacement(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            GetLetterPlacement getLetterPlacement = new GetLetterPlacement(ref reader);
            if (getLetterPlacement.networkId != networkId)
            {
                // Show the opponents letter count placed
                Opponent opp = opponents.Find(x => x.networkId == getLetterPlacement.networkId);
                foreach (var letter in opp.letterPlaceholders)
                {
                    letter.SetActive(false);
                }
                for (int i = 0; i < getLetterPlacement.textLength; i++)
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
            if (validWord.networkId == networkId)
            {
                canPlay = !validWord.isValid;
                if (score != validWord.score * 10)
                {
                    StartCoroutine(TickUpScore(score, validWord.score * 10));
                }
                //scoreText.text = "Score: " + score;
                return ReadResult.Consumed;
            }
            return ReadResult.Skipped;
        }

        public ReadResult RecieveLetter(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            testingui.networking.packets.Letter letter = new testingui.networking.packets.Letter(ref reader);
            for (int i = 0; i < letter.text.Length; i++)
            {
                if (i == letter.doubleLetterIndex)
                {
                    letters[i].value = 2;
                }
                if (i == letter.tripleLetterIndex)
                {
                    letters[i].value = 3;
                }
                letters[i].letter = letter.text[i].ToString().ToLower();
            }

            return ReadResult.Processed;
        }



        private ReadResult OnCanPlay(NetworkConnection networkConnection, ref DataStreamReader reader)
        {
            CanPlay canPlayPacket = new CanPlay(ref reader);

            print("Can Play: " + canPlayPacket.canPlay);
            canPlay = canPlayPacket.canPlay;

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

        public void ClickedLetter(Letter letter)
        {
            if (!canPlay)
            {
                return;
            }
            if (letter.used)
            {
                int index = letters.IndexOf(letter);
                int placeIndex = placeholders.FindIndex(x => x.letterIndex == index);

                print(placeIndex);
                print(placeHolderIndex);

                for (int i = placeIndex; i < placeHolderIndex - 1; i++)
                {
                    placeholders[i].letterIndex = placeholders[i + 1].letterIndex;
                    letters[placeholders[i].letterIndex].MoveToPosition(placeholders[i].transform.position + new Vector3(0f, 55f, 0f));
                }

                placeholders[placeHolderIndex - 1].letterIndex = -1;
                placeHolderIndex--;
                letters[index].used = false;
                letters[index].ResetPosition();
                Client.instance.Send(new testingui.networking.packets.ShowLetterPlacement() { networkId = (ushort)networkId, textLength = placeHolderIndex });
            }
            else
            {
                int index = letters.IndexOf(letter);
                placeholders[placeHolderIndex].letterIndex = index;
                letters[index].used = true;
                letters[index].MoveToPosition(placeholders[placeHolderIndex].transform.position + new Vector3(0f, 55f, 0f));
                placeHolderIndex++;
                Client.instance.Send(new testingui.networking.packets.ShowLetterPlacement() { networkId = (ushort)networkId, textLength = placeHolderIndex });
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!canPlay)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
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
                Client.instance.Send(new testingui.networking.packets.ShowLetterPlacement() { networkId = (ushort)networkId, textLength = placeHolderIndex });
            }

        }


        public void GetVowel()
        {
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



        public IEnumerator CheckHold()
        {
            yield return new WaitForSeconds(inputModule.repeatDelay);
            while (Input.GetKey(KeyCode.Backspace))
            {
                Backspace();
                yield return new WaitForSeconds((1 / inputModule.inputActionsPerSecond));
            }
        }


        public void ClearLetterBank()
        {
            foreach (var letter in letters)
            {
                letter.display.text = "";

            }
        }

        public void ResetBoardState()
        {
            ClearLetterBank();
            placeHolderIndex = 0;
            foreach (var letter in letters)
            {
                letter.used = false;
                letter.ResetPosition();
                letter.value = 1;
            }
            foreach (var placeholder in placeholders)
            {
                placeholder.letterIndex = -1;
            }
            foreach (var opp in opponents)
            {
                foreach (var letter in opp.letterPlaceholders)
                {
                    letter.SetActive(false);
                }
            }
        }

        public void PlayAgain()
        {
            networkId = -1;
            playAgain = true;
            score = 0;
            scoreText.text = "Score: " + score;
            foreach (Opponent opp in opponents)
            {
                Destroy(opp.letterPlaceholders[0].transform.parent.gameObject);
            }
            opponents.Clear();
            Client.instance.Send(new testingui.networking.packets.GetClientID() { });
        }

        public void ShuffleLetterBank()
        {
            if (!canPlay)
            {
                return;
            }
            /* while (placeHolderIndex > 0)
            {
                placeHolderIndex--;
                if (placeholders[placeHolderIndex].letterIndex > -1)
                {
                    letters[placeholders[placeHolderIndex].letterIndex].used = false;
                    letters[placeholders[placeHolderIndex].letterIndex].ResetPosition();
                }
                placeholders[placeHolderIndex].letterIndex = -1;
            } */
            // Rearrange parent order?
            int n = letters.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                Letter value = letters[k];
                Vector2 tempPosition = letters[k].startingPosition;
                Vector2 otherPosition = letters[n].startingPosition;
                /* letters[k] = letters[n];
                letters[n] = value; */
                letters[k].startingPosition = otherPosition;
                letters[n].startingPosition = tempPosition;
            }

            foreach (Letter letter in letters)
            {
                if (!letter.used)
                    letter.ResetPosition();
            }
        }


        public void ValidateWord()
        {
            if (!canPlay)
            {
                return;
            }
            string test = "";
            potientalScore = 0;
            foreach (Placeholders p in placeholders)
            {
                if (p.letterIndex >= 0)
                {
                    test += letters[p.letterIndex].letter.ToLower();
                    potientalScore += letters[p.letterIndex].value;
                }
            }
            Client.instance.Send(new testingui.networking.packets.Word() { text = test, networkId = (ushort)this.networkId, score = potientalScore });
        }

        private IEnumerator TickUpScore(int oldScore, int newScore)
        {
            score = newScore;
            while (oldScore < score)
            {
                oldScore++;
                scoreText.text = "Score: " + oldScore;
                yield return new WaitForSeconds(tickUpTime);
            }
        }

        private IEnumerator TickUpScore(int oldScore, int newScore, bool winner)
        {
            score = newScore;
            playAgain = false;
            while (oldScore < score)
            {
                oldScore++;
                scoreText.text = "Score: " + oldScore;
                yield return new WaitForSeconds(tickUpTime / 2f);
            }
            if (winner)
            {
                // Show winner screen
                winnerEndScreen.SetActive(true);
            }
            else
            {
                // Show loser screen
                loserEndScreen.SetActive(true);
                TextMeshProUGUI scoreText = Instantiate(loserPlayerScorePrefab, loserPlayerScorePrefab.transform.parent);
                scoreText.gameObject.SetActive(true);
                scoreText.text = "Me: " + score;
                scoreTexts.Add(scoreText);
                foreach (var opp in opponents)
                {
                    scoreText = Instantiate(loserPlayerScorePrefab, loserPlayerScorePrefab.transform.parent);
                    print("In TickUpScore: " + opp.name + ": " + (opp.score * 10));
                    scoreText.text = opp.name + ": " + (opp.score * 10);
                    scoreText.gameObject.SetActive(true);
                    scoreTexts.Add(scoreText);
                    opp.scoreText = scoreText;
                }
            }
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