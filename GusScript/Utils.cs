/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

//Extracted from the Visual Fox Pro ToolKit
//Author: Kamal Patel (kppatel@yahoo.com)
//Copyright: None (Public Domain)
//Date: April 23rd, 2002
//Version 1.00
//Source dode found on: http://foxcentral.net/microsoft/vfptoolkitnet.htm

namespace GusNet.GusScripting
{
    public class Utils
    {
   
        /// <summary>
        /// Searches one string into another string and replaces all occurences with
        /// a blank character.
        /// <pre>
        /// Example:
        /// VFPToolkit.strings.StrTran("Joe Doe", "o");		//returns "J e D e" :)
        /// </pre>
        /// </summary>
        /// <param name="cSearchIn"> </param>
        /// <param name="cSearchFor"> </param>
        public static string StrTran(string cSearchIn, string cSearchFor)
        {
            //Create the StringBuilder
            StringBuilder sb = new StringBuilder(cSearchIn);

            //Call the Replace() method of the StringBuilder
            return sb.Replace(cSearchFor, " ").ToString();
        }

        /// <summary>
        /// Searches one string into another string and replaces all occurences with
        /// a third string.
        /// <pre>
        /// Example:
        /// VFPToolkit.strings.StrTran("Joe Doe", "o", "ak");		//returns "Jake Dake" 
        /// </pre>
        /// </summary>
        /// <param name="cSearchIn"> </param>
        /// <param name="cSearchFor"> </param>
        /// <param name="cReplaceWith"> </param>
        public static string StrTran(string cSearchIn, string cSearchFor, string cReplaceWith)
        {
            //Create the StringBuilder
            StringBuilder sb = new StringBuilder(cSearchIn);

            //There is a bug in the replace method of the StringBuilder
            sb.Replace(cSearchFor, cReplaceWith);

            //Call the Replace() method of the StringBuilder and specify the string to replace with
            return sb.Replace(cSearchFor, cReplaceWith).ToString();
        }

        /// Searches one string into another string and replaces each occurences with
        /// a third string. The fourth parameter specifies the starting occurence and the 
        /// number of times it should be replaced
        /// <pre>
        /// Example:
        /// VFPToolkit.strings.StrTran("Joe Doe", "o", "ak", 2, 1);		//returns "Joe Dake" 
        /// </pre>
        public static string StrTran(string cSearchIn, string cSearchFor, string cReplaceWith, int nStartoccurence, int nCount)
        {
            //Create the StringBuilder
            StringBuilder sb = new StringBuilder(cSearchIn);

            //There is a bug in the replace method of the StringBuilder
            sb.Replace(cSearchFor, cReplaceWith);

            //Call the Replace() method of the StringBuilder specifying the replace with string, occurence and count
            return sb.Replace(cSearchFor, cReplaceWith, nStartoccurence, nCount).ToString();
        }

        /// <summary>
        /// Receives a string along with starting and ending delimiters and returns the 
        /// part of the string between the delimiters. Receives a beginning occurence
        /// to begin the extraction from and also receives a flag (0/1) where 1 indicates
        /// that the search should be case insensitive.
        /// <pre>
        /// Example:
        /// string cExpression = "JoeDoeJoeDoe";
        /// VFPToolkit.strings.StrExtract(cExpression, "o", "eJ", 1, 0);		//returns "eDo"
        /// </pre>
        /// </summary>
        public static string StrExtract(string cSearchExpression, string cBeginDelim, string cEndDelim, int nBeginOccurence, int nFlags)
        {

            string cstring = cSearchExpression;
            string cb = cBeginDelim;
            string ce = cEndDelim;
            //string lcRetVal = "";

            if (nFlags == 1)
            {
                cb = cb.ToLower();
                ce = ce.ToLower();
                cstring = cstring.ToLower();
            }

            int lnAt = cSearchExpression.IndexOf(cb, 0);
            if (lnAt < 0)
                return "";

            int lnAtCut = lnAt + cb.Length;

            int lnAt2 = cSearchExpression.IndexOf(ce, lnAtCut);
            if (lnAt2 < 0)
                return "";

            return cSearchExpression.Substring(lnAtCut, lnAt2 - lnAtCut);
        }

        /// <summary>
        /// Receives a string and a delimiter as parameters and returns a string starting 
        /// from the position after the delimiter
        /// <pre>
        /// Example:
        /// string cExpression = "JoeDoeJoeDoe";
        /// VFPToolkit.strings.StrExtract(cExpression, "o");		//returns "eDoeJoeDoe"
        /// </pre>
        /// </summary>
        /// <param name="cSearchExpression"> </param>
        /// <param name="cBeginDelim"> </param>
        public static string StrExtract(string cSearchExpression, string cBeginDelim)
        {
            int nbpos = At(cBeginDelim, cSearchExpression);
            return cSearchExpression.Substring(nbpos + cBeginDelim.Length - 1);
        }

        /// <summary>
        /// Receives a string along with starting and ending delimiters and returns the 
        /// part of the string between the delimiters
        /// <pre>
        /// Example:
        /// string cExpression = "JoeDoeJoeDoe";
        /// VFPToolkit.strings.StrExtract(cExpression, "o", "eJ");		//returns "eDo"
        /// </pre>
        /// </summary>
        /// <param name="cSearchExpression"> </param>
        /// <param name="cBeginDelim"> </param>
        /// <param name="cEndDelim"> </param>
        public static string StrExtract(string cSearchExpression, string cBeginDelim, string cEndDelim)
        {
            return StrExtract(cSearchExpression, cBeginDelim, cEndDelim, 1, 0);
        }

        /// <summary>
        /// Receives a string along with starting and ending delimiters and returns the 
        /// part of the string between the delimiters. It also receives a beginning occurence
        /// to begin the extraction from.
        /// <pre>
        /// Example:
        /// string cExpression = "JoeDoeJoeDoe";
        /// VFPToolkit.strings.StrExtract(cExpression, "o", "eJ", 2);		//returns ""
        /// </pre>
        /// </summary>
        /// <param name="cSearchExpression"> </param>
        /// <param name="cBeginDelim"> </param>
        /// <param name="cEndDelim"> </param>
        /// <param name="nBeginOccurence"> </param>
        public static string StrExtract(string cSearchExpression, string cBeginDelim, string cEndDelim, int nBeginOccurence)
        {
            return StrExtract(cSearchExpression, cBeginDelim, cEndDelim, nBeginOccurence, 0);
        }


        /// Private Implementation: This is the actual implementation of the At() and RAt() functions. 
        /// Receives two strings, the expression in which search is performed and the expression to search for. 
        /// Also receives an occurence position and the mode (1 or 0) that specifies whether it is a search
        /// from Left to Right (for At() function)  or from Right to Left (for RAt() function)
        private static int __at(string cSearchFor, string cSearchIn, int nOccurence, int nMode)
        {
            //In this case we actually have to locate the occurence
            int i = 0;
            int nOccured = 0;
            int nPos = 0;
            if (nMode == 1) { nPos = 0; }
            else { nPos = cSearchIn.Length; }

            //Loop through the string and get the position of the requiref occurence
            for (i = 1; i <= nOccurence; i++)
            {
                if (nMode == 1) { nPos = cSearchIn.IndexOf(cSearchFor, nPos); }
                else { nPos = cSearchIn.LastIndexOf(cSearchFor, nPos); }

                if (nPos < 0)
                {
                    //This means that we did not find the item
                    break;
                }
                else
                {
                    //Increment the occured counter based on the current mode we are in
                    nOccured++;

                    //Check if this is the occurence we are looking for
                    if (nOccured == nOccurence)
                    {
                        return nPos + 1;
                    }
                    else
                    {
                        if (nMode == 1) { nPos++; }
                        else { nPos--; }

                    }
                }
            }
            //We never found our guy if we reached here
            return 0;
        }
        /// <summary>
        /// Receives two strings as parameters and searches for one string within another. 
        /// If found, returns the beginning numeric position otherwise returns 0
        /// <pre>
        /// Example:
        /// VFPToolkit.strings.At("D", "Joe Doe");	//returns 5
        /// </pre>
        /// </summary>
        /// <param name="cSearchFor"> </param>
        /// <param name="cSearchIn"> </param>
        public static int At(string cSearchFor, string cSearchIn)
        {
            return cSearchIn.IndexOf(cSearchFor) + 1;
        }

        /// <summary>
        /// Receives two strings and an occurence position (1st, 2nd etc) as parameters and 
        /// searches for one string within another for that position. 
        /// If found, returns the beginning numeric position otherwise returns 0
        /// <pre>
        /// Example:
        /// VFPToolkit.strings.At("o", "Joe Doe", 1);	//returns 2
        /// VFPToolkit.strings.At("o", "Joe Doe", 2);	//returns 6
        /// </pre>
        /// </summary>
        /// <param name="cSearchFor"> </param>
        /// <param name="cSearchIn"> </param>
        /// <param name="nOccurence"> </param>
        public static int At(string cSearchFor, string cSearchIn, int nOccurence)
        {
            return __at(cSearchFor, cSearchIn, nOccurence, 1);
        }


        /// <summary>
        /// Receives two strings as parameters and searches for one string within another. 
        /// This function ignores the case and if found, returns the beginning numeric position 
        /// otherwise returns 0
        /// <pre>
        /// Example:
        /// VFPToolkit.strings.AtC("d", "Joe Doe");	//returns 5
        /// </pre>
        /// </summary>
        /// <param name="cSearchFor"> </param>
        /// <param name="cSearchIn"> </param>
        public static int AtC(string cSearchFor, string cSearchIn)
        {
            return cSearchIn.ToLower().IndexOf(cSearchFor.ToLower()) + 1;
        }

        /// <summary>
        /// Receives two strings and an occurence position (1st, 2nd etc) as parameters and 
        /// searches for one string within another for that position. This function ignores the
        /// case of both the strings and if found, returns the beginning numeric position 
        /// otherwise returns 0.
        /// <pre>
        /// Example:
        /// VFPToolkit.strings.AtC("d", "Joe Doe", 1);	//returns 5
        /// VFPToolkit.strings.AtC("O", "Joe Doe", 2);	//returns 6
        /// </pre>
        /// </summary>
        /// <param name="cSearchFor"> </param>
        /// <param name="cSearchIn"> </param>
        /// <param name="nOccurence"> </param>
        public static int AtC(string cSearchFor, string cSearchIn, int nOccurence)
        {
            return __at(cSearchFor.ToLower(), cSearchIn.ToLower(), nOccurence, 1);
        }

    }
}
