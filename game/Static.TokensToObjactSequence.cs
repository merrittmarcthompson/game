using System;
using System.Collections.Generic;

namespace Game
{
  public static partial class Static
  {
    // These temporary global variables aren't that great and should be gotten rid of.
    private static Token PushedToken;
    private static Token GottenToken;
    private static List<Token> Tokens;
    private static int TokenIndex;

    private static void GetToken()
    {
      if (PushedToken != null)
      {
        GottenToken = PushedToken;
        PushedToken = null;
      }
      else
      {
        // This should never go off the end. There is already an end of source text marker at the end of the tokens.
        GottenToken = Tokens[TokenIndex];
        ++TokenIndex;
      }
    }

    private static void UngetToken()
    {
      PushedToken = GottenToken;
    }

    private static string Expected(
      string expected,
      Token actual)
    {
      return string.Format("{0}: expected {1} but got '{2}'", actual.LineNumber, expected, actual.Value);
    }

    public static (ObjactSequence, string) TokensToObjactSequence(
      List<Token> tokens)
    {
      Tokens = tokens;
      PushedToken = null;
      TokenIndex = 0;

      var result = new ObjactSequence();

      while (true)
      {
        GetToken();

        if (GottenToken.Type == Token.Text)
        {
          result.Objacts.Add(new ObjactText(GottenToken.Value));
        }
        else if (GottenToken.Type == Token.Id)
        {
          result.Objacts.Add(new ObjactLookup(GottenToken.Value));
        }
        /*
        else if (GottenToken.Type == Token.If)
        {
          var ifSource = GetIf(tokens, isActionSequence);
          result.Objacts.Add(ifSource);

          // The whole if/or case statement is terminated by 'end'.
          GetToken();
          if (GottenToken.Type != Token.End)
            return (null, Expected(Token.End.Name, GottenToken));
        }
        else if (GottenToken.Type == Token.Set)
        {
          GetToken();
          if (GottenToken.Type != Token.Id)
            return (null, Expected(Token.Id.Name, GottenToken));
          result.Objacts.Add(new ObjactSet(GottenToken.Value));
        }
        else if (!isActionSequence && (GottenToken.Type == Token.Lower || GottenToken.Type == Token.Raise))
        {
          var type = GottenToken.Type;
          GetToken();
          if (GottenToken.Type != Token.Id)
            throw new SyntaxError(GottenToken, Token.Id);
          int scoreIndex = Array.FindIndex(ScoreIds, id => id.NoCaseEquals(GottenToken.Value));
          if (scoreIndex == -1)
            throw new SyntaxError(GottenToken, "expected a score ID after {0} but got {1}", type.ToString(), GottenToken.Value);
          result.Sources.Add(new ScoreSource(scoreIndex, type == Token.Raise));
        }
        */
        else
        {
          // Hopefully this token is something the caller is expecting to see next (i.e. end of source text).
          UngetToken();
          return (result, null);
        }
      }
    }
  }
}

