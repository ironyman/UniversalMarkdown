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


using System.Text;

namespace UniversalMarkdown.Parse.Elements
{
    public class TextRunInline : MarkdownInline
    {
        /// <summary>
        /// The text for this run.
        /// </summary>
        public string Text { get; set; }

        public TextRunInline() :
            base(MarkdownInlineType.TextRun)
        {   }

        /// <summary>
        /// Called when the object should parse it's goods out of the markdown. The markdown, start, and stop are given. 
        /// The start and stop are what is returned from the FindNext function below. The object should do it's parsing and 
        /// return up to the last pos it used. This can be shorter than what is given to the function in endingPos.
        /// </summary>
        /// <param name="markdown">The markdown</param>
        /// <param name="startingPos">Where the parse should start</param>
        /// <param name="endingPos">Where the parse should end</param>
        /// <returns></returns>
        internal override int Parse(ref string markdown, int startingPos, int maxEndingPos)
        {
            // We need to go though all of the text and remove any newlines and returns if they aren't needed
            // the most efficient way to do this is to use a string builder to build the new string as normal.
            // We need to guess at the capacity of the string builder string, the entire range should be good.
            StringBuilder strBuilder = new StringBuilder(maxEndingPos - startingPos);

            // We need to keep track of continuous spaces, if there are more than 2 in a row we shouldn't remove the
            // new line.
            int continuousSpaceCount = 0;

            // Loop through from start to end.
            for (int currentMarkdownPos = startingPos; currentMarkdownPos < maxEndingPos; currentMarkdownPos++)
            {
                char currentChar = markdown[currentMarkdownPos];
                if (currentChar == '\n' || currentChar == '\r')
                {
                    // If we have two spaces before add it as normal.
                    if (continuousSpaceCount >= 2)
                    {
                        strBuilder.Append(currentChar);
                    }
                    else
                    {
                        // Check if we have a space before this one. If so don't bother inserting another.
                        if (currentMarkdownPos == 0 || !char.IsWhiteSpace(markdown[currentMarkdownPos - 1]))
                        {
                            strBuilder.Append(' ');
                        }
                    }
                }
                // If we have a space keep track of it.
                else if (currentChar == ' ')
                {
                    // Collapse multiple spaces.  TODO: collapse all types of whitespace, like how HTML does it.
                    // collapses all white space.
                    if (continuousSpaceCount == 0)
                        strBuilder.Append(currentChar);
                    continuousSpaceCount++;
                }
                // Also remove any non breaking spaces (&nbsp;)
                else if (currentChar == '&' && currentMarkdownPos + 5 < maxEndingPos && 
                        markdown[currentMarkdownPos + 1] == 'n' &&
                        markdown[currentMarkdownPos + 2] == 'b' &&
                        markdown[currentMarkdownPos + 3] == 's' &&
                        markdown[currentMarkdownPos + 4] == 'p' &&
                        markdown[currentMarkdownPos + 5] == ';')
                {
                    // Add a space. 
                    strBuilder.Append(' ');

                    // Jump the count ahead, don't forget the for loop will +1 by itself.
                    currentMarkdownPos += 5;
                }
                // Handle escape characters.
                else if (currentChar == '\\' && currentMarkdownPos + 1 < maxEndingPos && (
                    markdown[currentMarkdownPos + 1] == '*' ||
                    markdown[currentMarkdownPos + 1] == '_' ||
                    markdown[currentMarkdownPos + 1] == '^' ||
                    markdown[currentMarkdownPos + 1] == '~' ||
                    markdown[currentMarkdownPos + 1] == '`'))
                {
                    // Remove the backslash.
                }
                // If we have anything else add it and reset the space count.
                else
                {
                    strBuilder.Append(currentChar);
                    continuousSpaceCount = 0;
                }
            }

            // Finally set the text
            Text = strBuilder.ToString();

            // Return that we ate it all.
            return maxEndingPos;
        }
    }
}
