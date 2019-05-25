using System.Collections.Generic;
using System.Linq;

namespace Gamebook
{
   public static partial class Transform
   {
      public static SequenceObject TokensToObjects(
        List<Token> tokens,
        string sourceText)
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

         List<Expression> GetExpressions(
            bool allowNotEqual)
         {
            // ID
            // NOT ID
            // ID=ID
            // NOT ID=ID
            // allowNotEqual: [when not a=b] makes sense. But [set not a=b] doesn't mean anything.
            var result = new List<Expression>();
            do
            {
               var expression = new Expression();
               expression.Not = true;
               GetToken();
               if (GottenToken.Type != Token.Not)
               {
                  expression.Not = false;
                  UngetToken();
               }
               GetToken();
               if (GottenToken.Type != Token.Id)
               {
                  Log.Fail(Expected(Token.Id.Name, GottenToken));
               }
               expression.LeftId = GottenToken.Value;
               if (allowNotEqual || !expression.Not)
               {
                  GetToken();
                  if (GottenToken.Type != Token.Equal)
                  {
                     UngetToken();
                  }
                  else
                  {
                     GetToken();
                     if (GottenToken.Type != Token.Id)
                     {
                        Log.Fail(Expected(Token.Id.Name, GottenToken));
                     }
                     expression.RightId = GottenToken.Value;
                  }
               }
               result.Add(expression);
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
            result.Expressions = GetExpressions(true);

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

               if (GottenToken.Type == Token.Characters)
               {
                  result.Objects.Add(new CharacterObject(GottenToken.Value));
               }
               else if (GottenToken.Type == Token.Special)
               {
                  // Ex. [he]
                  result.Objects.Add(new SpecialObject(GottenToken.Value));
               }
               // I think this lets by too many mistakes. You put in some wrong ID somewhere, and it says, okay, it's a substitution, rather than complaining. Maybe have something like [insert bobsShoeSize] instead of [bobsShoeSize]?
               /*
               else if (GottenToken.Type == Token.Id)
               {
                  // This is a text substitution.
                  var substitutionObject = new SubstitutionObject();
                  substitutionObject.Id = GottenToken.Value;
                  result.Objects.Add(substitutionObject);
               }
               */
               else if (GottenToken.Type == Token.Merge)
               {
                  // [merge]
                  // [merge sceneId]
                  string sceneId = null;

                  GetToken();
                  if (GottenToken.Type == Token.Id)
                  {
                     sceneId = GottenToken.Value;
                  }
                  else
                  {
                     UngetToken();
                  }
                  result.Objects.Add(new MergeObject(sceneId));
               }
               else if (GottenToken.Type == Token.Scene)
               {
                  // [scene soundsLikeAScam]
                  GetToken();
                  if (GottenToken.Type != Token.Id)
                  {
                     Log.Fail(Expected(Token.Scene.Name, GottenToken));
                  }
                  result.Objects.Add(new SceneObject(GottenToken.Value));
               }
               else if (GottenToken.Type == Token.Score)
               {
                  // SCORE ID [, ID...]
                  List<string> ids = new List<string>();
                  do
                  {
                     GetToken();
                     if (GottenToken.Type != Token.Id)
                     {
                        Log.Fail(Expected(Token.Score.Name, GottenToken));
                     }
                     ids.Add(GottenToken.Value);
                     GetToken();
                  } while (GottenToken.Type == Token.Comma);
                  UngetToken();
                  result.Objects.Add(new ScoreObject(ids));
               }
               else if (GottenToken.Type == Token.Text)
               {
                  GetToken();
                  if (GottenToken.Type != Token.Id)
                  {
                     Log.Fail(Expected(Token.Id.Name, GottenToken));
                  }
                  string id = GottenToken.Value;
                  SequenceObject text = GetSequence();
                  if (text == null)
                     return null;
                  GetToken();
                  if (GottenToken.Type != Token.End)
                  {
                     Log.Fail(Expected(Token.End.Name, GottenToken));
                  }
                  result.Objects.Add(new TextObject(id, text));
               }
               else if (GottenToken.Type == Token.Set)
               {
                  var setObject = new SetObject();
                  setObject.Expressions = GetExpressions(false);
                  result.Objects.Add(setObject);
               }
               else if (GottenToken.Type == Token.When)
               {
                  var whenObject = new WhenObject();
                  whenObject.Expressions = GetExpressions(true);
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
         var sequenceObject = GetSequence();
         if (sequenceObject == null)
            return null;
         GetToken();
         if (GottenToken.Type != Token.EndOfSourceText)
         {
            Log.Fail(Expected(Token.EndOfSourceText.Name, GottenToken));
            return null;
         }
         sequenceObject.SourceText = sourceText;
         return sequenceObject;
      }
   }
}
