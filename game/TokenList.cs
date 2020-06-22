#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamebook
{
   public class TokenList
   {
      // Creates a list of tokens from source text and allows you to access them by index.

      private List<Token> TheList;

      public Token At(
         int index)
      {
         return TheList[index];
      }

      public TokenList(
         string sourceText,
         string sourceNameForErrorMessages,
         Dictionary<string, Setting> settings)
      {
         // Tokens only get created as part of this list of tokens.
         char pushedLetter = '\0';
         char gottenLetter;
         int letterIndex;
         var specialIds = new List<string> { "John", "Jane", "Smith", "he", "He", "him", "Him", "his", "His", "himself", "Himself", "man", "Man", "boy", "Boy", "Mr", "Mrs", "she", "She", "her", "Her", "hers", "Hers", "herself", "Herself", "woman", "Woman", "girl", "Girl", "Ms" };

         TheList = new List<Token>();
         string textAccumulator = "";
         int lineNumber = 1;

         letterIndex = 0;

         // This outer loop is for accumulating text strings
         while (true)
         {
            if (!GetLetter())
            {
               if (textAccumulator.Length != 0)
               {
                  TheList.Add(new Token(TokenType.Characters, textAccumulator, lineNumber));
                  textAccumulator = "";
               }
               TheList.Add(new Token(TokenType.EndOfSourceText, "", lineNumber));
               return;
            }

            switch (gottenLetter)
            {
               case '\n':
                  // @ lets you explicitly put in a paragraph break. We'll clean up any extra spaces later. This lets you break contiguous text up into multiple lines within 'if' groups without having it affect formatting.
                  textAccumulator += "\n";
                  ++lineNumber;
                  break;

               case '\r':
                  // Ignore the CR in CRLF.
                  break;

               case '\t':
                  textAccumulator += "\t";
                  break;

               // Didn't want to put this way at the bottom--gets lost.
               default:
                  textAccumulator += gottenLetter;
                  break;

               case '[':
                  if (!GetLetter())
                  {
                     TheList.Add(new Token(TokenType.EndOfSourceText, "", lineNumber));
                     return;
                  }

                  // Check for a [[ comment starter.
                  if (gottenLetter != '[')
                     // No comment--pretend this never happened.
                     UngetLetter();
                  else
                  {
                     //  [[ This is an example comment. ]]
                     if (!GetComment())
                     {
                        // If it returns false, it means you've reached the end.
                        TheList.Add(new Token(TokenType.EndOfSourceText, "", lineNumber));
                        return;
                     }
                     // We got a text mode comment, so we break back out to the outer loop.
                     break; // switch
                  }

                  // If we have accumulated a text string, record it as the next token before going into code mode.
                  if (textAccumulator.Length != 0)
                  {
                     TheList.Add(new Token(TokenType.Characters, textAccumulator, lineNumber));
                     textAccumulator = "";
                  }

                  // This inner loop is for breaking out control codes
                  for (bool gotClosingBracket = false; !gotClosingBracket;)
                  {
                     if (!GetLetter())
                     {
                        TheList.Add(new Token(TokenType.EndOfSourceText, "", lineNumber));
                        return;
                     }

                     switch (gottenLetter)
                     {
                        case ' ':
                        case '\r':
                           break;

                        case '\n':
                           ++lineNumber;
                           break;

                        case ']':
                           // Leave code mode 
                           gotClosingBracket = true;
                           break;

                        case '=':
                           TheList.Add(new Token(TokenType.Equal, "=", lineNumber));
                           break;

                        case '.':
                           TheList.Add(new Token(TokenType.Period, ".", lineNumber));
                           break;

                        case ',':
                           TheList.Add(new Token(TokenType.Comma, ",", lineNumber));
                           break;

                        case '[':
                           // Must be a comment.
                           if (!GetLetter())
                           {
                              TheList.Add(new Token(TokenType.EndOfSourceText, "", lineNumber));
                              return;
                           }
                           if (gottenLetter != '[')
                              throw new InvalidOperationException(string.Format($"file {sourceNameForErrorMessages} line {lineNumber}: expected '[[' but got '[{gottenLetter}' in\n{sourceText}"));

                           if (!GetComment())
                           {
                              // If it returns false, it means you've reached the end.
                              TheList.Add(new Token(TokenType.EndOfSourceText, "", lineNumber));
                              return;
                           }
                           // Done skipping comment. Move on to the next token.
                           break;

                        default:
                           if (!Char.IsLetterOrDigit(gottenLetter) || gottenLetter == '_')
                              throw new InvalidOperationException(string.Format($"file {sourceNameForErrorMessages} line {lineNumber}: unexpected character '{gottenLetter}' in\n{sourceText}"));

                           string id = "";
                           do
                           {
                              id += gottenLetter;

                              if (!GetLetter())
                                 break;
                           } while (Char.IsLetterOrDigit(gottenLetter) || gottenLetter == '_' || gottenLetter == '.');

                           UngetLetter();
                           if (id == "if")
                              TheList.Add(new Token(TokenType.If, id, lineNumber));
                           else if (id == "else")
                              TheList.Add(new Token(TokenType.Else, id, lineNumber));
                           else if (id == "or")
                              TheList.Add(new Token(TokenType.Or, id, lineNumber));
                           else if (id == "not")
                              TheList.Add(new Token(TokenType.Not, id, lineNumber));
                           else if (id == "end")
                              TheList.Add(new Token(TokenType.End, id, lineNumber));
                           else if (id == "when")
                              TheList.Add(new Token(TokenType.When, id, lineNumber));
                           else if (id == "set")
                              TheList.Add(new Token(TokenType.Set, id, lineNumber));
                           else if (id == "score")
                              TheList.Add(new Token(TokenType.Score, id, lineNumber));
                           else if (id == "sort")
                              TheList.Add(new Token(TokenType.Sort, id, lineNumber));
                           else if (id == "text")
                              TheList.Add(new Token(TokenType.Text, id, lineNumber));
                           else if (id == "merge")
                              TheList.Add(new Token(TokenType.Merge, id, lineNumber));
                           else if (id == "return")
                              TheList.Add(new Token(TokenType.Return, id, lineNumber));
                           else if (id == "scene")
                              TheList.Add(new Token(TokenType.Scene, id, lineNumber));
                           else if (specialIds.Contains(id))
                              TheList.Add(new Token(TokenType.SpecialId, id, lineNumber));
                           else
                           {
                              if (settings.ContainsKey(id))
                                 switch (settings[id])
                                 {
                                    case ScoreSetting scoreSetting:
                                       TheList.Add(new Token(TokenType.ScoreId, id, lineNumber));
                                       break;
                                    case StringSetting stringSetting:
                                       TheList.Add(new Token(TokenType.StringId, id, lineNumber));
                                       break;
                                    case BooleanSetting booleanSetting:
                                       TheList.Add(new Token(TokenType.BooleanId, id, lineNumber));
                                       break;
                                 }
                              else
                                 TheList.Add(new Token(TokenType.Id, id, lineNumber));
                           }
                           break;
                     }
                  }
                  break;
            }
         }

         // Some local helper functions

         bool GetLetter()
         {
            if (pushedLetter != '\0')
            {
               gottenLetter = pushedLetter;
               pushedLetter = '\0';
               return true;
            }

            if (letterIndex >= sourceText.Length)
            {
               gottenLetter = '\0';
               return false;
            }

            gottenLetter = sourceText[letterIndex];
            ++letterIndex;
            return true;
         }

         void UngetLetter()
         {
            pushedLetter = gottenLetter;
         }

         bool GetComment()
         {
            // Nesting example:
            //   [[ They always were.[p] ]]
            // No-nesting example:
            //   [[ They always were. ]]
            int level = 0;
            while (true)
            {
               if (!GetLetter())
                  return false;
               if (gottenLetter == '[')
                  ++level;
               else if (gottenLetter == ']')
               {
                  --level;
                  if (level < 0)
                  {
                     if (!GetLetter())
                        return false;
                     if (gottenLetter == ']')
                        return true;
                     UngetLetter();
                  }
               }
            }
         }

      }

   }
}
