using System;
using System.Collections.Generic;

namespace Game
{
  public static partial class Static
  {
    public static SequenceObject TokensToObject(
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

      string ExpectedIdOrVariable(
        Token actual)
      {
        return string.Format("line {0}: expected an ID or variable but got '{1}'", actual.LineNumber, actual.Value);
      }

      TagSpec GetTagSpec(
        bool allowEqual)
      {
        // You can get one of these:
        //  tag [ID|VARIABLE .] LABEL-ID {. LABEL-ID} [= ID|VARIABLE]
        //  tag [ID|VARIABLE .] LABEL-ID [. LABEL_ID} = TOKENS [END]
        GetToken();
        if (gottenToken.Type != Token.Id && gottenToken.Type != Token.Variable)
        {
          Log.Add(ExpectedIdOrVariable(gottenToken));
          return null;
        }

        // Let's assume it's going to be the label with no name.
        var result = new TagSpec();
        result.Name = "";
        var tempLabel = gottenToken.Value;
        GetToken();
        if (gottenToken.Type == Token.Period)
        {
          // But if it's followed by a period, it turns out to be the name.
          result.Name = tempLabel;
          GetToken();

          // So the next ID must be the label.
          if (gottenToken.Type != Token.Id)
          {
            Log.Add(Expected(Token.Id.Name, gottenToken));
            return null;
          }
          result.Labels.Add(gottenToken.Value);

          // Let's see if there are any more labels.
          GetToken();
          while (gottenToken.Type == Token.Period)
          {
            GetToken();
            if (gottenToken.Type != Token.Id)
            {
              Log.Add(Expected(Token.Id.Name, gottenToken));
              return null;
            }
            result.Labels.Add(gottenToken.Value);
          }
        }
        else
        {
          // There's just this one label.
          result.Labels.Add(tempLabel);
        }

        // We've already got a pending unidentified token at this point.
        if (!allowEqual)
        {
          // Whatever it is, put it back for somebody else to look at.
          UngetToken();
        }
        else
        {
          if (gottenToken.Type == Token.Equal)
          {
            GetToken();
            if (gottenToken.Type != Token.Id && gottenToken.Type != Token.Variable)
            {
              Log.Add(ExpectedIdOrVariable(gottenToken));
              return null;
            }
            result.Value = gottenToken.Value;
          }
          else
          {
            result.Value = "";
            UngetToken();
          }
        }

        return result;
      }

      (List<bool>, List<TagSpec>) GetBooleanList()
      {
        var nots = new List<bool>();
        var tagSpecs = new List<TagSpec>();
        do
        {
          GetToken();
          if (gottenToken.Type == Token.Not)
          {
            nots.Add(true);
          }
          else
          {
            nots.Add(false);
            UngetToken();
          }
          var tagSpec = GetTagSpec(true);
          tagSpecs.Add(tagSpec);
          GetToken();
        } while (gottenToken.Type == Token.Comma);
        UngetToken();
        return (nots, tagSpecs);
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
      IfObject GetIf()
      {
        // This is called after getting 'if' or 'or'.
        // First get the expression. It's like one of these:
        //   [if brave]
        //   [if not killedInspector]
        var result = new IfObject();
        (result.Nots, result.TagSpecs) = GetBooleanList();

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

      SequenceObject GetSequence()
      {
        var result = new SequenceObject();

        while (true)
        {
          GetToken();

          if (gottenToken.Type == Token.Text)
          {
            result.Objects.Add(new TextObject(gottenToken.Value));
          }
          else if (gottenToken.Type == Token.Special)
          {
            // Ex. [p]
            result.Objects.Add(new SpecialObject(gottenToken.Value));
          }
          else if (gottenToken.Type == Token.Id)
          {
            // This is a text substitution.
            var tagSpec = GetTagSpec(false);
            if (tagSpec == null)
              return null;
            result.Objects.Add(new SubstitutionObject(tagSpec));
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
            result.Objects.Add(new NameObject(gottenToken.Value));
          }
          else if (gottenToken.Type == Token.Tag || gottenToken.Type == Token.Untag)
          {
            var tagSpec = GetTagSpec(true);
            if (tagSpec == null)
              return null;
            result.Objects.Add(new TagObject(tagSpec, gottenToken.Type == Token.Tag));
          }
          else if (gottenToken.Type == Token.When)
          {
            var whenObject = new WhenObject();
            (whenObject.Nots, whenObject.TagSpecs) = GetBooleanList();
          }
          else if (gottenToken.Type == Token.If)
          {
            var ifObject = GetIf();
            result.Objects.Add(ifObject);

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
