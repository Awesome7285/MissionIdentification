﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class MissionIdentificationScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    //public AudioSource SecondMusic;
    
    public KMSelectable[] TypableText;
    public KMSelectable[] ShiftButtons;
    public KMSelectable[] UselessButtons;
    public KMSelectable Backspace;
    public KMSelectable Enter;
    public KMSelectable SpaceBar;
    public KMSelectable Border;
    
    public SpriteRenderer SeedPacket;
    public Sprite[] SeedPacketIdentifier;
    public Sprite DefaultSprite;
    public Sprite Check;
    //public Sprite[] Brain;
    public Sprite SolvedSprite;
    //public Material[] ImageLighting;
    
    public MeshRenderer[] LightBulbs;
    public Material[] TheLights;
    
    public TextMesh[] Text;
    public TextMesh TextBox;
    public GameObject TheBox;
    
    bool Shifted = false, CapsLocked = false;
    
    public AudioClip[] NotBuffer;
    public AudioClip[] Buffer;
    public KMAudio.KMAudioRef bufferSound;

    public static bool playingIntroMissionID = false;
    
    string[][] ChangedText = new string[4][]{
        new string[47] {"`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "[", "]", "\\", "a", "s", "d", "f", "g", "h", "j", "k", "l", ";", "'", "z", "x", "c", "v", "b", "n", "m", ",", ".", "/"},
        new string[47] {"~", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "+", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "{", "}", "|", "A", "S", "D", "F", "G", "H", "J", "K", "L", ":", "\"", "Z", "X", "C", "V", "B", "N", "M", "<", ">", "?"},
		new string[47] {"`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/"},
		new string[47] {"~", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "+", "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "{", "}", "|", "a", "s", "d", "f", "g", "h", "j", "k", "l", ":", "\"", "z", "x", "c", "v", "b", "n", "m", "<", ">", "?"}
    };
    
    private KeyCode[] TypableKeys =
    {
        KeyCode.BackQuote, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals,
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.LeftBracket, KeyCode.RightBracket, KeyCode.Backslash,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon, KeyCode.Quote,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period, KeyCode.Slash,
    };
    
    private KeyCode[] ShiftKeys =
    {
        KeyCode.LeftShift, KeyCode.RightShift,
    };
    
    private KeyCode[] UselessKeys =
    {
        KeyCode.Tab, KeyCode.CapsLock, KeyCode.LeftControl, KeyCode.LeftWindows,  KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.RightWindows, KeyCode.Menu, KeyCode.RightControl,
    };
    
    private KeyCode[] OtherKeys =
    {
        KeyCode.Backspace, KeyCode.Return, KeyCode.Space, KeyCode.CapsLock
    };
    
    int[] Unique = {0, 0, 0};
    bool Playable = false, Enterable = false, Toggleable = true;
    private bool focused;
    int Stages = 0;
    List<int> SizeChangeValues = new List<int> {-1};
    
    //Logging
    static int moduleIdCounter = 1;
    int moduleId;

    // Stuff for giving keyboard interactions
    void Awake()
    {
        moduleId = moduleIdCounter++;
        for (int b = 0; b < TypableText.Count(); b++)
        {
            int KeyPress = b;
            TypableText[KeyPress].OnInteract += delegate
            {
                TypableKey(KeyPress);
                return false;
            };
        }
        
        for (int a = 0; a < ShiftButtons.Count(); a++)
        {
            int Shifting = a;
            ShiftButtons[Shifting].OnInteract += delegate
            {
                PressShift(Shifting);
                return false;
            };
        }
        
        for (int c = 0; c < UselessButtons.Count(); c++)
        {
            int Useless = c;
			if (Useless != 1)
			{
				UselessButtons[Useless].OnInteract += delegate
				{
					UselessButtons[Useless].AddInteractionPunch(.2f);
					Audio.PlaySoundAtTransform(NotBuffer[1].name, transform);
					return false;
				};
			}
        }
        
        Backspace.OnInteract += delegate () { PressBackspace(); return false; };
        Enter.OnInteract += delegate () { PressEnter(); return false; };
        SpaceBar.OnInteract += delegate () { PressSpaceBar(); return false; };
        Border.OnInteract += delegate () { PressBorder(); return false; };
		UselessButtons[1].OnInteract += delegate () { PressCapsLock(); return false; };
        GetComponent<KMSelectable>().OnFocus += delegate () { focused = true; };
        GetComponent<KMSelectable>().OnDefocus += delegate () { focused = false; };
        if (Application.isEditor)
            focused = true;
    }
    
    // Play sound and run introduction?
    void Start()
    {
        this.GetComponent<KMSelectable>().UpdateChildren();
        UniquePlay();
        Module.OnActivate += Introduction;
    }
    
    void Introduction()
    {
        StartCoroutine(Reintroduction());
    }
    
    // Picks the 3 sprites from the random list of sprites in SeedPacketIdentifier
    void UniquePlay()
    {
		do
		{
			for (int c = 0; c < Unique.Count(); c++)
			{
				Unique[c] = Random.Range(0, SeedPacketIdentifier.Count());
			}	
		}
        while (Unique[0] == Unique[1] || Unique[0] == Unique[2] || Unique[1] == Unique[2]);
    }
    
    // ok this one actually plays the audio
    IEnumerator Reintroduction()
    {
        if (!playingIntroMissionID)
        {
            Audio.PlaySoundAtTransform(NotBuffer[0].name, transform);
            playingIntroMissionID = true;
        }
        Intro = true;
        Debug.LogFormat("[Mission Identification #{0}] All available experts please report to room A-9!", moduleId);

        yield return new WaitForSecondsRealtime(NotBuffer[0].length);
        Playable = true;
        Intro = false;
        playingIntroMissionID = false;
    }
    
    // Type key into display, requires Playable and Enterable to be true
    void TypableKey(int KeyPress)
    {
        TypableText[KeyPress].AddInteractionPunch(.2f);
        Audio.PlaySoundAtTransform(NotBuffer[1].name, TypableText[KeyPress].transform);
        if (Playable && Enterable)
        {
            float width = 0;
            foreach (char symbol in TextBox.text)
            {
                CharacterInfo info;
                if (TextBox.font.GetCharacterInfo(symbol, out info, TextBox.fontSize, TextBox.fontStyle))
                {
                    width += info.advance;
                }
            }
            width =  width * TextBox.characterSize * 0.1f;
            
            TextBox.text += Text[KeyPress].text;
            if (width > 0.28)
            {
                TextBox.fontSize -= 15;
                SizeChangeValues.Add(TextBox.text.Length);
            }
        }
    }
    
    // Press Backspace
    void PressBackspace()
    {
        Backspace.AddInteractionPunch(.2f);
        Audio.PlaySoundAtTransform(NotBuffer[1].name, Backspace.transform);
        if (Playable)
        {
            if (TextBox.text.Length != 0)
            {
                TextBox.text = TextBox.text.Remove(TextBox.text.Length - 1);
                if (TextBox.text.Length < SizeChangeValues.Last()) {
                    TextBox.fontSize += 15;
                    SizeChangeValues.RemoveAt(SizeChangeValues.Count - 1);
                }
            }
        }
    }
    
    // Press Space, something about changing font size
    void PressSpaceBar()
    {
        SpaceBar.AddInteractionPunch(.2f);
        Audio.PlaySoundAtTransform(NotBuffer[1].name, SpaceBar.transform);
        if (Playable && Enterable)
        {
            float width = 0;
            foreach (char symbol in TextBox.text)
            {
                CharacterInfo info;
                if (TextBox.font.GetCharacterInfo(symbol, out info, TextBox.fontSize, TextBox.fontStyle))
                {
                    width += info.advance;
                }
            }
            width =  width * TextBox.characterSize * 0.1f;
            
            if (TextBox.text.Length != 0 && TextBox.text[TextBox.text.Length - 1].ToString() != " ")
            {
                TextBox.text += " ";
                if (width > 0.28)
                {
                    TextBox.fontSize -= 15;
                    SizeChangeValues.Add(TextBox.text.Length);
                }
            }
        }
    }
    
    // Start PlayTheQueue()
    void PressBorder()
    {
        Border.AddInteractionPunch(.2f);
        if (Playable && Toggleable)
        {
            StartCoroutine(PlayTheQueue());
            //PlayTheQueue();
        }
    }
    
    // Start TheCorrect()
    void PressEnter()
    {
        Enter.AddInteractionPunch(.2f);
        Audio.PlaySoundAtTransform(NotBuffer[1].name, Enter.transform);
        if (Playable && Enterable)
        {
            StartCoroutine(TheCorrect());
        }
    }
	
    // Change to caps
	void PressCapsLock()
    {
		UselessButtons[1].AddInteractionPunch(.2f);
        Audio.PlaySoundAtTransform(NotBuffer[1].name, UselessButtons[1].transform);
        if (Playable && Enterable)
        {
			CapsLocked = CapsLocked ? false : true;
			for (int b = 0; b < Text.Count(); b++)
			{
				Text[b].text = Shifted ? CapsLocked ? ChangedText[3][b] : ChangedText[1][b] : CapsLocked ? ChangedText[2][b] : ChangedText[0][b];
			}
		}
	}
    
    // also change to caps
    void PressShift(int Shifting)
    {
        ShiftButtons[Shifting].AddInteractionPunch(.2f);
        Audio.PlaySoundAtTransform(NotBuffer[1].name, ShiftButtons[Shifting].transform);
		if (Playable && Enterable)
        {
			StartingNumber = Shifted ? 0 : 1;
			Shifted = Shifted ? false: true;
			for (int b = 0; b < Text.Count(); b++)
			{
				Text[b].text = Shifted ? CapsLocked ? ChangedText[3][b] : ChangedText[1][b] : CapsLocked ? ChangedText[2][b] : ChangedText[0][b];
			}
		}
    }
    
    // Show image, wait 5 seconds, hide image
    IEnumerator PlayTheQueue()
    {
        Toggleable = false;
        Debug.LogFormat("[Mission Identification #{0}] The mission's name that was shown: {1}", moduleId, FixIdentifierName());
        SeedPacket.sprite = SeedPacketIdentifier[Unique[Stages]];
        AudioClip chosenTrack = Buffer[Random.Range(0,5)];
        bufferSound = Audio.PlaySoundAtTransformWithRef(chosenTrack.name, transform);
        // SecondMusic.clip = Buffer[Random.Range(0,5)];
        // SecondMusic.Play();
        Enterable = true;
        yield return new WaitForSecondsRealtime(chosenTrack.length);
        bufferSound.StopSound();
    }

    string FixIdentifierName() {
        string bombNameFixed = SeedPacketIdentifier[Unique[Stages]].name;
        bombNameFixed = bombNameFixed.Replace("!!c", ":");
        bombNameFixed = bombNameFixed.Replace("!!f", "/");
        bombNameFixed = bombNameFixed.Replace("!!b", "\\");
        bombNameFixed = bombNameFixed.Replace("!!q", "?");
        bombNameFixed = bombNameFixed.Replace("!!a", "*");
        bombNameFixed = bombNameFixed.Replace("!!l", "<");
        bombNameFixed = bombNameFixed.Replace("!!g", ">");
        bombNameFixed = bombNameFixed.Replace("!!u", "\"");
        return bombNameFixed;
    }
    
    IEnumerator TheCorrect()
    {
        if (TextBox.text.Length != 0 && TextBox.text[TextBox.text.Length - 1].ToString() == " ")
            TextBox.text = TextBox.text.Remove(TextBox.text.Length - 1);
        string Analysis = TextBox.text.Replace('`', '\'');
        Debug.LogFormat("[Mission Identification #{0}] Text that was submitted: {1}", moduleId, Analysis);
        /* KEYS: these values are not usable in windows filenames
        !!c = colon
        !!f = forward slash
        !!b = backslash
        !!q = question mark
        !!a = asterisk
        !!l = less than
        !!g = greater than
        !!u = qUote mark
        */
        bufferSound.StopSound();
        //SecondMusic.Stop();
        if (Analysis == FixIdentifierName())
        {
            TextBox.text = "";
            TextBox.fontSize = 175;
            Stages++;
            Playable = false;
            Enterable = false;
            if (Stages == 3)
            {
                Animating1 = true;
                Debug.LogFormat("[Mission Identification #{0}] You correctly guessed the mission three times in a row. GG!", moduleId);
                Audio.PlaySoundAtTransform(NotBuffer[3].name, transform);
                // SecondMusic.clip = NotBuffer[3];
                // SecondMusic.Play();
                StartCoroutine(RoulleteToWin(NotBuffer[3].length));
                StartCoroutine(SolveAnimation(NotBuffer[3].length));
                // yield return new WaitForSecondsRealtime(NotBuffer[3].length);
                // bufferSound.StopSound();
            }
            
            else
            {
                Debug.LogFormat("[Mission Identification #{0}] The text matches the name of the mission. Good job!", moduleId);
                Animating1 = true;
                SeedPacket.sprite = Check;
                Audio.PlaySoundAtTransform(NotBuffer[2].name, transform);
                // SecondMusic.clip = NotBuffer[2];
                // SecondMusic.Play();
                StartCoroutine(CorrectAnimation(1f));
                yield return new WaitForSecondsRealtime(NotBuffer[2].length);
                
            }
        }
        
        else
        {
            Debug.LogFormat("[Mission Identification #{0}] The text does not match the name of the mission. Oh no!", moduleId);
            // SecondMusic.clip = NotBuffer[4];
            // SecondMusic.Play();
            // StrikeIncoming = true;
            // Animating1 = true;
            // SeedPacket.sprite = CheckOrCross[1];
            // Enterable = false;
            
            // LightBulbs[0].material = TheLights[2];
            // LightBulbs[1].material = TheLights[0];
            // LightBulbs[2].material = TheLights[2];
            // yield return new WaitForSecondsRealtime(0.4f);
            // LightBulbs[0].material = TheLights[0];
            // LightBulbs[1].material = TheLights[2];
            // LightBulbs[2].material = TheLights[0];
            // yield return new WaitForSecondsRealtime(0.4f);
            // LightBulbs[0].material = TheLights[2];
            // LightBulbs[1].material = TheLights[2];
            // LightBulbs[2].material = TheLights[2];
            // yield return new WaitForSecondsRealtime(0.4f);
            // LightBulbs[0].material = TheLights[0];
            // LightBulbs[1].material = TheLights[0];
            // LightBulbs[2].material = TheLights[0];
            // yield return new WaitForSecondsRealtime(0.6f);
            // for (int x = 0; x < 8; x++)
            // {
            //     LightBulbs[0].material = TheLights[2];
            //     LightBulbs[1].material = TheLights[2];
            //     LightBulbs[2].material = TheLights[2];
            //     yield return new WaitForSecondsRealtime(0.06f);
            //     LightBulbs[0].material = TheLights[0];
            //     LightBulbs[1].material = TheLights[0];
            //     LightBulbs[2].material = TheLights[0];
            //     yield return new WaitForSecondsRealtime(0.06f);
            // }
            // LightBulbs[0].material = TheLights[2];
            // LightBulbs[1].material = TheLights[2];
            // LightBulbs[2].material = TheLights[2];
            // SeedPacket.sprite = Brain[0];
            // yield return new WaitForSecondsRealtime(2.4f);
            // SeedPacket.sprite = Brain[1];
            Debug.LogFormat("[Mission Identification #{0}] Back to profiles it is...", moduleId);
            // yield return new WaitForSecondsRealtime(3.6f);
            // SeedPacket.sprite = DefaultSprite;
            // LightBulbs[0].material = TheLights[0];
            // LightBulbs[1].material = TheLights[0];
            // LightBulbs[2].material = TheLights[0];
            // Playable = true;
            // Toggleable = true;
            // Animating1 = false;
            // StrikeIncoming = false;
            // Stages = 0;
            Module.HandleStrike();
            // Debug.LogFormat("[Mission Identification #{0}] The module resetted and striked as a cost for giving an incorrect answer.", moduleId);
            // UniquePlay();
        }
    }
    
    // IEnumerator RoulleteToWin()
    // {
    //     while (roulleteHappening)
    //     {
    //         SeedPacket.sprite = SeedPacketIdentifier[Random.Range(0, SeedPacketIdentifier.Count())];
    //         yield return new WaitForSecondsRealtime(0.1f);
    //     }
    // }
    IEnumerator RoulleteToWin(float length)
    {
        float currentTime = 0;
        while (currentTime < length)
        {
            currentTime += Time.deltaTime;

            for (int x = 0; x < 3; x++)
            {
                SeedPacket.sprite = SeedPacketIdentifier[Unique[x]];
                yield return new WaitForSecondsRealtime(0.15f);
                currentTime += .15f;
            }

            yield return null;
        }

    }

    IEnumerator SolveAnimation(float length) {
        float currentTime = 0;
        while (currentTime < length)
        {
            currentTime += Time.deltaTime;

            LightBulbs[0].material = TheLights[0];
            LightBulbs[1].material = TheLights[0];
            LightBulbs[2].material = TheLights[1];
            yield return new WaitForSecondsRealtime(0.02f);
            LightBulbs[0].material = TheLights[0];
            LightBulbs[1].material = TheLights[1];
            LightBulbs[2].material = TheLights[0];
            yield return new WaitForSecondsRealtime(0.02f);
            LightBulbs[0].material = TheLights[1];
            LightBulbs[1].material = TheLights[0];
            LightBulbs[2].material = TheLights[0];
            yield return new WaitForSecondsRealtime(0.02f);

            currentTime += .06f;
            yield return null;
        }
        LightBulbs[0].material = TheLights[1];
        LightBulbs[1].material = TheLights[1];
        LightBulbs[2].material = TheLights[1];
        SeedPacket.sprite = SolvedSprite;
        Debug.LogFormat("[Mission Identification #{0}] The module is done.", moduleId);
        Module.HandlePass();
        Animating1 = false;
    }

    IEnumerator CorrectAnimation(float length) {
        float currentTime = 0;
        while (currentTime < length)
        {
            currentTime += Time.deltaTime;

            LightBulbs[Stages-1].material = TheLights[1];
            yield return new WaitForSecondsRealtime(0.075f);
            LightBulbs[Stages-1].material = TheLights[0];
            yield return new WaitForSecondsRealtime(0.075f);

            currentTime += .05f;
            yield return null;
        }

        LightBulbs[Stages-1].material = TheLights[1];
        SeedPacket.sprite = DefaultSprite;
        //bufferSound.StopSound();
        Playable = true;
        Toggleable = true;
        Animating1 = false;
    }
    
    // Change some keys depending on focus
    void Update()
    {
        if (focused)
        {
            for (int i = 0; i < TypableKeys.Count(); i++)
            {
                if (Input.GetKeyDown(TypableKeys[i]))
                {
                    TypableText[i].OnInteract();
                }
            }
            for (int j = 0; j < ShiftKeys.Count(); j++)
            {
                if ((Input.GetKeyDown(ShiftKeys[j]) && !Shifted) || Input.GetKeyUp(ShiftKeys[j]))
                {
                    ShiftButtons[j].OnInteract();
                }
            }
            for (int k = 0; k < UselessKeys.Count(); k++)
            {
                if (Input.GetKeyDown(UselessKeys[k]) && k != 1)
                {
                    UselessButtons[k].OnInteract();
                }
            }
            for (int l = 0; l < OtherKeys.Count(); l++)
            {
                if (Input.GetKeyDown(OtherKeys[l]))
                {
                    switch (l)
                    {
                        case 0:
                            Backspace.OnInteract(); break;
                        case 1:
                            Enter.OnInteract(); break;
                        case 2:
                            SpaceBar.OnInteract(); break;
						case 3:
							UselessButtons[1].OnInteract(); break;
                        default:
                            break;
                    }
                }
            }
        }
    }
    
    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To press the border in the module, use the command !{0} play, or !{0} playfocus | To type a text in the text box, use the command !{0} type <text> | To submit the text in the text box, use the command !{0} submit | To clear the text in the text box, use the command !{0} clear, or !{0} fastclear";
    #pragma warning restore 414
    
    int StartingNumber = 0;
    bool Intro = false;
    bool Animating1 = false;
    bool StrikeIncoming = false;
    string Current = "";
    
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*type\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            
            if (Intro == true)
            {
                yield return "sendtochaterror The introduction music is still playing. Command was ignored";
                yield break;
            }
            
            if (Animating1 == true)
            {
                yield return "sendtochaterror The module is performing an animation. Command was ignored";
                yield break;
            }
            
            if (Enterable == false)
            {
                yield return "sendtochaterror The keys are not yet pressable. Command was ignored";
                yield break;
            }
            
            for (int x = 0; x < parameters.Length - 1; x++)
            {
                foreach (char c in parameters[x+1])
                {
                    if (!c.ToString().EqualsAny(ChangedText[0]) && !c.ToString().EqualsAny(ChangedText[1]))
                    {
                        yield return "sendtochaterror The command being submitted contains a character that is not typable in the given keyboard";
                        yield break;
                    }
                }
            }
			
			if (CapsLocked)
			{
				UselessButtons[1].OnInteract();
				yield return new WaitForSeconds(0.01f);
			}
            
            for (int y = 0; y < parameters.Length - 1; y++)
            {
                yield return "trycancel The command to type the text given was halted due to a cancel request";
                foreach (char c in parameters[y+1])
                {
                    yield return "trycancel The command to type the text given was halted due to a cancel request";
                    Current = TextBox.text;
                    if (!c.ToString().EqualsAny(ChangedText[StartingNumber]))
                    {
                        ShiftButtons[0].OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                    
                    for (int z = 0; z < ChangedText[StartingNumber].Count(); z++)
                    {
                        if (c.ToString() == ChangedText[StartingNumber][z])
                        {
                            TypableText[z].OnInteract();
                            yield return new WaitForSeconds(0.05f);
                            break;
                        }
                    }
                    
                    if (Current == TextBox.text)
                    {
                        yield return "sendtochaterror The command was stopped due to the text box not able to recieve more characters";
                        yield break;
                    }
                }

                if (y != parameters.Length - 2)
                {
                    SpaceBar.OnInteract();
                    yield return new WaitForSeconds(0.05f);
                }
                
                if (Current == TextBox.text)
                {
                    yield return "sendtochaterror The command was stopped due to the text box not able to recieve more characters";
                    yield break;
                }
            }
        }
        
        else if (Regex.IsMatch(command, @"^\s*clear\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            
            if (Intro == true)
            {
                yield return "sendtochaterror The introduction music is still playing. Command was ignored";
                yield break;
            }
            
            if (Animating1 == true)
            {
                yield return "sendtochaterror The module is performing an animation. Command was ignored";
                yield break;
            }
            
            if (Enterable == false)
            {
                yield return "sendtochaterror The key is not yet pressable. Command was ignored";
                yield break;
            }
            
            while (TextBox.text.Length != 0)
            {
                yield return "trycancel The command to clear text in the text box was halted due to a cancel request";
                Backspace.OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        else if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            
            if (Intro == true)
            {
                yield return "sendtochaterror The introduction music is still playing. Command was ignored";
                yield break;
            }
            
            if (Animating1 == true)
            {
                yield return "sendtochaterror The module is performing an animation. Command was ignored";
                yield break;
            }
            
            if (Enterable == false)
            {
                yield return "sendtochaterror The key is not yet pressable. Command was ignored";
                yield break;
            }
            yield return "solve";
            yield return "strike";
                Enter.OnInteract();
        }
        
        else if (Regex.IsMatch(command, @"^\s*play\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            
            if (Intro == true)
            {
                yield return "sendtochaterror The introduction music is still playing. Command was ignored.";
                yield break;
            }
            
            if (Animating1 == true)
            {
                yield return "sendtochaterror The module is performing an animation. Command was ignored";
                yield break;
            }
            
            if (Enterable == true)
            {
                yield return "sendtochaterror You are not able to press the border again. Command was ignored";
                yield break;
            }
            
            Border.OnInteract();
        }
        
        else if (Regex.IsMatch(command, @"^\s*playfocus\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            
            if (Intro == true)
            {
                yield return "sendtochaterror The introduction music is still playing. Command was ignored";
                yield break;
            }
            
            if (Animating1 == true)
            {
                yield return "sendtochaterror The module is performing an animation. Command was ignored";
                yield break;
            }
            
            if (Enterable == true)
            {
                yield return "sendtochaterror You are not able to press the border again. Command was ignored";
                yield break;
            }
            
            Border.OnInteract();
            while (Playable == false)
            {
                yield return new WaitForSeconds(0.02f);
            }
        }
        
        else if (Regex.IsMatch(command, @"^\s*fastclear\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            
            if (Intro == true)
            {
                yield return "sendtochaterror The introduction music is still playing. Command was ignored.";
                yield break;
            }
            
            if (Animating1 == true)
            {
                yield return "sendtochaterror The module is performing an animation. Command was ignored";
                yield break;
            }
            
            if (Enterable == false)
            {
                yield return "sendtochaterror The key is not yet pressable. Command was ignored";
                yield break;
            }
            
            while (TextBox.text.Length != 0)
            {
                Backspace.OnInteract();
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (StrikeIncoming)
        {
            StopAllCoroutines();
            LightBulbs[0].material = TheLights[1];
            LightBulbs[1].material = TheLights[1];
            LightBulbs[2].material = TheLights[1];
            SeedPacket.sprite = SolvedSprite;
            Module.HandlePass();
            yield break;
        }
        int start = Stages;
        for (int i = start; i < 3; i++)
        {
            while (!Playable) { yield return true; }
            if (Toggleable)
                Border.OnInteract();
            while (!Enterable) { yield return true; }
            if (TextBox.text != SeedPacketIdentifier[Unique[i]].name)
            {
                int clearNum = -1;
                for (int j = 0; j < TextBox.text.Length; j++)
                {
                    if (j == SeedPacketIdentifier[Unique[i]].name.Length)
                        break;
                    if (TextBox.text[j] != SeedPacketIdentifier[Unique[i]].name[j])
                    {
                        clearNum = j;
                        int target = TextBox.text.Length - j;
                        for (int k = 0; k < target; k++)
                        {
                            Backspace.OnInteract();
                            yield return new WaitForSeconds(0.05f);
                        }
                        break;
                    }
                }
                if (clearNum == -1)
                {
                    if (TextBox.text.Length > SeedPacketIdentifier[Unique[i]].name.Length)
                    {
                        while (TextBox.text.Length > SeedPacketIdentifier[Unique[i]].name.Length)
                        {
                            Backspace.OnInteract();
                            yield return new WaitForSeconds(0.05f);
                        }
                    }
                    else
                        yield return ProcessTwitchCommand("type " + SeedPacketIdentifier[Unique[i]].name.Substring(TextBox.text.Length));
                }
                else
                    yield return ProcessTwitchCommand("type " + SeedPacketIdentifier[Unique[i]].name.Substring(clearNum));
            }
            Enter.OnInteract();
        }
        while (Animating1) { yield return true; }
    }
}
