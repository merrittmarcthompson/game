using System;
using System.Collections.Generic;

namespace Game
{
  public static partial class Static
  {
    public static SequenceObject TokensToObjects(
      List<Token> tokens)
    {
      Token PushedToken = null;
      Token GottenToken;
      int TokenIndex = 0;

      // Some local helper functions.

      void GetToken()
      {
        if (PushedToken != null)
        {
          GottenToken = PushedToken;
          PushedToken = null;
        }
        else
        {
          // This should never go off the end. There is already an end of source text marker at the end of the tokens.
          GottenToken = tokens[TokenIndex];
          ++TokenIndex;
        }
      }

      void UngetToken()
      {
        PushedToken = GottenToken;
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

      (string name, List<string> labels) GetLabels(
        string startingIdOrVariable)
      {
        // Let's assume the ID is going to be the label with no name. This allows a variable as a label name, ex. 'hero.Foo', which isn't quite right, but I don't think that will cause any problems.
        var resultLabels = new List<string>();
        string resultName = "";
        var tempLabel = startingIdOrVariable;
        GetToken();
        if (GottenToken.Type == Token.Period)
        {
          // But if it's followed by a period, it turns out to be the name.
          resultName = tempLabel;
          GetToken();

          // So the next ID must be the label.
          if (GottenToken.Type != Token.Id)
          {
            Log.Fail(Expected(Token.Id.Name, GottenToken));
          }
          resultLabels.Add(GottenToken.Value);

          // Let's see if there are any more labels.
          GetToken();
          while (GottenToken.Type == Token.Period)
          {
            GetToken();
            if (GottenToken.Type != Token.Id)
            {
              Log.Fail(Expected(Token.Id.Name, GottenToken));
            }
            resultLabels.Add(GottenToken.Value);
            GetToken();
          }
          // Put back the last non-period mystery token.
          UngetToken();
        }
        else
        {
          // There's just this one label.
          resultLabels.Add(tempLabel);
          // Put back the token that wasn't a period.
          UngetToken();
        }
        return (resultName, resultLabels);
      }

      Expression GetExpression()
      {
        // You can get one of these:
        //  [ID|VARIABLE .] LABEL-ID {. LABEL-ID} [= [ID|VARIABLE .] LABEL-ID {. LABEL-ID}]
        //  VARIABLE = [ID|VARIABLE .] LABEL-ID {. LABEL-ID}]
        //  [ID|VARIABLE .] LABEL-ID [. LABEL_ID} = TOKENS [END]
        var result = new Expression();
        GetToken();
        if (GottenToken.Type != Token.Id && GottenToken.Type != Token.Variable)
        {
          Log.Fail(ExpectedIdOrVariable(GottenToken));
        }

        // Handle the special case of 'VARIABLE ='.
        string firstId = GottenToken.Value;
        if (GottenToken.Type == Token.Variable)
        {
          GetToken();
          // If we get the '=', we're done on the left. Now get the right.
          if (GottenToken.Type == Token.Equal)
          {
            result.LeftName = firstId;
            GetToken();
            if (GottenToken.Type != Token.Id && GottenToken.Type != Token.Variable)
            {
              Log.Fail(ExpectedIdOrVariable(GottenToken));
            }
            (result.RightName, result.RightLabels) = GetLabels(GottenToken.Value);
            return result;
          }
          // Put back the token that isn't '=' for later processing.
          UngetToken();
        }
        (result.LeftName, result.LeftLabels) = GetLabels(firstId);
        GetToken();
        if (GottenToken.Type == Token.Equal)
        {
          GetToken();
          if (GottenToken.Type != Token.Id && GottenToken.Type != Token.Variable)
          {
            Log.Fail(ExpectedIdOrVariable(GottenToken));
          }
          (result.RightName, result.RightLabels) = GetLabels(GottenToken.Value);
        }
        else
        {
          UngetToken();
        }

        return result;
      }

      List<NotExpression> GetNotExpressions()
      {
        var result = new List<NotExpression>();
        do
        {
          var notExpression = new NotExpression();
          notExpression.Not = true;
          GetToken();
          if (GottenToken.Type != Token.Not)
          {
            notExpression.Not = false;
            UngetToken();
          }
          notExpression.Expression = GetExpression();
          result.Add(notExpression);
          GetToken();
        } while (GottenToken.Type == Token.Comma);
        UngetToken();
        return result;
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
        result.NotExpressions = GetNotExpressions();

        result.TrueSource = GetSequence();

        GetToken();
        if (GottenToken.Type == Token.Else)
        {
          result.FalseSource = GetSequence();
        }
        else if (GottenToken.Type == Token.Or)
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

          if (GottenToken.Type == Token.Text)
          {
            result.Objects.Add(new TextObject(GottenToken.Value));
          }
          else if (GottenToken.Type == Token.Special)
          {
            // Ex. [p]
            result.Objects.Add(new SpecialObject(GottenToken.Value));
          }
          else if (GottenToken.Type == Token.Id)
          {
            // This is a text substitution.
            var substitutionObject = new SubstitutionObject();
            (substitutionObject.Expression.LeftName, substitutionObject.Expression.LeftLabels) = GetLabels(GottenToken.Value);
            result.Objects.Add(substitutionObject);
          }
          else if (GottenToken.Type == Token.Name)
          {
            // [name bathroomDoor]
            GetToken();
            if (GottenToken.Type != Token.Id)
            {
              Log.Fail(Expected(Token.Name.Name, GottenToken));
            }
            result.Objects.Add(new NameObject(GottenToken.Value));
          }
          else if (GottenToken.Type == Token.Tag || GottenToken.Type == Token.Untag)
          {
            var tagSpec = GetExpression();
            if (tagSpec == null)
              return null;
            result.Objects.Add(new TagObject(tagSpec, GottenToken.Type == Token.Tag));
          }
          else if (GottenToken.Type == Token.When)
          {
            var whenObject = new WhenObject();
            whenObject.NotExpressions = GetNotExpressions();
            result.Objects.Add(whenObject);
          }
          else if (GottenToken.Type == Token.If)
          {
            var ifObject = GetIf();
            result.Objects.Add(ifObject);

            // The whole if/or case statement is terminated by 'end'.
            GetToken();
            if (GottenToken.Type != Token.End)
            {
              Log.Fail(Expected(Token.End.Name, GottenToken));
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
