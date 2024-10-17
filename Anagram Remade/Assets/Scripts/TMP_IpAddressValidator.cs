using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TMPro
{
    [CreateAssetMenu(fileName = "Input Field Validator", menuName = "Input Field Validator")]
    public class TMP_IpAddressValidator : TMP_InputValidator
    {
        public override char Validate(ref string text, ref int pos, char ch)
        {
            // Allow digits and periods (.) only
            if (char.IsDigit(ch) || ch == '.')
            {
                if (text.Length > 0)
                {
                    if (text[pos - 1] == '.' && ch == '.')
                    {
                        return (char)0;
                    }
                }
                else if (ch == '.')
                {
                    return (char)0;
                }
                // Check if the text with the new character is a valid IPv4 address
                string newText = text.Substring(0, pos) + ch + text.Substring(pos);
                string[] parts = newText.Split('.');
                if (parts.Length <= 4)
                {
                    if (parts.Length == 4 && ch == '.')
                    {
                        //return (char)0;
                    }
                    foreach (string part in parts)
                    {
                        if (!string.IsNullOrEmpty(part))
                        {
                            if (!IsValidIPv4Part(part))
                            {
                                // If any part is not valid, reject the input
                                return (char)0;
                            }
                        }
                    }
                }
                else
                {
                    return (char)0;
                }
            }
            else
            {
                // Reject any other characters
                return (char)0;
            }

            text = text.Insert(pos, ch.ToString());
            pos++;
            // Accept the character
            return ch;
        }

        // Helper function to validate each part of the IPv4 address
        private bool IsValidIPv4Part(string part)
        {
            int value;
            if (int.TryParse(part, out value))
            {
                return value >= 0 && value <= 255;
            }
            return false;
        }
    }
}