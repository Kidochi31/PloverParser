using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Plover.Debugging
{
    public record class ErrorMessage(int Line, int Column, string errorMessage) { }

    public record class ErrorUnderline(int StartLine, int StartColumn, int EndLine, int EndColumn, char Underline) { }

    public record class ErrorPointer(int Line, int Column, List<string> pointerMessage) { }

    public record class ErrorSettings(int SpaceBefore = 0, int SpaceAfter = 0, int LinesBefore = 0, int LinesAfter = 0)
    { }

    public abstract record class Suggestion(int Line, int Column) { }

    public record class ErrorInsertSuggestion(int Line, int Column, string InsertText) : Suggestion(Line, Column) { }

    public record class ErrorDeleteSuggestion(int Line, int Column, int Length) : Suggestion(Line, Column) { }

    public record class ErrorReplaceSuggestion(int Line, int Column, int Length, string ReplaceText) : Suggestion(Line, Column) { }

    public record class ErrorCombineLinesSuggestion(int FirstLine) : Suggestion(FirstLine, int.MaxValue) { }

    public record class ErrorSuggestion(List<Suggestion> Suggestions, string Message) { }


    internal class LineInfo(string line)
    {
        public string Line = line;
        public List<(int StartColumn, int EndColumn, char Underline)> Underlines = new();
        public List<(int Column, List<string> Message)> Pointers = new();
        public List<List<char?>> LinesBelow = new();
    }


    public static class Debug
    {
        public static string CreateErrorMessage(List<string> lines, List<ErrorMessage> errorMessages, List<string> messages, List<ErrorUnderline> underlines, List<ErrorPointer> pointers, ErrorSettings settings, List<ErrorSuggestion> suggestions)
        {
            return GetErrorMessages(errorMessages)
                + GetVisualErrorMessages(lines, underlines, pointers, settings)
                + GetMessages(messages)
                + GetSuggestions(lines, suggestions);
        }

        public static string GetSuggestions(List<string> lines, List<ErrorSuggestion> suggestions)
        {
            StringBuilder builder = new StringBuilder();
            foreach(ErrorSuggestion suggestion in suggestions)
            {
                builder.AppendLine(GetSuggestion(lines, suggestion));
                builder.AppendLine();
            }
            return builder.ToString();
        }

        public static string GetSuggestion(List<string> lines,  ErrorSuggestion suggestion)
        {
            StringBuilder finalString = new StringBuilder();
            finalString.AppendLine($"Suggestion: {suggestion.Message}");
            suggestion.Suggestions.Sort((s1, s2) => s1.Line > s2.Line ? 1 :
                                                        s2.Line > s1.Line ? -1 :
                                                        s1.Column > s2.Column ? 1 :
                                                        s2.Column > s1.Column ? -1 :
                                                        0);
            suggestion.Suggestions.Reverse();
            Dictionary<int, string> suggestedLines = new Dictionary<int, string>();
            foreach(Suggestion fix in suggestion.Suggestions)
            {
                if (fix is ErrorInsertSuggestion insertFix)
                {
                    int line = fix.Line;
                    if (!suggestedLines.ContainsKey(line)) suggestedLines[line] = lines[line - 1];
                    suggestedLines[line] = suggestedLines[line].Insert(insertFix.Column - 1, insertFix.InsertText);
                }
                else if (fix is ErrorDeleteSuggestion deleteFix)
                {
                    int line = fix.Line;
                    if (!suggestedLines.ContainsKey(line)) suggestedLines[line] = lines[line - 1];
                    Console.WriteLine(deleteFix.Length);
                    Console.WriteLine(deleteFix.Length + fix.Column - 1);
                    Console.WriteLine(suggestedLines[line].Length - 1);
                    Console.WriteLine(fix.Column - 1);
                    Console.WriteLine(Math.Min(deleteFix.Length + fix.Column - 1, suggestedLines[line].Length) - (fix.Column - 1));
                    suggestedLines[line] = suggestedLines[line].Remove(fix.Column - 1, Math.Min(deleteFix.Length + fix.Column - 1, suggestedLines[line].Length) - (fix.Column - 1));
                }
                else if (fix is ErrorReplaceSuggestion replaceFix)
                {
                    int line = fix.Line;
                    if (!suggestedLines.ContainsKey(line)) suggestedLines[line] = lines[line - 1];
                    suggestedLines[line] = suggestedLines[line].Remove(fix.Column - 1, Math.Min(replaceFix.Length + fix.Column - 1, suggestedLines[line].Length) - (fix.Column - 1));
                    suggestedLines[line] = suggestedLines[line].Insert(replaceFix.Column - 1, replaceFix.ReplaceText);
                }
                else if (fix is ErrorCombineLinesSuggestion combineFix)
                {
                    int line = fix.Line;
                    if (!suggestedLines.ContainsKey(line)) suggestedLines[line] = lines[line - 1];
                    if (!suggestedLines.ContainsKey(line+1)) suggestedLines[line+1] = lines[line];
                    suggestedLines[line] = suggestedLines[line] + suggestedLines[line + 1];
                    suggestedLines.Remove(line + 1);
                }
            }

            // add in the lines
            List<int> linesToPrint = suggestedLines.Keys.Order().ToList();
            int maxLineNumberLength = linesToPrint.Last().ToString().Length;
            string separator = "| ";

            int previousLine = linesToPrint[0];
            foreach (int line in linesToPrint)
            {
                if (line - previousLine > 1)
                {
                    // there has been a jump -> add a line which is just "..."
                    finalString.Append("...\n");
                }
                string suggestionsToPrint = suggestedLines[line];
                // add the original line with line number
                string lineNumberStr = line.ToString();
                lineNumberStr = lineNumberStr.PadLeft(maxLineNumberLength);
                finalString.Append(lineNumberStr);
                finalString.Append(separator);
                finalString.AppendLine(suggestionsToPrint);
                previousLine = line;
            }

            return finalString.ToString();

        }

        public static string GetErrorMessages(List<ErrorMessage> errorMessages)
        {
            StringBuilder finalString = new StringBuilder();

            //add error messages at top
            foreach (ErrorMessage message in errorMessages)
            {
                finalString.AppendLine($"[{message.Line}:{message.Column}] {message.errorMessage}");
            }
            return finalString.ToString();
        }

        public static string GetMessages(List<string> messages)
        {
            StringBuilder finalString = new StringBuilder();
            //add messages at bottom
            foreach (string message in messages)
            {
                finalString.AppendLine(message);
            }
            return finalString.ToString();
        }

        public static string GetVisualErrorMessages(List<string> lines, List<ErrorUnderline> underlines, List<ErrorPointer> pointers, ErrorSettings settings)
        {
            lines = [.. lines, ""];
            HashSet<int> importantLines = new HashSet<int>();
            foreach(ErrorUnderline underline in underlines)
            {
                int startLine = Math.Max(1, underline.StartLine - settings.LinesBefore);
                int endLine = Math.Min(lines.Count, underline.EndLine + settings.LinesAfter);
                importantLines.UnionWith(Enumerable.Range(startLine, endLine - startLine + 1));
            }
            foreach (ErrorPointer pointer in pointers)
            {
                int startLine = Math.Max(1, pointer.Line - settings.LinesBefore);
                int endLine = Math.Min(lines.Count, pointer.Line + settings.LinesAfter);
                importantLines.UnionWith(Enumerable.Range(startLine, endLine - startLine + 1));
            }

            // importantLines contains all of the lines which need to be printed
            Dictionary<int, LineInfo> lineInfos = new();
            foreach(int line in importantLines)
            {
                lineInfos[line] = new LineInfo(lines[line-1]);
            }

            // go through all of the error pointers and add it to the respective line info
            foreach(ErrorPointer pointer in pointers)
            {
                List<string> pointerMessage = [..pointer.pointerMessage];
                if (pointerMessage.Count == 0) pointerMessage.Add("^");
                else pointerMessage[0] = "^" + pointerMessage[0];

                lineInfos[pointer.Line].Pointers.Add((pointer.Column, pointerMessage));
            }
            // go through all of the underlines and add it to the respective line info
            foreach(ErrorUnderline underline in underlines)
            {
                // if the start is the same as the end, add an underline in between
                if (underline.StartLine == underline.EndLine)
                {
                    lineInfos[underline.StartLine].Underlines.Add((underline.StartColumn, underline.EndColumn, underline.Underline));
                    continue;
                }
                // otherwise, start goes from start column to the end
                lineInfos[underline.StartLine].Underlines.Add((underline.StartColumn, lineInfos[underline.StartLine].Line.Length, underline.Underline));
                // middle goes across the entire line
                for (int i = underline.StartLine + 1; i < underline.EndLine; i++)
                {
                    lineInfos[i].Underlines.Add((1, lineInfos[underline.StartLine].Line.Length, underline.Underline));
                }
                // the end goes from the start to the end column
                lineInfos[underline.EndLine].Underlines.Add((1, underline.EndColumn, underline.Underline));
            }

            // now all of the information has been added, must compute the lines below
            foreach((_, LineInfo lineInfo) in lineInfos)
            {
                // firstly, add underlines below
                foreach((int StartColumn, int EndColumn, char Underline) in lineInfo.Underlines)
                {
                    bool foundSpace = false;
                    //need to find a place to put the underline
                    foreach(List<char?> lineBelow in lineInfo.LinesBelow)
                    {
                        bool spaceInLine = true;
                        for(int i = StartColumn; i <= Math.Min(EndColumn, lineBelow.Count); i++)
                        {
                            if (lineBelow[i-1] is not null)
                            {
                                // lineBelow is not empty -> continue
                                
                                spaceInLine = false;
                                break;
                            }
                        }
                        if (spaceInLine)
                        {
                            // found a space on this line
                            // first, need to ensure list's Count is at least the end column
                            while(lineBelow.Count < EndColumn)
                            {
                                lineBelow.Add(null);
                            }
                            // secondly, insert the characters
                            for (int i = StartColumn; i <= EndColumn; i++)
                            {
                                lineBelow[i - 1] = Underline;
                            }
                            foundSpace = true;
                            break;
                        }
                    }

                    if (foundSpace)
                        break;
                    // no space
                    // add a new line below
                    List<char?> newLine = [..Enumerable.Repeat<char?>(null, StartColumn-1), ..Enumerable.Repeat<char?>(Underline, EndColumn - StartColumn + 1)];
                    lineInfo.LinesBelow.Add(newLine);
                }

                // secondly, add the pointers below
                // if there is space on all lines to put the message, put it there
                // otherwise, keep going down
                // Add a | to decend pointers down
                foreach ((int column, List<string> message) in lineInfo.Pointers)
                {
                    //precompute the width and height
                    int width = message.MaxBy(m => m.Length).Length;
                    int height = message.Count;

                    bool foundSpace = false;
                    //need to find a place to put the messages
                    for (int i = 0; i < lineInfo.LinesBelow.Count; i++)
                    {
                        bool spaceInLine = true;
                        for (int extra = 0; extra < height && i + extra < lineInfo.LinesBelow.Count; extra++)
                        {
                            for(int c = 0; c < width && c + column - 1 <= lineInfo.LinesBelow[i + extra].Count; c++)
                            {
                                if (lineInfo.LinesBelow[i+extra][c + column - 1] is not null)
                                {
                                    spaceInLine = false;
                                    break;
                                }
                            }
                            if (!spaceInLine) break;
                        }

                        if (spaceInLine)
                        {
                            // found a space on this line
                            // go through all lines
                            // ensure there is a list
                            // ensure the list is long enough
                            // and insert the characters
                            for (int extra = 0; extra < height; extra++)
                            {
                                // ensure there is a list
                                if (i + extra >= lineInfo.LinesBelow.Count)
                                {
                                    lineInfo.LinesBelow.Add(new List<char?>());
                                }
                                // ensure the list is long enough
                                while (lineInfo.LinesBelow[i + extra].Count < column + width)
                                {
                                    lineInfo.LinesBelow[i + extra].Add(null);
                                }
                                // insert the characters
                                for (int c = 0; c < width && c < message[extra].Length; c++)
                                {
                                    lineInfo.LinesBelow[i + extra][column - 1 + c] = message[extra][c];
                                }
                            }

                            // also place a | pointing upwards above the first column
                            for(int bar = 0; bar < i; bar++)
                            {
                                while (lineInfo.LinesBelow[bar].Count < column)
                                {
                                    lineInfo.LinesBelow[bar].Add(null);
                                }
                                if (lineInfo.LinesBelow[bar][column-1] is null)
                                {
                                    lineInfo.LinesBelow[bar][column-1] = '|';
                                }
                            }

                            foundSpace = true;
                            break;
                        }
                    }

                    if (foundSpace)
                        break;
                    // no space
                    // add a new line below
                    int newLineI = lineInfo.LinesBelow.Count;
                    foreach(string line in message)
                    {
                        List<char?> newLine = [.. Enumerable.Repeat<char?>(null, column - 1), ..line];
                        lineInfo.LinesBelow.Add(newLine);
                    }
                    // also place a | pointing upwards above the first column
                    for (int bar = 0; bar < newLineI; bar++)
                    {
                        while (lineInfo.LinesBelow[bar].Count < column)
                        {
                            lineInfo.LinesBelow[bar].Add(null);
                        }
                        if (lineInfo.LinesBelow[bar][column-1] is null)
                        {
                            lineInfo.LinesBelow[bar][column-1] = '|';
                        }
                    }
                }
            }

            // all of the lineInfos are computed completely -> turn into string
            StringBuilder finalString = new();

            


            // add in the lines
            List<int> linesToPrint = lineInfos.Keys.Order().ToList();
            if(linesToPrint.Count == 0)
            {
                return finalString.ToString();
            }
            int maxLineNumberLength = linesToPrint.Last().ToString().Length;
            string separator = "| ";
            string extraPrelude = "".PadLeft(maxLineNumberLength, ' ') + separator;

            for(int i = 0; i < settings.SpaceBefore; i++)
            {
                finalString.AppendLine();
            }

            int previousLine = linesToPrint[0];
            foreach(int line in linesToPrint)
            {
                if(line - previousLine > 1)
                {
                    // there has been a jump -> add a line which is just "..."
                    finalString.Append("...\n");
                }
                LineInfo info = lineInfos[line];
                // add the original line with line number
                string lineNumberStr = line.ToString();
                lineNumberStr = lineNumberStr.PadLeft(maxLineNumberLength);
                finalString.Append(lineNumberStr);
                finalString.Append(separator);
                finalString.AppendLine(info.Line);
                // add on all the extra lines, with the buffer
                foreach(List<char?> extraLine in info.LinesBelow)
                {
                    string extraLineStr = new string((from c in extraLine select (c is null ? ' ' : (char)c)).ToArray());
                    finalString.Append(extraPrelude);
                    finalString.AppendLine(extraLineStr);
                }
                previousLine = line;
            }

            for (int i = 0; i < settings.SpaceAfter; i++)
            {
                finalString.AppendLine();
            }

            

            return finalString.ToString();
        }
    }
    
}
