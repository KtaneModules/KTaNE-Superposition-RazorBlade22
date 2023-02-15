using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class SuperpositionScript : MonoBehaviour
{

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMSelectable[] Buttons;
    public TextMesh[] Displays;
    public KMColorblindMode colorblindMode;

    private static readonly string[,] colorGrid =
    {
        {"ROYBGVW","BVOGYRW","OYWVBGR","WVGRBYO","YWVBGRO","VBYROWG","WRYGVBO"},
        {"YWRVGOB","ORWGVBY","BVGORWY","VGOWBRY","OYRBWVG","ROWYVBG","GYBWOVR"},
        {"BRGWYOV","VWOYGRB","YGRWBOV","BYOWVRG","ROYWBVG","BGYWORV","RGOYWBV"},
        {"GYVRWBO","RWOBVYG","GRYOBVW","GOVBWRY","GVRWYOB","YGVWORB","VOWRGYB"},
        {"OBYWRVG","VWRBYOG","WGOYBVR","YRWBOVG","BGVOWYR","WOBVGRY","GOBYWVR"},
        {"WGVRBYO","YRVGBOW","RWYBGVO","OVBGRWY","YGORWVB","VGROYWB","OVRYBGW"},
        {"YWBVOGR","GRBYVWO","BWVORGY","WGYVORB","VORWYBG","RWGYOBV","WVGBYOR"}
    };
    private static readonly string[][] cellMatching =
    {
        new string[] { "A--", "Y5Y", "-8R", "-4B", "T-G", "F1-", "J-O", "Q5-", "--R", "I3V" },
        new string[] { "P0-", "X-V", "R0Y", "K-G", "-6-", "O0O", "-1G", "--B", "Z9-", "-5R" },
        new string[] { "B0W", "-6Y", "--G", "-3-", "U2V", "-9O", "C7-", "M-G", "W-W", "E4-" },
        new string[] { "-8B", "-0-", "G-O", "L1-", "A5-", "--W", "X9B", "H2O", "-8Y", "R-G" },
        new string[] { "D-R", "N2-", "V7-", "M7B", "-2W", "G-Y", "-8-", "-6V", "S4R", "Z--" },
    };
    private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private string topAlphabet, rightAlphabet;
    private char[] topLetters, rightLetters;
    private int[] topNumbers = new int[25], rightNumbers = new int[25], topColors = new int[25], rightColors = new int[25];
    private int blankDisplay;
    private bool colorblind;
    private int super = 0; // 0 = Top, 1 = Right
    private int[] missingLetters = new int[2];
    private bool[] greyButtons = new bool[25];
    private string modifiedSerialNumber;
    private int serialSum;
    private int[] validCells = new int[25];
    private int solveCheck;
    private int strikeCheck;
    private bool solved;
    private int startDate;
    private bool[] pressing = new bool[26];
    private bool canNotPress;
    private string[] coordinates = { "A1", "B1", "C1", "D1", "E1", "A2", "B2", "C2", "D2", "E2", "A3", "B3", "C3", "D3", "E3", "A4", "B4", "C4", "D4", "E4", "A5", "B5", "C5", "D5", "E5" };
    private float tpDelay = 3;

    private readonly Color[] colors = { new Color(1f, 0f, 0f, 1f), new Color(0.75f, 0.5f, 0f, 1f), new Color(1f, 1f, 0f, 1f), new Color(0f, 1f, 0f, 1f), new Color(0f, 0f, 1f, 1f), new Color(0.4f, 0f, .7f, 1f), new Color(1f, 1f, 1f, 1f) };
    private readonly string[] colorSymbols = { "R", "O", "Y", "G", "B", "V", "W" };

    // Colors
    // 0 = Red
    // 1 = Orange
    // 2 = Yellow
    // 3 = Green
    // 4 = Blue
    // 5 = Violet
    // 6 = White

    //Digital Root function
    private static int DigitalRoot(int n)
    {
        var root = 0;
        while (n > 0 || root > 9)
        {
            if (n == 0)
            {
                n = root;
                root = 0;
            }

            root += n % 10;
            n /= 10;
        }
        return root;
    }

    void Awake()
    {
        //Randomizer for alphabets
        _moduleID = _moduleIdCounter++;
        topAlphabet = rightAlphabet = alphabet;
        missingLetters[0] = Rnd.Range(0, 26);
        missingLetters[1] = Rnd.Range(0, 26);
        topAlphabet = topAlphabet.Remove(missingLetters[0], 1);
        rightAlphabet = rightAlphabet.Remove(missingLetters[1], 1);
        topLetters = topAlphabet.ToCharArray();
        rightLetters = rightAlphabet.ToCharArray();
        for (int i = 24; i > 0; i--)
        {
            //Shuffle code
            int place1 = Rnd.Range(0, i);
            char tmp1 = topLetters[place1];
            topLetters[place1] = topLetters[i];
            topLetters[i] = tmp1;
            int place2 = Rnd.Range(0, i);
            char tmp2 = rightLetters[place2];
            rightLetters[place2] = rightLetters[i];
            rightLetters[i] = tmp2;
        }
        //Debug.Log(topLetters.Join());
        //Debug.Log(rightLetters.Join());


        //Randomizer for Numbers and Colors
        for (int i = 0; i < 25; i++)
        {
            topNumbers[i] = Rnd.Range(0, 10);
            rightNumbers[i] = Rnd.Range(0, 10);
            topColors[i] = Rnd.Range(0, 7);
            rightColors[i] = Rnd.Range(0, 7);
        }

        //Debug.Log(topNumbers.Join());
        //Debug.Log(rightNumbers.Join());
        //Debug.Log(topColors.Join());
        //Debug.Log(rightColors.Join());

        foreach (KMSelectable button in Buttons)
        {
            button.OnInteract += delegate () { if (!pressing[Array.IndexOf(Buttons, button)] && !canNotPress && !solved) ButtonPress(Array.IndexOf(Buttons, button)); return false; };
            if (Array.IndexOf(Buttons, button) != 25)
            {
                button.OnHighlight += delegate () { if (!solved && !canNotPress) ButtonHL(Array.IndexOf(Buttons, button)); };
                button.OnHighlightEnded += delegate () { if (!solved && !canNotPress) ButtonHLEnd(Array.IndexOf(Buttons, button)); };
            }
        }

        //Randomizer for Blank Display
        if (Rnd.Range(0, 10) == 0)
        {
            blankDisplay = Rnd.Range(1, 51);
        }

        //Logging
        for (int i = 0; i < 2; i++)
        {
            Debug.LogFormat("[Superposition #{0}] {1} Display:", _moduleID, new string[]{
            "Top", "Right"}[i]);

            for (int j = 0; j < 5; j++)
            {
                List<string> log = new List<string>();

                for (int k = 0; k < 5; k++)
                {
                    if (((j * 5) + k + 1) + (i * 25) != blankDisplay)
                    {
                        log.Add(i == 0 ? topLetters[(j * 5) + k].ToString() + topNumbers[(j * 5) + k].ToString() + colorSymbols[topColors[(j * 5) + k]]
                          : rightLetters[(j * 5) + k].ToString() + rightNumbers[(j * 5) + k].ToString() + colorSymbols[rightColors[(j * 5) + k]]);
                    }
                    else
                    {
                        log.Add("???");
                    }
                }

                Debug.LogFormat("[Superposition #{0}] {1}", _moduleID, log.Join());
            }

            Debug.LogFormat("[Superposition #{0}] Missing Letter: {1}", _moduleID, alphabet[missingLetters[i]]);
        }

        //Start up Sound
        Module.OnActivate += delegate
        {
            Audio.PlaySoundAtTransform("Activate", Buttons[12].transform);
        };

    }

    // Use this for initialization
    void Start()
    {
        startDate = (int)DateTime.Now.DayOfWeek;
        colorblind = colorblindMode.ColorblindModeActive;
        serialSum = Bomb.GetSerialNumberNumbers().Sum();
        StartCoroutine(CollapsedSuperpostion());
    }

    // Update is called once per frame
    void Update()
    {
        if (!solved)
        {
            if (solveCheck != Bomb.GetSolvedModuleIDs().Count || strikeCheck != Bomb.GetStrikes())
            {
                validCells = new int[25];
                LetterCiphers();
                solveCheck = Bomb.GetSolvedModuleIDs().Count();
                strikeCheck = Bomb.GetStrikes();
            }
        }
    }

    private void ButtonHL(int buttonID)
    {

        if (blankDisplay == buttonID + 1)
        {
            Displays[0].text = "";
            Displays[1].text = rightLetters[buttonID].ToString() + rightNumbers[buttonID].ToString() + (colorblind ? colorSymbols[rightColors[buttonID]] : "");
            Displays[1].color = colors[rightColors[buttonID]];
        }
        else if (blankDisplay == buttonID + 26)
        {
            Displays[0].text = Displays[0].text = topLetters[buttonID].ToString() + topNumbers[buttonID].ToString() + (colorblind ? colorSymbols[topColors[buttonID]] : "");
            Displays[1].text = "";
            Displays[0].color = colors[topColors[buttonID]];
        }
        else
        {
            Displays[0].text = topLetters[buttonID].ToString() + topNumbers[buttonID].ToString() + (colorblind ? colorSymbols[topColors[buttonID]] : "");
            Displays[1].text = rightLetters[buttonID].ToString() + rightNumbers[buttonID].ToString() + (colorblind ? colorSymbols[rightColors[buttonID]] : "");
            Displays[0].color = colors[topColors[buttonID]];
            Displays[1].color = colors[rightColors[buttonID]];
        }

    }

    private void ButtonHLEnd(int buttonID)
    {
        Displays[0].text = "Super";
        Displays[1].text = "Position";
        Displays[0].color = colors[6];
        Displays[1].color = colors[6];
    }

    private void LetterCiphers()
    {
        int[] validCellsStep1 = new int[25];
        modifiedSerialNumber = "";

        //Debug.Log(alphabet[displayRandom[Super]]);

        Debug.LogFormat("[Superposition #{0}] Step #1: Letter Ciphers", _moduleID);

        //Missing Letter
        Debug.LogFormat("[Superposition #{0}] Doing Missing Letter Conversion...", _moduleID);
        if (Bomb.GetSerialNumberLetters().Contains(alphabet[missingLetters[super]]))
        {
            Debug.LogFormat("[Superposition #{0}] The missing letter is present in the Serial Number.", _moduleID);
            Debug.LogFormat("[Superposition #{0}] Following Missing Letter Conversion...", _moduleID);

            int position = super == 0 ? Array.IndexOf(topLetters, alphabet[(missingLetters[0] + 1 + Bomb.GetSerialNumberNumbers().Last()) % 26]) : Array.IndexOf(rightLetters, alphabet[(missingLetters[1] + 1 + Bomb.GetSerialNumberNumbers().Last()) % 26]);
            Debug.LogFormat("[Superposition #{0}] {1} Caesar shifted up by {2} is {3}.", _moduleID, alphabet[missingLetters[super]], 1 + Bomb.GetSerialNumberNumbers().Last(), alphabet[(missingLetters[super] + 1 + Bomb.GetSerialNumberNumbers().Last()) % 26]);
            Debug.LogFormat("[Superposition #{0}] This letter is located in {1}.", _moduleID, coordinates[position]);

            if (alphabet.Contains(Bomb.GetSerialNumber().First()) && alphabet.Contains(Bomb.GetSerialNumber()[1]))
            {
                position = (position + 20) % 25;
                Debug.LogFormat("[Superposition #{0}] Serial begins with letter, letter. Moving up.", _moduleID);
            }
            else if (!alphabet.Contains(Bomb.GetSerialNumber().First()) && !alphabet.Contains(Bomb.GetSerialNumber()[1]))
            {
                position = (position + 5) % 25;
                Debug.LogFormat("[Superposition #{0}] Serial begins with number, number. Moving down.", _moduleID);
            }
            else if (alphabet.Contains(Bomb.GetSerialNumber().First()) && !alphabet.Contains(Bomb.GetSerialNumber()[1]))
            {
                Debug.LogFormat("[Superposition #{0}] Serial begins with letter, number. Moving left.", _moduleID);

                if (position % 5 == 0)
                {
                    position = position + 4;
                }
                else
                {
                    position--;
                }
            }
            else
            {
                Debug.LogFormat("[Superposition #{0}] Serial begins with number, letter. Moving right.", _moduleID);

                if (position % 5 == 4)
                {
                    position = position - 4;
                }
                else
                {
                    position++;
                }
            }
            Debug.LogFormat("[Superposition #{0}] New letter is {1}.", _moduleID, (super == 0 ? topLetters[position] : rightLetters[position]));

            if (super == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (alphabet[missingLetters[0]] == Bomb.GetSerialNumber()[i])
                    {
                        modifiedSerialNumber += topLetters[position];
                    }
                    else
                    {
                        modifiedSerialNumber += Bomb.GetSerialNumber()[i];
                    }

                }
                //Debug.Log(alphabet[displayRandom[0]]);
                //Debug.Log(topLetters[position]);
            }
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    if (alphabet[missingLetters[1]] == Bomb.GetSerialNumber()[i])
                    {
                        modifiedSerialNumber += rightLetters[position];
                    }
                    else
                    {
                        modifiedSerialNumber += Bomb.GetSerialNumber()[i];
                    }

                }
                //Debug.Log(alphabet[displayRandom[1]]);
                //Debug.Log(rightLetters[position]);
            }
        }
        else
        {
            modifiedSerialNumber = Bomb.GetSerialNumber();
            Debug.LogFormat("[Superposition #{0}] The missing letter is not present in the Serial Number.", _moduleID);
        }
        Debug.LogFormat("[Superposition #{0}] The Cipher Code is {1}.", _moduleID, modifiedSerialNumber);

        //Number Conversion
        Debug.LogFormat("[Superposition #{0}] Doing Number Conversion...", _moduleID);
        for (int i = 0; i < 6; i++)
        {
            if ("0123456789".Contains(Bomb.GetSerialNumber()[i]))
            {
                int number = int.Parse(Bomb.GetSerialNumber()[i].ToString());

                Debug.LogFormat("[Superposition #{0}] The {1} character of the serial number is {2}, which is a number.", _moduleID, new string[] { "1st", "2nd", "3rd", "4th", "5th", "6th" }[i], number);

                if (number == 0)
                {
                    number = 10;
                    Debug.LogFormat("[Superposition #{0}] This number is 0, changing it to 10.", _moduleID);
                }

                Debug.LogFormat("[Superposition #{0}] {1} × {2} = {3}.", _moduleID, number, i + 1, number * (i + 1));
                number *= (i + 1);

                while (number < 10)
                {
                    Debug.LogFormat("[Superposition #{0}] Number is a single digit. New number is {1}", _moduleID, number * 2);
                    number *= 2;
                }

                Debug.LogFormat("[Superposition #{0}] The digital root of the number is {1}, therefore the number added to its digital root is {2}.", _moduleID, DigitalRoot(number), number + DigitalRoot(number));
                number += DigitalRoot(number);

                Debug.LogFormat("[Superposition #{0}] The number {1}, modulo 26, is {2}.", _moduleID, number, number % 26);
                number = number % 26;
                Debug.LogFormat("[Superposition #{0}] The number {1} into a letter is {2}.", _moduleID, number, alphabet[number]);

                if ((super == 0 && !topLetters.Contains(alphabet[number])) || (super == 1 && !rightLetters.Contains(alphabet[number])))
                {
                    number = (number + 1) % 26;
                    Debug.LogFormat("[Superposition #{0}] The letter is missing from the Superposion. The new letter is {1}.", _moduleID, alphabet[number]);
                }

                char[] modifiedSerialCharacters = modifiedSerialNumber.ToCharArray();
                modifiedSerialCharacters[i] = alphabet[number];
                modifiedSerialNumber = new string(modifiedSerialCharacters);
            }
        }

        //Debug.Log(modifiedSerialNumber);
        Debug.LogFormat("[Superposition #{0}] The new Cipher Code is {1}.", _moduleID, modifiedSerialNumber);

        //Playfair Cipher
        for (int i = 0; i < 3; i++)
        {
            char letterA = modifiedSerialNumber[2 * i];
            char letterB = modifiedSerialNumber[2 * i + 1];
            char newLetterA = letterA;
            char newLetterB = letterB;

            //Same
            if (letterA == letterB)
            {
                newLetterA = letterA;
                newLetterB = letterB;
            }
            //Same Column
            else if (super == 0 ? System.Array.IndexOf(topLetters, letterA) % 5 == System.Array.IndexOf(topLetters, letterB) % 5 : System.Array.IndexOf(rightLetters, letterA) % 5 == System.Array.IndexOf(rightLetters, letterB) % 5)
            {
                if (super == 0)
                {
                    newLetterA = topLetters[(System.Array.IndexOf(topLetters, letterA) + 5) % 25];
                    newLetterB = topLetters[(System.Array.IndexOf(topLetters, letterB) + 5) % 25];
                }
                else
                {
                    newLetterA = rightLetters[(System.Array.IndexOf(rightLetters, letterA) + 5) % 25];
                    newLetterB = rightLetters[(System.Array.IndexOf(rightLetters, letterB) + 5) % 25];
                }
            }
            //Same Row
            else if (super == 0 ? System.Array.IndexOf(topLetters, letterA) / 5 == System.Array.IndexOf(topLetters, letterB) / 5 : System.Array.IndexOf(rightLetters, letterA) / 5 == System.Array.IndexOf(rightLetters, letterB) / 5)
            {
                if (super == 0)
                {
                    if (System.Array.IndexOf(topLetters, letterA) % 5 == 4)
                    {
                        newLetterA = topLetters[System.Array.IndexOf(topLetters, letterA) - 4];
                    }
                    else
                    {
                        newLetterA = topLetters[System.Array.IndexOf(topLetters, letterA) + 1];
                    }

                    if (System.Array.IndexOf(topLetters, letterB) % 5 == 4)
                    {
                        newLetterB = topLetters[System.Array.IndexOf(topLetters, letterB) - 4];
                    }
                    else
                    {
                        newLetterB = topLetters[System.Array.IndexOf(topLetters, letterB) + 1];
                    }
                }
                else
                {
                    if (System.Array.IndexOf(rightLetters, letterA) % 5 == 4)
                    {
                        newLetterA = rightLetters[System.Array.IndexOf(rightLetters, letterA) - 4];
                    }
                    else
                    {
                        newLetterA = rightLetters[System.Array.IndexOf(rightLetters, letterA) + 1];
                    }

                    if (System.Array.IndexOf(rightLetters, letterB) % 5 == 4)
                    {
                        newLetterB = rightLetters[System.Array.IndexOf(rightLetters, letterB) - 4];
                    }
                    else
                    {
                        newLetterB = rightLetters[System.Array.IndexOf(rightLetters, letterB) + 1];
                    }
                }
            }
            //Playfair
            else
            {

                if (super == 0)
                {
                    if (System.Array.IndexOf(topLetters, letterA) % 5 > System.Array.IndexOf(topLetters, letterB) % 5)
                    {
                        newLetterA = topLetters[System.Array.IndexOf(topLetters, letterA) - ((System.Array.IndexOf(topLetters, letterA) % 5) - (System.Array.IndexOf(topLetters, letterB) % 5))];
                        newLetterB = topLetters[System.Array.IndexOf(topLetters, letterB) + ((System.Array.IndexOf(topLetters, letterA) % 5) - (System.Array.IndexOf(topLetters, letterB) % 5))];
                    }
                    else
                    {
                        newLetterA = topLetters[System.Array.IndexOf(topLetters, letterA) + ((System.Array.IndexOf(topLetters, letterB) % 5) - (System.Array.IndexOf(topLetters, letterA) % 5))];
                        newLetterB = topLetters[System.Array.IndexOf(topLetters, letterB) - ((System.Array.IndexOf(topLetters, letterB) % 5) - (System.Array.IndexOf(topLetters, letterA) % 5))];
                    }
                }
                else
                {
                    if (System.Array.IndexOf(rightLetters, letterA) % 5 > System.Array.IndexOf(rightLetters, letterB) % 5)
                    {
                        newLetterA = rightLetters[System.Array.IndexOf(rightLetters, letterA) - ((System.Array.IndexOf(rightLetters, letterA) % 5) - (System.Array.IndexOf(rightLetters, letterB) % 5))];
                        newLetterB = rightLetters[System.Array.IndexOf(rightLetters, letterB) + ((System.Array.IndexOf(rightLetters, letterA) % 5) - (System.Array.IndexOf(rightLetters, letterB) % 5))];
                    }
                    else
                    {
                        newLetterA = rightLetters[System.Array.IndexOf(rightLetters, letterA) + ((System.Array.IndexOf(rightLetters, letterB) % 5) - (System.Array.IndexOf(rightLetters, letterA) % 5))];
                        newLetterB = rightLetters[System.Array.IndexOf(rightLetters, letterB) - ((System.Array.IndexOf(rightLetters, letterB) % 5) - (System.Array.IndexOf(rightLetters, letterA) % 5))];
                    }
                }
            }

            char[] modifiedSerialCharacters1 = modifiedSerialNumber.ToCharArray();
            modifiedSerialCharacters1[2 * i] = newLetterA;
            modifiedSerialCharacters1[2 * i + 1] = newLetterB;
            modifiedSerialNumber = new string(modifiedSerialCharacters1);
        }

        //Debug.Log(modifiedSerialNumber);
        Debug.LogFormat("[Superposition #{0}] The new Cipher Code after the Playfair Cipher is {1}.", _moduleID, modifiedSerialNumber);

        //Caesar Cipher
        char[] modifiedSerialCharacters2 = modifiedSerialNumber.ToCharArray();
        if (Bomb.GetSerialNumberNumbers().Last() % 2 == 0)
        {
            Debug.LogFormat("[Superposition #{0}] The last digit of the bomb's serial number is even. Caesar shifting up by {1}...", _moduleID, serialSum);
        }
        else
        {
            Debug.LogFormat("[Superposition #{0}] The last digit of the bomb's serial number is odd. Caesar shifting down by {1}...", _moduleID, serialSum);
        }
        for (int i = 0; i < 6; i++)
        {
            if (Bomb.GetSerialNumberNumbers().Last() % 2 == 0)
            {
                modifiedSerialCharacters2[i] = alphabet[(alphabet.IndexOf(modifiedSerialNumber[i]) + serialSum) % 26];
            }
            else
            {
                modifiedSerialCharacters2[i] = alphabet[(alphabet.IndexOf(modifiedSerialNumber[i]) - serialSum + 52) % 26];
            }
        }
        modifiedSerialNumber = new string(modifiedSerialCharacters2);

        //Debug.Log(modifiedSerialNumber);
        Debug.LogFormat("[Superposition #{0}] The new Cipher Code after the Caesar Cipher is {1}.", _moduleID, modifiedSerialNumber);

        //Atbash Cipher
        string atbashAlphabet = "ZYXWVUTSRQPONMLKJIHGFEDCBA";
        for (int i = 0; i < 6; i++)
        {
            if ((super == 0 && !topAlphabet.Contains(atbashAlphabet[alphabet.IndexOf(modifiedSerialNumber[i])])) || (super == 1 && !rightAlphabet.Contains(atbashAlphabet[alphabet.IndexOf(modifiedSerialNumber[i])])))
            {
                modifiedSerialCharacters2[i] = modifiedSerialCharacters2[i];
            }
            else
            {
                modifiedSerialCharacters2[i] = atbashAlphabet[alphabet.IndexOf(modifiedSerialNumber[i])];
            }
        }
        modifiedSerialNumber = new string(modifiedSerialCharacters2);

        //Debug.Log(modifiedSerialNumber);
        Debug.LogFormat("[Superposition #{0}] The new Cipher Code after the Atbash Cipher is {1}.", _moduleID, modifiedSerialNumber);

        //Final CipherCode
        char[] temp = modifiedSerialNumber.ToCharArray();
        for (int i = 1; i < 6; i++)
        {
            while (temp.Take(i).Contains(temp[i]) || ((super == 0 && !topAlphabet.Contains(temp[i])) || (super == 1 && !rightAlphabet.Contains(temp[i]))))
            {
                temp[i] = alphabet[(alphabet.IndexOf(temp[i]) + 1) % 26];
            }
        }
        modifiedSerialNumber = temp.Join("");

        //Debug.Log(modifiedSerialNumber);
        Debug.LogFormat("[Superposition #{0}] The final Cipher Code is {1}.", _moduleID, modifiedSerialNumber);

        //Vaild Cells
        for (int i = 0; i < 6; i++)
        {
            // Debug.Log(modifiedSerialNumber[i]);

            if (super == 0)
            {
                //Debug.Log(Array.IndexOf(topLetters, modifiedSerialNumber[i]));
                validCells[Array.IndexOf(topLetters, modifiedSerialNumber[i])]++;
                validCellsStep1[Array.IndexOf(topLetters, modifiedSerialNumber[i])]++;
            }
            else
            {
                //Debug.Log(Array.IndexOf(rightLetters, modifiedSerialNumber[i]));
                validCells[Array.IndexOf(rightLetters, modifiedSerialNumber[i])]++;
                validCellsStep1[Array.IndexOf(rightLetters, modifiedSerialNumber[i])]++;
            }
        }

        //Debug.Log(validCells.Join());
        LogValidCells(validCellsStep1);

        NumberMath();
    }

    private void NumberMath()
    {
        int[] validCellsStep2 = new int[25];
        int startingNumber = 0;
        int cellNumber = 0;
        int firstRowSum = 0;
        int checkCellNumber = 0;
        bool[] visitedCells = new bool[25];

        Debug.LogFormat("[Superposition #{0}] Step #2: Number Math", _moduleID);
        Debug.LogFormat("[Superposition #{0}] Getting Starting Number...", _moduleID);

        //Starting Number
        if (super == 0)
        {
            firstRowSum = topNumbers[0] + topNumbers[1] + topNumbers[2] + topNumbers[3] + topNumbers[4];
            Debug.LogFormat("[Superposition #{0}] The numbers in the first row of the Superposition added up equals {1}. This is now the Starting Number.", _moduleID, firstRowSum);
            startingNumber = firstRowSum;
            Debug.LogFormat("[Superposition #{0}] The Starting Number minus the last digit of the bomb's serial number is {1}.", _moduleID, startingNumber - Bomb.GetSerialNumberNumbers().Last());
            startingNumber -= Bomb.GetSerialNumberNumbers().Last();
            Debug.LogFormat("[Superposition #{0}] The Starting Number multiplied by the number in the cell in the postion of the number of batteries is {1}.", _moduleID, startingNumber * topNumbers[Bomb.GetBatteryCount()]);
            startingNumber *= topNumbers[Bomb.GetBatteryCount()];
            Debug.LogFormat("[Superposition #{0}] The absolute value of the Starting Number is {1}.", _moduleID, Math.Abs(startingNumber));
            startingNumber = Math.Abs(startingNumber);
            Debug.LogFormat("[Superposition #{0}] The digital root of the Starting Number is {1}.", _moduleID, DigitalRoot(startingNumber));
            startingNumber = DigitalRoot(startingNumber);
            Debug.LogFormat("[Superposition #{0}] The Starting Number plus the number of solved modules is {1}.", _moduleID, startingNumber + Bomb.GetSolvedModuleIDs().Count());
            startingNumber += Bomb.GetSolvedModuleIDs().Count();
            startingNumber = startingNumber % 25;
            Debug.LogFormat("[Superposition #{0}] The final Starting Number modulo 25 is {1}.", _moduleID, startingNumber);
            cellNumber = startingNumber;

        }
        else
        {
            firstRowSum = rightNumbers[0] + rightNumbers[1] + rightNumbers[2] + rightNumbers[3] + rightNumbers[4];
            Debug.LogFormat("[Superposition #{0}] The numbers in the first row of the Superposition added up equals {1}. This is now the Starting Number.", _moduleID, firstRowSum);
            startingNumber = firstRowSum;
            Debug.LogFormat("[Superposition #{0}] The Starting Number minus the last digit of the bomb's serial number is {1}.", _moduleID, startingNumber - Bomb.GetSerialNumberNumbers().Last());
            startingNumber -= Bomb.GetSerialNumberNumbers().Last();
            Debug.LogFormat("[Superposition #{0}] The Starting Number multiplied by the number in the cell in the postion of the number of batteries is {1}.", _moduleID, startingNumber * rightNumbers[Bomb.GetBatteryCount()]);
            startingNumber *= rightNumbers[Bomb.GetBatteryCount()];
            Debug.LogFormat("[Superposition #{0}] The absolute value of the Starting Number is {1}.", _moduleID, Math.Abs(startingNumber));
            startingNumber = Math.Abs(startingNumber);
            Debug.LogFormat("[Superposition #{0}] The digital root of the Starting Number is {1}.", _moduleID, DigitalRoot(startingNumber));
            startingNumber = DigitalRoot(startingNumber);
            Debug.LogFormat("[Superposition #{0}] The Starting Number plus the number of solved modules is {1}.", _moduleID, startingNumber + Bomb.GetSolvedModuleIDs().Count());
            startingNumber += Bomb.GetSolvedModuleIDs().Count();
            startingNumber = startingNumber % 25;
            Debug.LogFormat("[Superposition #{0}] The final Starting Number modulo 25 is {1}.", _moduleID, startingNumber);
            cellNumber = startingNumber;
        }

        //Debug.Log(startingNumber);

        validCells[startingNumber]++;
        validCellsStep2[startingNumber]++;
        visitedCells[startingNumber] = true;

        //Iterations for valid cells
        if (super == 0)
        {
            Debug.LogFormat("[Superposition #{0}] The Current Cell Number plus the number in the Cell Number's Cell is {1}", _moduleID, (cellNumber + topNumbers[cellNumber]) % 25);

            cellNumber = (cellNumber + topNumbers[cellNumber]) % 25;

            //Debug.Log(cellNumber);

            for (int i = 0; i < 5; i++)
            {

                checkCellNumber = cellNumber;

                //Debug.Log(checkCellNumber);

                while (visitedCells[checkCellNumber] == true)
                {
                    checkCellNumber++;
                    checkCellNumber = checkCellNumber % 25;
                }

                if (i < 4)
                {
                    Debug.LogFormat("[Superposition #{0}] The Current Cell Number plus the number in the Cell Number's Cell is {1}", _moduleID, (cellNumber + topNumbers[checkCellNumber]) % 25);
                }
                cellNumber = (cellNumber + topNumbers[checkCellNumber]) % 25;
                //Debug.Log(cellNumber);
                validCells[checkCellNumber]++;
                validCellsStep2[checkCellNumber]++;
                visitedCells[checkCellNumber] = true;
            }
        }
        else
        {
            Debug.LogFormat("[Superposition #{0}] The Current Cell Number plus the number in the Cell Number's Cell is {1}", _moduleID, (cellNumber + rightNumbers[cellNumber]) % 25);

            cellNumber = (cellNumber + rightNumbers[cellNumber]) % 25;

            //Debug.Log(cellNumber);

            for (int i = 0; i < 5; i++)
            {

                checkCellNumber = cellNumber;

                //Debug.Log(checkCellNumber);

                while (visitedCells[checkCellNumber] == true)
                {
                    checkCellNumber++;
                    checkCellNumber = checkCellNumber % 25;
                }

                if (i < 4)
                {
                    Debug.LogFormat("[Superposition #{0}] The Current Cell Number plus the number in the Cell Number's Cell is {1}", _moduleID, (cellNumber + rightNumbers[checkCellNumber]) % 25);
                }
                cellNumber = (cellNumber + rightNumbers[checkCellNumber]) % 25;
                //Debug.Log(cellNumber);
                validCells[checkCellNumber]++;
                validCellsStep2[checkCellNumber]++;
                visitedCells[checkCellNumber] = true;
            }
        }

        //Debug.Log(validCells.Join());
        LogValidCells(validCellsStep2);

        ColorSequence();
    }

    private void ColorSequence()
    {

        Debug.LogFormat("[Superposition #{0}] Step #3: Color Sequence", _moduleID);

        int[] validCellsStep3 = new int[25];
        int[] markedCells = new int[6];
        int markedCellsAmount = 0;
        string colorLine = colorGrid[super == 0 ? topColors[24] : rightColors[24], super == 0 ? topColors[0] : rightColors[0]];

        Debug.LogFormat("[Superposition #{0}] The Color Sequence is {1}", _moduleID, colorLine);
        //Debug.Log(colorLine);

        for (int i = 0; i < 7 && markedCellsAmount < 6; i++)
        {
            for (int j = 0; j < 25 && markedCellsAmount < 6; j++)
            {
                if ((super == 0 ? topColors[j] : rightColors[j]) == Array.IndexOf(new char[] { 'R', 'O', 'Y', 'G', 'B', 'V', 'W' }, colorLine[i]))
                {
                    markedCells[markedCellsAmount] = j;
                    markedCellsAmount++;
                }
            }
        }

        //Debug.Log(markedCells.Join(", "));

        for (int i = 0; i < 6; i++)
        {
            validCells[markedCells[i]]++;
            validCellsStep3[markedCells[i]]++;
        }

        //Debug.Log(validCells.Join());
        LogValidCells(validCellsStep3);

        CellMatching();
    }

    private void CellMatching()
    {
        int[] validCellsStep4 = new int[25];
        int[] columnChecks = new int[5];
        Debug.LogFormat("[Superposition #{0}] Step #4: Cell Matching", _moduleID);


        //Column 1

        //Last Digit < Batteries
        if (Bomb.GetSerialNumberNumbers().Last() < Bomb.GetBatteryCount())
        {
            columnChecks[0]++;
            //Debug.Log("Last Digit < Batteries 1");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 1, Row 1 applies. (Last Digit Of Serial Number < Batteries)", _moduleID);
        }

        //No White A or Z
        bool validTest2 = true;
        for (int i = 0; i < 25; i++)
        {
            if (super == 0 && "AZ".Contains(topLetters[i]) && topColors[i] == 6)
            {
                validTest2 = false;
                break;
            }

            if (super == 1 && "AZ".Contains(rightLetters[i]) && rightColors[i] == 6)
            {
                validTest2 = false;
                break;
            }
        }

        if (validTest2)
        {
            columnChecks[0]++;
            //Debug.Log("No White A or Z 1");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 1, Row 2 applies. (No White A Or Z)", _moduleID);
        }

        //Even Battery Holders
        if (Bomb.GetBatteryHolderCount() % 2 == 0)
        {
            columnChecks[0]++;
            //Debug.Log("Even Battery Holders 1");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 1, Row 3 applies. (Even Number Of Battery Holders)", _moduleID);
        }

        //Green Cell Serial
        bool validTest5 = false;
        for (int i = 0; i < 25; i++)
        {
            if (super == 0 && topColors[i] == 3 && (Bomb.GetSerialNumber().Contains(topLetters[i]) || Bomb.GetSerialNumber().Contains(topNumbers[i].ToString())))
            {
                validTest5 = true;
                break;
            }

            if (super == 1 && rightColors[i] == 3 && (Bomb.GetSerialNumber().Contains(rightLetters[i]) || Bomb.GetSerialNumber().Contains(rightNumbers[i].ToString())))
            {
                validTest5 = true;
                break;
            }
        }

        if (validTest5)
        {
            columnChecks[0]++;
            //Debug.Log("Green Cell Serial 1");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 1, Row 4 applies. (Green Cell Has A Shared Character In Serial Number)", _moduleID);
        }

        //No Strikes
        if (Bomb.GetStrikes() == 0)
        {
            columnChecks[0]++;
            //Debug.Log("No Strikes 1");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 1, Row 5 applies. (No Strikes)", _moduleID);
        }


        //Column 2

        //Weekend
        if (startDate == 0 || startDate == 6)
        {
            columnChecks[1]++;
            //Debug.Log("Weekend 2");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 2, Row 1 applies. (Started On A Weekend)", _moduleID);
        }

        //Missing Edgework
        if (Bomb.GetBatteryCount() == 0 || Bomb.GetIndicators().Count() == 0 || Bomb.GetPortPlateCount() == 0)
        {
            columnChecks[1]++;
            //Debug.Log("Missing Edgework 2");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 2, Row 2 applies. (Missing Some Type Of Edgework)", _moduleID);
        }

        //6 of a Color
        int[] tempColors = new int[25];
        for (int i = 0; i < 25; i++)
        {
            tempColors[i] = super == 1 ? rightColors[i] : topColors[i];
        }

        for (int i = 0; i < colors.Length; i++)
        {
            int colorCount = 0;
            for (int j = 0; j < 25; j++)
            {
                if (i == tempColors[j])
                {
                    colorCount++;
                }
            }

            if (colorCount >= 6)
            {
                columnChecks[1]++;
                //Debug.Log("6 of a Color 2");
                Debug.LogFormat("[Superposition #{0}] Rule for Column 2, Row 3 applies. (6 Of A Color)", _moduleID);
                break;
            }
        }

        //Total Ports in Serial
        int totalPorts = Bomb.GetPortCount() % 10;
        if (Bomb.GetSerialNumberNumbers().Contains(totalPorts))
        {
            columnChecks[1]++;
            //Debug.Log("Total Ports in Serial 2");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 2, Row 4 applies. (Number Of Ports In The Serial Number)", _moduleID);
        }

        //0 24 Letter in Serial
        if (super == 0 && (Bomb.GetSerialNumber().Contains(topLetters[0]) || (Bomb.GetSerialNumber().Contains(topLetters[24]))))
        {
            columnChecks[1]++;
            //Debug.Log("0 24 Letter in Serial 2");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 2, Row 5 applies. (Cell 0 or 24 Has A Letter In The Serial Number)", _moduleID);
        }
        if (super == 1 && (Bomb.GetSerialNumber().Contains(rightLetters[0]) || (Bomb.GetSerialNumber().Contains(rightLetters[24]))))
        {
            columnChecks[1]++;
            //Debug.Log("0 24 Letter in Serial 2");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 2, Row 5 applies. (Cell 0 or 24 Has A Letter In The Serial Number)", _moduleID);
        }


        //Column 3

        //No Vowel First Row
        bool validTest1 = true;
        for (int i = 0; i < 5; i++)
        {
            if (super == 0 && "AEIOU".Contains(topLetters[i]))
            {
                validTest1 = false;
                break;
            }

            if (super == 1 && "AEIOU".Contains(rightLetters[i]))
            {
                validTest1 = false;
                break;
            }
        }

        if (validTest1)
        {
            columnChecks[2]++;
            //Debug.Log("No Vowel First Row 3");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 3, Row 1 applies. (No Vowel In The First Row)", _moduleID);
        }

        //Indicator Letter In Serial
        if (Bomb.GetIndicators().Any(x => Bomb.GetSerialNumber().Intersect(x).Any()))
        {
            columnChecks[2]++;
            //Debug.Log("Indicator Letter In Serial 3");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 3, Row 2 applies. (Indicator Letter In The Serial Number)", _moduleID);
        }

        //Always True
        columnChecks[2]++;
        Debug.LogFormat("[Superposition #{0}] Rule for Column 3, Row 3 applies. (This Module Is On A Bomb)", _moduleID);

        //Red 1
        bool validTest4 = false;
        for (int i = 0; i < 25; i++)
        {
            if (super == 0 && topColors[i] == 0 && topNumbers[i] == 1)
            {
                validTest4 = true;
                break;
            }

            if (super == 1 && rightColors[i] == 0 && rightNumbers[i] == 1)
            {
                validTest4 = true;
                break;
            }
        }

        if (validTest4)
        {
            columnChecks[2]++;
            //Debug.Log("Red 1 3");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 3, Row 4 applies. (Red Cell With Number 1)", _moduleID);
        }

        //Serial digits < Batteries + Lit Indicators
        if (Bomb.GetSerialNumberNumbers().Sum() < 2 * (Bomb.GetBatteryCount() + Bomb.GetOnIndicators().Count()))
        {
            columnChecks[2]++;
            //Debug.Log("Serial digits < Batteries + Lit Indicators 3");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 3, Row 5 applies. (Sum Of Serial Digits < Batteries + Lit Indicators)", _moduleID);
        }


        //Column 4

        //QU4N7M
        if (Bomb.GetSerialNumber().Contains('Q') || Bomb.GetSerialNumber().Contains('U') || Bomb.GetSerialNumber().Contains('4') || Bomb.GetSerialNumber().Contains('N') || Bomb.GetSerialNumber().Contains('7') || Bomb.GetSerialNumber().Contains('M'))
        {
            columnChecks[3]++;
            //Debug.Log("QU4N7M 4");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 4, Row 1 applies. (QU4N7M In Serial Number)", _moduleID);
        }

        //Right Display
        if (super == 1)
        {
            columnChecks[3]++;
            //Debug.Log("Right Display 4");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 4, Row 2 applies. (Right Display)", _moduleID);
        }

        //No Parallel Serial plate
        bool validTest6 = true;
        foreach (var plate in Bomb.GetPortPlates())
        {
            if (plate.Contains("Parallel") && plate.Contains("Serial"))
            {
                validTest6 = false;
            }
        }

        if (validTest6)
        {
            columnChecks[3]++;
            //Debug.Log("No Parallel Serial plate 4");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 4, Row 3 applies. (No Parallel Port Paired With Serial Port)", _moduleID);
        }

        //Z = 0
        bool validTest3 = false;
        for (int i = 0; i < 25; i++)
        {
            if (super == 0 && "Z".Contains(topLetters[i]) && topNumbers[i] == 0)
            {
                validTest3 = true;
                break;
            }

            if (super == 1 && "Z".Contains(rightLetters[i]) && rightNumbers[i] == 0)
            {
                validTest3 = true;
                break;
            }
        }

        if (validTest3)
        {
            columnChecks[3]++;
            //Debug.Log("Z = 0 4");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 4, Row 4 applies. (0 Is Paired With Z)", _moduleID);
        }

        //No D Batteries
        if (Bomb.GetBatteryCount(Battery.D) == 0)
        {
            columnChecks[3]++;
            //Debug.Log("No D Batteries 4");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 4, Row 5 applies. (No D Batteries)", _moduleID);
        }


        //Column 5

        //Warm Colors
        int WarmCount = 0;
        for (int i = 0; i < 25; i++)
        {
            if (super == 0 && (topColors[i] == 6 || topColors[i] < 3))
            {
                WarmCount++;
            }

            if (super == 1 && (rightColors[i] == 6 || rightColors[i] < 3))
            {
                WarmCount++;
            }
        }

        if (WarmCount >= 13)
        {
            columnChecks[4]++;
            //Debug.Log("Warm Colors 5");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 5, Row 1 applies. (Warm Colors > Cold Colors)", _moduleID);
        }

        //Lit BOB
        if (Bomb.IsIndicatorOn(Indicator.BOB))
        {
            columnChecks[4]++;
            //Debug.Log("Lit BOB 5");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 5, Row 2 applies. (Hi Bob!)", _moduleID);
        }

        //No 10 Numbers or 7 Colors
        bool done = false;
        int[] tempNumbers = new int[25];
        for (int i = 0; i < 25; i++)
        {
            tempNumbers[i] = super == 1 ? rightNumbers[i] : topNumbers[i];
        }

        for (int i = 0; i < 10; i++)
        {
            if (Array.IndexOf(tempNumbers, i) == -1)
            {
                columnChecks[4]++;
                //Debug.Log("No 10 Numbers or 7 Colors 5");
                Debug.LogFormat("[Superposition #{0}] Rule for Column 5, Row 3 applies. (Not Every Color, Not Every Number)", _moduleID);
                done = true;
                break;
            }
        }

        if (!done)
        {
            for (int i = 0; i < 7; i++)
            {
                if (Array.IndexOf(tempColors, i) == -1)
                {
                    columnChecks[4]++;
                    //Debug.Log("No 10 Numbers or 7 Colors 5");
                    Debug.LogFormat("[Superposition #{0}] Rule for Column 5, Row 3 applies. (Not Every Color, Not Every Number)", _moduleID);
                    break;
                }
            }
        }

        //No Unlit Indicators
        if (Bomb.GetOffIndicators().Count() == 0)
        {
            columnChecks[4]++;
            //Debug.Log("No Unlit Indicators 5");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 5, Row 4 applies. (No Unlit Inicators)", _moduleID);
        }

        //Missing Vowel
        if (super == 0 && (missingLetters[0] == 0 || missingLetters[0] == 4 || missingLetters[0] == 8 || missingLetters[0] == 14 || missingLetters[0] == 20))
        {
            columnChecks[4]++;
            //Debug.Log("Missing Vowel 5");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 5, Row 5 applies. (Missing Letter Is Vowel)", _moduleID);
        }
        if (super == 1 && (missingLetters[1] == 0 || missingLetters[1] == 4 || missingLetters[1] == 8 || missingLetters[1] == 14 || missingLetters[1] == 20))
        {
            columnChecks[4]++;
            //Debug.Log("Missing Vowel 5");
            Debug.LogFormat("[Superposition #{0}] Rule for Column 5, Row 5 applies. (Missing Letter Is Vowel)", _moduleID);
        }

        //Debug.Log(columnChecks.Join());

        int largest = 0;
        for (int i = 4; i > -1; i--)
        {
            if (columnChecks[i] >= columnChecks[0] && columnChecks[i] >= columnChecks[1] && columnChecks[i] >= columnChecks[2] && columnChecks[i] >= columnChecks[3] && columnChecks[i] >= columnChecks[4])
            {
                largest = i;
                break;
            }
        }
        //Debug.Log(largest);
        Debug.LogFormat("[Superposition #{0}] Column {1} has the most rules that applied.", _moduleID, largest + 1);

        List<int> tempValidCells = new List<int>();
        bool hasRun = false;
        string[] display = new string[25];

        for (int i = 0; i < 25; i++)
        {
            display[i] = (super == 0 ? topLetters : rightLetters)[i] + (super == 0 ? topNumbers : rightNumbers)[i].ToString() + colorSymbols[(super == 0 ? topColors : rightColors)[i]];
        }

        for (int i = largest; (i != largest || !hasRun) && tempValidCells.Count() < 6; i = (i + 1) % 5)
        {
            for (int j = 0; j < cellMatching[0].Length && tempValidCells.Count() < 6; j++)
            {
                for (int k = 0; k < 25 && tempValidCells.Count() < 6; k++)
                {
                    if ((cellMatching[i][j][0] == display[k][0] || cellMatching[i][j][0] == '-') &&
                        (cellMatching[i][j][1] == display[k][1] || cellMatching[i][j][1] == '-') &&
                        (cellMatching[i][j][2] == display[k][2] || cellMatching[i][j][2] == '-') &&
                        !tempValidCells.Contains(k))
                    {
                        tempValidCells.Add(k);
                        Debug.LogFormat("[Superposition #{0}] Cell Matching contains {1}.", _moduleID, cellMatching[i][j]);
                        break;
                    }
                }
            }
            hasRun = true;
        }

        //Debug.Log(display.Join(", "));

        if (tempValidCells.Count() < 6)
        {
            for (int i = 0; i < 6 && tempValidCells.Count() < 6; i++)
            {
                if (!tempValidCells.Contains(i))
                {
                    tempValidCells.Add(i);
                }
            }
        }

        //Debug.Log(tempValidCells.Join(", "));

        for (int i = 0; i < 6; i++)
        {
            validCells[tempValidCells[i]]++;
            validCellsStep4[tempValidCells[i]]++;
        }

        //Debug.Log(validCells.Join());
        LogValidCells(validCellsStep4);

        LogValidCells(validCells, true);
    }

    private IEnumerator CollapsedSuperpostion()
    {
        yield return null;

        if (blankDisplay != 0)
        {
            Debug.LogFormat("[Superposition #{0}] Rule #1 applies (Missing Cell).", _moduleID);

            if (blankDisplay < 26)
            {
                super = 1;
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the right display.", _moduleID);
            }
            else
            {
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the top display.", _moduleID);
            }

        }

        else if (Bomb.GetSolvableModuleIDs().Where((x) => x.Equals("RBsuperposition")).Count() >= 2)
        {
            super = 0;
            Debug.LogFormat("[Superposition #{0}] Rule #2 applies (Multiple Superpositions).", _moduleID);
            Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the top display.", _moduleID);
        }

        else if (topColors[12] == rightColors[12] && topNumbers[12] != rightNumbers[12])
        {
            Debug.LogFormat("[Superposition #{0}] Rule #3 applies (Same Color in Cell 12).", _moduleID);
            if (topNumbers[12] < rightNumbers[12])
            {
                super = 1;
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the right display.", _moduleID);
            }
            else
            {
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the top display.", _moduleID);
            }
        }

        else if (Bomb.GetPorts().Contains("DVI") && Bomb.GetPorts().Contains("PS2") && Bomb.GetPorts().Contains("RJ45"))
        {
            super = 1;
            Debug.LogFormat("[Superposition #{0}] Rule #4 applies (Ports).", _moduleID);
            Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the right display.", _moduleID);
        }

        else if ((topNumbers[0] + topNumbers[5] + topNumbers[10] + topNumbers[15] + topNumbers[20] > 20) ^ (rightNumbers[0] + rightNumbers[5] + rightNumbers[10] + rightNumbers[15] + rightNumbers[20] > 20))
        {
            Debug.LogFormat("[Superposition #{0}] Rule #5 applies (First Column 20).", _moduleID);

            if (rightNumbers[0] + rightNumbers[5] + rightNumbers[10] + rightNumbers[15] + rightNumbers[20] > 20)
            {
                super = 1;
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the right display.", _moduleID);
            }
            else
            {
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the top display.", _moduleID);
            }
        }

        else if (Bomb.GetOnIndicators().Count() > Bomb.GetOffIndicators().Count())
        {
            super = 0;
            Debug.LogFormat("[Superposition #{0}] Rule #6 applies (Indicators).", _moduleID);
            Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the top display.", _moduleID);
        }

        else if ((topColors[0] != topColors[4] && topColors[0] != topColors[20] && topColors[0] != topColors[24] && topColors[4] != topColors[20] && topColors[4] != topColors[24] && topColors[20] != topColors[24]) ^ (rightColors[0] != rightColors[4] && rightColors[0] != rightColors[20] && rightColors[0] != rightColors[24] && rightColors[4] != rightColors[20] && rightColors[4] != rightColors[24] && rightColors[20] != rightColors[24]))
        {
            Debug.LogFormat("[Superposition #{0}] Rule #7 applies (Different Colored Corners).", _moduleID);

            if (rightColors[0] != rightColors[4] && rightColors[0] != rightColors[20] && rightColors[0] != rightColors[24] && rightColors[4] != rightColors[20] && rightColors[4] != rightColors[24] && rightColors[20] != rightColors[24])
            {
                super = 1;
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the right display.", _moduleID);
            }
            else
            {
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the top display.", _moduleID);
            }
        }

        else if (Bomb.GetBatteryCount().Equals(3) && Bomb.GetBatteryHolderCount().Equals(2))
        {
            super = 1;
            Debug.LogFormat("[Superposition #{0}] Rule #8 applies (Batteries).", _moduleID);
            Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the right display.", _moduleID);
        }

        else if ((Bomb.GetSerialNumber().Contains(topLetters[24]) || Bomb.GetSerialNumber().Contains(topNumbers[24].ToString())) ^ (Bomb.GetSerialNumber().Contains(rightLetters[24]) || Bomb.GetSerialNumber().Contains(rightNumbers[24].ToString())))
        {
            Debug.LogFormat("[Superposition #{0}] Rule #9 applies (Cell 24 in Serial Number).", _moduleID);

            if ((Bomb.GetSerialNumber().Contains(topLetters[24]) || Bomb.GetSerialNumber().Contains(topNumbers[24].ToString())))
            {
                super = 1;
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the right display.", _moduleID);
            }
            else
            {
                Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the top display.", _moduleID);
            }
        }

        else if (Bomb.GetSerialNumberLetters().All(x => x != 'A' && x != 'E' && x != 'I' && x != 'O' && x != 'U'))
        {
            super = 1;
            Debug.LogFormat("[Superposition #{0}] Rule #10 applies (No vowel in Serial Number).", _moduleID);
            Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the right display.", _moduleID);
        }

        else
        {
            Debug.LogFormat("[Superposition #{0}] No rules Apply.", _moduleID);
            Debug.LogFormat("[Superposition #{0}] The Collasped Superposition is the top display.", _moduleID);
        }

        LetterCiphers();
    }

    private void ButtonPress(int buttonID)
    {
        if (buttonID != 25)
        {
            StartCoroutine(ButtonAnim(buttonID));
            Audio.PlaySoundAtTransform("Button Press", Buttons[buttonID].transform);

            if (!greyButtons[buttonID])
            {
                greyButtons[buttonID] = true;
                Buttons[buttonID].GetComponent<MeshRenderer>().material.color = new Color32(64, 64, 64, 255);
            }
            else
            {
                greyButtons[buttonID] = false;
                Buttons[buttonID].GetComponent<MeshRenderer>().material.color = new Color32(138, 1, 98, 255);
            }
        }

        else
        {
            Debug.LogFormat("[Superposition #{0}] The cells that have been submitted are: {1}.", _moduleID, Enumerable.Range(0, 25).Where(x => greyButtons[x]).Select(x => coordinates[x]).Join(", "));

            for (int i = 0; i < 25; i++)
            {
                if (greyButtons[i] != (validCells[i] == 1))
                {
                    Debug.LogFormat("[Superposition #{0}] Module Strike!", _moduleID);
                    Module.HandleStrike();
                    StartCoroutine(StrikeAnim(buttonID));
                    goto end;
                }
            }
            Debug.LogFormat("[Superposition #{0}] Module Solved.", _moduleID);
            Module.HandlePass();
            StartCoroutine(SolveAnim(buttonID));
            solved = true;
        end:;
        }
    }

    private IEnumerator ButtonAnim(int buttonID, float duration = 0.1f)
    {
        pressing[buttonID] = true;

        float timer = 0;
        float original = Buttons[buttonID].transform.localPosition.y;

        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[buttonID].transform.localPosition = new Vector3(Buttons[buttonID].transform.localPosition.x, Mathf.Lerp(original, original - 0.006f, timer * (1 / duration)), Buttons[buttonID].transform.localPosition.z);
        }

        Buttons[buttonID].transform.localPosition = new Vector3(Buttons[buttonID].transform.localPosition.x, original - 0.006f, Buttons[buttonID].transform.localPosition.z);
        timer = 0;

        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[buttonID].transform.localPosition = new Vector3(Buttons[buttonID].transform.localPosition.x, Mathf.Lerp(original - 0.006f, original, timer * (1 / duration)), Buttons[buttonID].transform.localPosition.z);
        }

        Buttons[buttonID].transform.localPosition = new Vector3(Buttons[buttonID].transform.localPosition.x, original, Buttons[buttonID].transform.localPosition.z);

        pressing[buttonID] = false;
    }

    private IEnumerator StrikeAnim(int buttonID, float duration = 1.5f)
    {
        Audio.PlaySoundAtTransform("Strike", Buttons[12].transform);

        canNotPress = true;
        float timer = 0;
        int[] marked = new int[] { 0, 4, 6, 8, 12, 16, 18, 20, 24 };
        Displays[0].color = colors[0];
        Displays[0].text = "Strike";
        Displays[1].color = colors[0];
        Displays[1].text = "Strike";

        for (int i = 0; i < 25; i++)
        {
            if (marked.Contains(i))
            {
                Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(1, 0, 0, 1);
            }
            else
            {
                Buttons[i].GetComponent<MeshRenderer>().material.color = new Color32(64, 64, 64, 255);
            }

        }

        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
        }

        greyButtons = new bool[25];

        for (int i = 0; i < 25; i++)
        {
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color32(138, 1, 98, 255);
        }
        canNotPress = false;

        Displays[0].color = colors[6];
        Displays[0].text = "Super";
        Displays[1].color = colors[6];
        Displays[1].text = "Position";
    }

    private IEnumerator SolveAnim(int buttonID, float interval = 0.1f)
    {
        Audio.PlaySoundAtTransform("Solve", Buttons[12].transform);

        Displays[0].color = colors[3];
        Displays[0].text = "Module";
        Displays[1].color = colors[3];
        Displays[1].text = "Solved";

        float timer = 0;
        int[][] pattern = new int[][]
        {
            new int[] {0},
            new int[] {1, 5},
            new int[] {2, 6, 10},
            new int[] {3, 7, 11, 15},
            new int[] {4, 8, 12, 16, 20},
            new int[] {9, 13, 17, 21},
            new int[] {14, 18, 22},
            new int[] {19, 23},
            new int[] {24}
        };

        for (int i = 0; i < pattern.Length; i++)
        {
            for (int j = 0; j < pattern[i].Length; j++)
            {
                Buttons[pattern[i][j]].GetComponent<MeshRenderer>().material.color = colors[3];
            }

            while (timer < interval)
            {
                yield return null;
                timer += Time.deltaTime;
            }
            timer = 0;
        }


    }

    private void LogValidCells(int[] valid, bool final = false)
    {
        List<string> temp = new List<string>();
        for (int i = 0; i < 25; i++)
        {
            if (valid[i] == 1)
            {
                temp.Add(coordinates[i]);
            }
        }
        if (final == false)
        {
            Debug.LogFormat("[Superposition #{0}] The valid cells for this step are: {1}.", _moduleID, temp.Join(", "));
        }
        else
        {
            Debug.LogFormat("[Superposition #{0}] The uniquely valid cells in all the stages are: {1}.", _moduleID, temp.Join(", "));
        }

    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} h a1 c3 e5' to hover over cells A1, C3 and E5. Use '!{0} cycle' to hover over every button in reading order. Use '!{0} a1 c3 e5 s' to press cells A1, C3, E5 and then the status light. Use '{0} colo(u)rblind' to toggle colorblind mode. Use '{0} d 0.5' to set the hover time to half a second. The maximum hover time is 5 seconds and is initially 3 seconds.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToUpperInvariant();
        string[] commandArray = command.Split(' ');
        yield return null;
        if (command == "COLORBLIND" || command == "COLOURBLIND")
        {
            colorblind = !colorblind;
        }
        else if (command == "CYCLE")
        {
            for (int i = 0; i < 25; i++)
            {
                Buttons[i].OnHighlight();
                float timer = 0;
                while (timer < tpDelay)
                {
                    yield return "trycancel Cycle cancelled.";
                    timer += Time.deltaTime;
                }
                Buttons[i].OnHighlightEnded();
                yield return "trycancel Cycle cancelled.";
            }
        }
        else if (commandArray[0] == "D")
        {
            try
            {
                float.Parse(commandArray[1]);
            }
            catch
            {
                goto somethingBroke;
            }
            if (float.Parse(commandArray[1]) < 0 || float.Parse(commandArray[1]) > 5)
            {
                yield return "sendtochaterror Invalid number. Please keep the delay between 0 and 5 seconds.";
                yield break;
            }
            else
            {
                tpDelay = float.Parse(commandArray[1]);
            }

        }
        else if (commandArray[0] == "H")
        {
            for (int i = 1; i < commandArray.Length; i++)
            {
                if (!coordinates.Contains(commandArray[i]))
                {
                    yield return "sendtochaterror Invalid coordinates.";
                    yield break;
                }
            }
            for (int i = 1; i < commandArray.Length; i++)
            {
                Buttons[Array.IndexOf(coordinates, commandArray[i])].OnHighlight();
                float timer = 0;
                while (timer < tpDelay)
                {
                    yield return "trycancel Input cancelled.";
                    timer += Time.deltaTime;
                }
                Buttons[Array.IndexOf(coordinates, commandArray[i])].OnHighlightEnded();
                yield return "trycancel Input cancelled.";
            }
        }
        else
        {
            for (int i = 0; i < commandArray.Length; i++)
            {
                if (!coordinates.Contains(commandArray[i]) && commandArray[i] != "S")
                {
                    yield return "sendtochaterror Invalid coordinates.";
                    yield break;
                }
            }
            for (int i = 0; i < commandArray.Length; i++)
            {
                if (commandArray[i] == "S")
                {
                    Buttons[25].OnInteract();
                }
                else
                {
                    Buttons[Array.IndexOf(coordinates,commandArray[i])].OnInteract();
                }
                float timer = 0;
                while (timer < 0.1f)
                {
                    yield return "trycancel Input cancelled.";
                    timer += Time.deltaTime;
                }
            }

        }
        yield break;
    somethingBroke:
        yield return "sendtochaterror Invalid command.";
        yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        retry:

        for (int i = 0; i < 25; i++)
        {
            if ((greyButtons[i] && validCells[i] != 1) || (!greyButtons[i] && validCells[i] == 1))
            {
                Buttons[i].OnInteract();
                float timer = 0;
                while (timer < 0.05f)
                {
                    yield return null;
                    timer += Time.deltaTime;
                }
            }
        }

        for (int i = 0; i < 25; i++)
        {
            if (greyButtons[i] != (validCells[i] == 1))
            {
                goto retry;
            }
        }

        yield return null;
        Buttons[25].OnInteract();
    }
}