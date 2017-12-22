using System;
using System.Collections.Generic;

namespace Game
{
  public static partial class Static
  {
    public static (ObjectSequence, string) TokensToObjactSequence(
      List<Token> tokens)
    {
      Token pushedToken = null;
      Token gottenToken;
      int tokenIndex = 0;

      // Some local helper functions.

      void GetToken()
      {
        if (pushedToken != null)
        {
          gottenToken = pushedToken;
          pushedToken = null;
        }
        else
        {
          // This should never go off the end. There is already an end of source text marker at the end of the tokens.
          gottenToken = tokens[tokenIndex];
          ++tokenIndex;
        }
      }

      void UngetToken()
      {
        pushedToken = gottenToken;
      }

      string Expected(
        string expected,
        Token actual)
      {
        return string.Format("{0}: expected {1} but got '{2}'", actual.LineNumber, expected, actual.Value);
      }

      // Start here

      var result = new ObjectSequence();

      while (true)
      {
        GetToken();

        if (gottenToken.Type == Token.Text)
        {
          result.Objects.Add(new ObjectText(gottenToken.Value));
        }
        else if (gottenToken.Type == Token.Id)
        {
          // This can be:
          //  [NAME :] LABEL-ID
          var label = gottenToken.Value;
          var name = "";
          GetToken();
          if (gottenToken.Type == Token.Period)
          {
            name = label;
            GetToken();
            if (gottenToken.Type != Token.Id)
              return (null, Expected(Token.Id.Name, gottenToken));
            label = gottenToken.Value;
          }
          else
          {
            UngetToken();
          }
          result.Objects.Add(new ObjectLookup(name, label));
        }
        else if (gottenToken.Type == Token.Tag)
        {
          // You can get one of these:
          //  tag [NAME :] LABEL-ID [= VALUE-ID]
          //  tag [NAME :] LABEL-ID = TOKENS [END]
          GetToken();
          if (gottenToken.Type != Token.Id)
            return (null, Expected(Token.Id.Name, gottenToken));

          // Let's assume it's going to be the label with no name.
          var label = gottenToken.Value;
          var name = "";
          GetToken();
          if (gottenToken.Type == Token.Period)
          {
            // But if it's followed by a period, it turns out to be the name.
            name = label;
            GetToken();

            // So the next ID must be the label.
            if (gottenToken.Type != Token.Id)
              return (null, Expected(Token.Id.Name, gottenToken));
            label = gottenToken.Value;
          }
          else
          {
            // If no period, there's no name. Pretend we didn't get the colon.
            UngetToken();
          }

          // I'm not implementing the '=' yet.

          result.Objects.Add(new ObjectTag(name, label, ""));
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

