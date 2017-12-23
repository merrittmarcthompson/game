using System;
using System.Collections.Generic;

namespace Game
{
  public static partial class Static
  {
    public static ObjectSequence TokensToObject(
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
        return string.Format("line {0}: expected {1} but got '{2}'", actual.LineNumber, expected, actual.Value);
      }

      (string name, string label, string value) GetTagSpecification()
      {
        // You can get one of these:
        //  tag [NAME :] LABEL-ID [= VALUE-ID]
        //  tag [NAME :] LABEL-ID = TOKENS [END]
        GetToken();
        if (gottenToken.Type != Token.Id)
        {
          Log.Add(Expected(Token.Id.Name, gottenToken));
          return (null, null, null);
        }

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
          {
            Log.Add(Expected(Token.Id.Name, gottenToken));
            return (null, null, null);
          }
          label = gottenToken.Value;
        }
        else
        {
          // If no period, there's no name. Pretend we didn't get the colon.
          UngetToken();
        }

        // I'm not implementing the '=' yet.
        var value = "";

        return (name, label, value);
      }

      /* This flat 'if' sequence with 'or' (which is like 'elif' but more Englishy)

        [if reaction=Flee]
          A
        [or reaction=Escape]
          B
        [else]
          C
        [end]

       is equivalent to this nested sequence:

        if reaction=Flee
          A
        else
          if reaction=Escape
            B
          else
            C
      */
      ObjectIf GetIf()
      {
        // This is called after getting 'if' or 'or'.
        // First get the expression. It's like one of these:
        //   [if brave]
        //   [if not killedInspector]
        var result = new ObjectIf();

        GetToken();
        if (gottenToken.Type == Token.Not)
        {
          result.Not = true;
          GetToken();
        }
        else
        {
          result.Not = false;
          UngetToken();
        }

        (result.Name, result.Label, result.Value) = GetTagSpecification();

        result.TrueSource = GetSequence();

        GetToken();
        if (gottenToken.Type == Token.Else)
        {
          result.FalseSource = GetSequence();
        }
        else if (gottenToken.Type == Token.Or)
        {
          result.FalseSource = GetIf();
        }
        else
        {
          // Must be 'end'. Let caller handle it.
          UngetToken();
          result.FalseSource = null;
        }
        return result;
      }

      ObjectSequence GetSequence()
      {
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
              {
                Log.Add(Expected(Token.Id.Name, gottenToken));
                return null;
              }
              label = gottenToken.Value;
            }
            else
            {
              UngetToken();
            }
            result.Objects.Add(new ObjectSubstitution(name, label));
          }
          else if (gottenToken.Type == Token.Name)
          {
            // [name bathroomDoor]
            GetToken();
            if (gottenToken.Type != Token.Id)
            {
              Log.Add(Expected(Token.Name.Name, gottenToken));
              return null;
            }
            result.Objects.Add(new ObjectName(gottenToken.Value));
          }
          else if (gottenToken.Type == Token.Tag || gottenToken.Type == Token.Untag)
          {
            (var name, var label, var value) = GetTagSpecification();
            if (name == null)
              return null;
            result.Objects.Add(new ObjectTag(name, label, value, gottenToken.Type == Token.Tag));
          }
          else if (gottenToken.Type == Token.If)
          {
            var objectIf = GetIf();
            result.Objects.Add(objectIf);

            // The whole if/or case statement is terminated by 'end'.
            GetToken();
            if (gottenToken.Type != Token.End)
            {
              Log.Add(Expected(Token.End.Name, gottenToken));
              return null;
            }
          }
          else
          {
            // Hopefully this token is something the caller is expecting to see next (i.e. end of source text).
            UngetToken();
            return result;
          }
        }
      }

      // Start here
      return GetSequence();
    }
  }
}
