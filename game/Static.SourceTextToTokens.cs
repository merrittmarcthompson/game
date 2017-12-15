using System;
using System.Collections.Generic;

namespace Game
{
  public static partial class Static
  {
    public static (List<Token>, string) SourceTextToTokens(
      string sourceText)
    {
      char pushedLetter = '\0';
      char gottenLetter;
      int letterIndex;

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
          return (result, null);
        }

        switch (gottenLetter)
        {
          case '\n':
            // [p] lets you explicitly put in a paragraph break. We'll clean up any extra spaces later. This lets you break continguous text up into multiple lines within 'if' groups without having it affect formatting.
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
              return (result, null);
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
                return (result, null);
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
                return (result, null);
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

                case '[':
                  // Must be a comment.
                  if (!GetLetter())
                  {
                    result.Add(new Token(Token.EndOfSourceText, "", lineNumber));
                    return (result, null);
                  }
                  if (gottenLetter != '[')
                  {
                    string syntaxError = String.Format("{0}: expected '[[' but got '[{1}'", lineNumber, gottenLetter);
                    return (null, syntaxError);
                  }

                  if (!GetComment())
                  {
                    // If it returns false, it means you've reached the end.
                    result.Add(new Token(Token.EndOfSourceText, "", lineNumber));
                    return (result, null);
                  }
                  // Done skipping comment. Move on to the next token.
                  break;

                default:
                  if (!Char.IsLetterOrDigit(gottenLetter))
                  {
                    string syntaxError = String.Format("{0}: unexpected character '{1}'", lineNumber, gottenLetter);
                    return (null, syntaxError);
                  }

                  string id = "";
                  do
                  {
                    id += gottenLetter;

                    if (!GetLetter())
                      break;
                  } while (Char.IsLetterOrDigit(gottenLetter) || gottenLetter == '.');

                  UngetLetter();
                  if (id.NoCaseEquals("if"))
                  {
                    result.Add(new Token(Token.If, id, lineNumber));
                  }
                  else if (id.NoCaseEquals("else"))
                  {
                    result.Add(new Token(Token.Else, id, lineNumber));
                  }
                  else if (id.NoCaseEquals("or"))
                  {
                    result.Add(new Token(Token.Or, id, lineNumber));
                  }
                  else if (id.NoCaseEquals("not"))
                  {
                    result.Add(new Token(Token.Not, id, lineNumber));
                  }
                  else if (id.NoCaseEquals("end"))
                  {
                    result.Add(new Token(Token.End, id, lineNumber));
                  }
                  else if (id.NoCaseEquals("raise"))
                  {
                    result.Add(new Token(Token.Raise, id, lineNumber));
                  }
                  else if (id.NoCaseEquals("lower"))
                  {
                    result.Add(new Token(Token.Lower, id, lineNumber));
                  }
                  else if (id.NoCaseEquals("set"))
                  {
                    result.Add(new Token(Token.Set, id, lineNumber));
                  }
                  else
                  {
                    result.Add(new Token(Token.Id, id, lineNumber));
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
