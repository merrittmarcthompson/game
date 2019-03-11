using System;
using System.Collections.Generic;

namespace Game
{
   public static partial class Transform
   {
      public static List<Token> SourceTextToTokens(
        string sourceText)
      {
         char pushedLetter = '\0';
         char gottenLetter;
         int letterIndex;
         var specialIds = new List<string> { "John", "Jane", "Smith", "he", "He", "him", "Him", "his", "His", "himself", "Himself", "man", "Man", "boy", "Boy", "Mr", "Mrs", "she", "She", "her", "Her", "hers", "Hers", "herself", "Herself", "woman", "Woman", "girl", "Girl", "Ms" };

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
               {
                  ++level;
               }
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

         // Start here

         List<Token> result = new List<Token>();
         string textAccumulator = "";
         int lineNumber = 1;

         letterIndex = 0;

         // This outer loop is for accumulating text strings
         while (true)
         {
            if (!GetLetter())
            {
               if (textAccumulator != "")
               {
                  result.Add(new Token(Token.Text, textAccumulator, lineNumber));
                  textAccumulator = "";
               }
               result.Add(new Token(Token.EndOfSourceText, "", lineNumber));
               return result;
            }

            switch (gottenLetter)
            {
               case '\n':
                  // @ lets you explicitly put in a paragraph break. We'll clean up any extra spaces later. This lets you break contiguous text up into multiple lines within 'if' groups without having it affect formatting.
                  textAccumulator += " ";
                  ++lineNumber;
                  break;

               case '\r':
                  // Ignore the CR in CRLF.
                  break;

               case '\t':
                  textAccumulator += " ";
                  break;

               // Didn't want to put this way at the bottom--gets lost.
               default:
                  textAccumulator += gottenLetter;
                  break;

               case '[':
                  if (!GetLetter())
                  {
                     result.Add(new Token(Token.EndOfSourceText, "", lineNumber));
                     return result;
                  }

                  // Check for a [[ comment starter.
                  if (gottenLetter != '[')
                  {
                     // No comment--pretend this never happened.
                     UngetLetter();
                  }
                  else
                  {
                     //  [[ This is an example comment. ]]
                     if (!GetComment())
                     {
                        // If it returns false, it means you've reached the end.
                        result.Add(new Token(Token.EndOfSourceText, "", lineNumber));
                        return result;
                     }
                     // We got a text mode comment, so we break back out to the outer loop.
                     break; // switch
                  }

                  // If we have accumulated a text string, record it as the next token before going into code mode.
                  if (textAccumulator != "")
                  {
                     result.Add(new Token(Token.Text, textAccumulator, lineNumber));
                     textAccumulator = "";
                  }

                  // This inner loop is for breaking out control codes
                  for (bool gotClosingBracket = false; !gotClosingBracket;)
                  {
                     if (!GetLetter())
                     {
                        result.Add(new Token(Token.EndOfSourceText, "", lineNumber));
                        return result;
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
                           result.Add(new Token(Token.Equal, "=", lineNumber));
                           break;

                        case '.':
                           result.Add(new Token(Token.Period, ".", lineNumber));
                           break;

                        case ',':
                           result.Add(new Token(Token.Comma, ",", lineNumber));
                           break;

                        case '[':
                           // Must be a comment.
                           if (!GetLetter())
                           {
                              result.Add(new Token(Token.EndOfSourceText, "", lineNumber));
                              return result;
                           }
                           if (gottenLetter != '[')
                           {
                              Log.Fail(String.Format("{0}: expected '[[' but got '[{1}'", lineNumber, gottenLetter));
                           }

                           if (!GetComment())
                           {
                              // If it returns false, it means you've reached the end.
                              result.Add(new Token(Token.EndOfSourceText, "", lineNumber));
                              return result;
                           }
                           // Done skipping comment. Move on to the next token.
                           break;

                        default:
                           if (!Char.IsLetterOrDigit(gottenLetter) || gottenLetter == '_')
                           {
                              Log.Fail(String.Format("line {0}: unexpected character '{1}'", lineNumber, gottenLetter));
                           }

                           string id = "";
                           do
                           {
                              id += gottenLetter;

                              if (!GetLetter())
                                 break;
                           } while (Char.IsLetterOrDigit(gottenLetter) || gottenLetter == '_');

                           UngetLetter();
                           if (id == "if")
                           {
                              result.Add(new Token(Token.If, id, lineNumber));
                           }
                           else if (id == "else")
                           {
                              result.Add(new Token(Token.Else, id, lineNumber));
                           }
                           else if (id == "or")
                           {
                              result.Add(new Token(Token.Or, id, lineNumber));
                           }
                           else if (id == "not")
                           {
                              result.Add(new Token(Token.Not, id, lineNumber));
                           }
                           else if (id == "end")
                           {
                              result.Add(new Token(Token.End, id, lineNumber));
                           }
                           else if (id == "when")
                           {
                              result.Add(new Token(Token.When, id, lineNumber));
                           }
                           else if (id == "tag")
                           {
                              result.Add(new Token(Token.Tag, id, lineNumber));
                           }
                           else if (id == "untag")
                           {
                              result.Add(new Token(Token.Untag, id, lineNumber));
                           }
                           else if (id == "bag")
                           {
                              result.Add(new Token(Token.Bag, id, lineNumber));
                           }
                           else if (id == "name")
                           {
                              result.Add(new Token(Token.Name, id, lineNumber));
                           }
                           else if (id == "as")
                           {
                              result.Add(new Token(Token.As, id, lineNumber));
                           }
                           else if (id == "start")
                           {
                              result.Add(new Token(Token.Start, id, lineNumber));
                           }
                           else if (specialIds.Contains(id))
                           {
                              result.Add(new Token(Token.Special, id, lineNumber));
                           }
                           else
                           {
                              // Variables start with an uppercase letter.
                              if (Char.IsUpper(id[0]))
                              {
                                 result.Add(new Token(Token.Variable, id, lineNumber));
                              }
                              else
                              {
                                 result.Add(new Token(Token.Id, id, lineNumber));
                              }
                           }
                           break;
                     }
                  }
                  break;
            }
         }
      }
   }
}
