using System;
using System.Collections.Generic;

namespace Game
{
  public static partial class Static
  {
    // These temporary global variables aren't that great and should be gotten rid of.
    private static char PushedLetter = '\0';
    private static char GottenLetter;
    private static int LetterIndex;
    private static string SourceText;

    private static bool GetLetter()
    {
      if (PushedLetter != '\0')
      {
        GottenLetter = PushedLetter;
        PushedLetter = '\0';
        return true;
      }

      if (LetterIndex >= SourceText.Length)
      {
        GottenLetter = '\0';
        return false;
      }

      GottenLetter = SourceText[LetterIndex];
      ++LetterIndex;
      return true;
    }

    private static void UngetLetter()
    {
      PushedLetter = GottenLetter;
    }

    private static bool GetComment()
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
        if (GottenLetter == '[')
        {
          ++level;
        }
        else if (GottenLetter == ']')
        {
          --level;
          if (level < 0)
          {
            if (!GetLetter())
              return false;
            if (GottenLetter == ']')
              return true;
            UngetLetter();
          }
        }
      }
    }

    public static (List<Token>, string) SourceTextToTokens(
      string sourceText)
    {
      List<Token> result = new List<Token>();
      string textAccumulator = "";
      int lineNumber = 1;

      LetterIndex = 0;
      SourceText = sourceText;

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

        switch (GottenLetter)
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
            textAccumulator += GottenLetter;
            break;

          case '[':
            if (!GetLetter())
            {
              result.Add(new Token(Token.EndOfSourceText, "", lineNumber));
              return (result, null);
            }

            // Check for a [[ comment starter.
            if (GottenLetter != '[')
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

              switch (GottenLetter)
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
                  if (GottenLetter != '[')
                  {
                    string syntaxError = String.Format("{0}: expected '[[' but got '[{1}'", lineNumber, GottenLetter);
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
                  if (!Char.IsLetterOrDigit(GottenLetter))
                  {
                    string syntaxError = String.Format("{0}: unexpected character '[{1}'", lineNumber, GottenLetter);
                    return (null, syntaxError);
                  }

                  string id = "";
                  do
                  {
                    id += GottenLetter;

                    if (!GetLetter())
                      break;
                  } while (Char.IsLetterOrDigit(GottenLetter) || GottenLetter == '.');

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
