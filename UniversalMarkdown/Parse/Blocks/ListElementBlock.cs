﻿// Copyright (c) 2016 Quinn Damerell
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.


using System;
using UniversalMarkdown.Helpers;

namespace UniversalMarkdown.Parse.Elements
{
    public class ListElementBlock : MarkdownBlock
    {
        public int ListIndent = 0;

        public string ListBullet = String.Empty;

        public ListElementBlock()
            : base(MarkdownBlockType.ListElement)
        { }

        /// <summary>
        /// Called when this block type should parse out the goods. Given the markdown, a starting point, and a max ending point
        /// the block should find the start of the block, find the end and parse out the middle. The end most of the time will not be
        /// the max ending pos, but it sometimes can be. The function will return where it ended parsing the block in the markdown.
        /// </summary>
        /// <param name="markdown"></param>
        /// <param name="startingPos"></param>
        /// <param name="maxEndingPos"></param>
        /// <returns></returns>
        internal override int Parse(string markdown, int startingPos, int maxEndingPos)
        {
            // Find out what the list is and where it begins.
            int listStart = startingPos;
            while (listStart < markdown.Length && listStart < maxEndingPos)
            {
                // We have a bullet list
                if (markdown[listStart] == '*' || markdown[listStart] == '-' || markdown[listStart] == '+')
                {
                    ListBullet = "•";
                    // +1 to move past the ' '
                    listStart++;
                    break;
                }
                // We have a numbered list, start grabbing the bullet
                else if (char.IsDigit(markdown[listStart]))
                {
                    // Grab the list letter, but keep going to get the rest.
                    ListBullet += markdown[listStart];
                }
                // We finished the letter list.
                else if(markdown[listStart] == '.')
                {
                    ListBullet += '.';
                    break;
                }
                listStart++;
            }

            // Now figure out how many spaces come before this list, we have to count backwards from the starting pos.
            ListIndent = 0;
            int currentBackCount = startingPos - 1;
            while (currentBackCount >= 0 && markdown[currentBackCount] != '\n' && markdown[currentBackCount] != '\r')
            {
                ListIndent++;
                currentBackCount--;
            }

            // A list should only single newline break if it is that start of another element in the list.
            // So we need to loop to check for them.
            // This is hard becasue of all of our list types. For * and - we just check if the next two chars
            // are * or and a ' ' if so we matched. For letters and digits, once we find one we keep looping until
            // we find a '.'. If we find a . we get a match, if anything else we fail.
            int nextDoubleBreak = Common.FindNextDoubleNewLine(markdown, listStart, maxEndingPos);
            int nextSingleBreak = Common.FindNextSingleNewLine(markdown, listStart, maxEndingPos);
            int potentialListStart = -1;
            int listEnd = nextDoubleBreak;
            while (nextSingleBreak < nextDoubleBreak && nextSingleBreak + 2 < maxEndingPos)
            {
                // Ignore spaces unless we are tracking a potential list start
                if (potentialListStart == -1 && markdown[nextSingleBreak + 1] == ' ')
                {
                    nextSingleBreak++;
                }
                // Check for a * or - or + followed by a space
                else if ((markdown[nextSingleBreak + 1] == '*' || markdown[nextSingleBreak + 1] == '-' || markdown[nextSingleBreak + 1] == '+') && markdown[nextSingleBreak + 2] == ' ')
                {
                    // This is our line break
                    listEnd = nextSingleBreak;
                    break;
                }
                // If this is a digit we might have a new list start. Note the position and loop.
                else if (char.IsDigit(markdown[nextSingleBreak + 1]))
                {
                    if (potentialListStart == -1)
                    {
                        potentialListStart = nextSingleBreak;
                    }
                    nextSingleBreak++;
                }
                // If we find a . and we have a potential list start then we matched.
                else if (potentialListStart != -1 && markdown[nextSingleBreak + 1] == '.')
                {
                    // This is our line break
                    listEnd = potentialListStart;
                    break;
                }
                else
                {
                    // We failed with this new line, try to get the next one.
                    nextSingleBreak = Common.FindNextSingleNewLine(markdown, nextSingleBreak + 1, maxEndingPos);
                    potentialListStart = -1;
                }
            }

            if (listEnd == -1)
            {
                DebuggingReporter.ReportCriticalError("Tried to parse list that didn't have an end");
                listEnd = maxEndingPos;
            }

            // Remove one indent from the list. This doesn't work exactly like reddit's
            // but it is close enough
            ListIndent = Math.Max(1, ListIndent - 1);

            // Jump past the *
            listStart++;

            // Make sure there is something to parse, and not just dead space
            if (listEnd > listStart)
            {
                // Parse the children of this list
                ParseInlineChildren(markdown, listStart, listEnd);
            }

            // Trim off any extra line endings, except ' ' otherwise we can't do code blocks
            while (listEnd < markdown.Length && listEnd < maxEndingPos && char.IsWhiteSpace(markdown[listEnd]) && markdown[listEnd] != ' ')
            {
                listEnd++;
            }

            // Return where we ended.
            return listEnd;
        }

        /// <summary>
        /// Called to determine if this block type can handle the next block.
        /// </summary>
        /// <param name="markdown"></param>
        /// <param name="nextCharPos"></param>
        /// <param name="endingPos"></param>
        /// <returns></returns>
        public static bool CanHandleBlock(string markdown, int nextCharPos, int endingPos)
        {
            // Check if we have reached the end of the input.
            if (nextCharPos + 1 >= markdown.Length || nextCharPos + 1 >= endingPos)
                return false;

            // Check for '*', '-' or '+', followed by a space.
            if ((markdown[nextCharPos] == '*' || markdown[nextCharPos] == '-' || markdown[nextCharPos] == '+') && markdown[nextCharPos + 1] == ' ')
            {
                return true;
            }

            // We need to also look for a numbered list ("1. test" or "100. test").
            // Loop past any digits.
            int pos = nextCharPos;
            while (pos < endingPos && char.IsDigit(markdown[pos]))
            {
                pos++;
            }

            // We found a numbered list if there was at least one digit and the next two characters are a dot and a space.
            return pos > nextCharPos && pos + 1 < endingPos && markdown[pos] == '.' && markdown[pos + 1] == ' ';
        }
    }
}
