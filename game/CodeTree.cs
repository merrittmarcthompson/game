#nullable enable
using System;
using System.Collections.Generic;

namespace Gamebook
{
   public class CodeTree
   {
      // CodeTree is an easy-to-use interface for clients of code trees. It lets you create them from source code text and then traverse the tree. The tree is composed of Code objects.
      private readonly SequenceCode RootCode;

      // You can also get the original source code text for error messages.
      public string SourceText { get; }

      public IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker = null)
      {
         return RootCode.Traverse(branchPicker);
      }

      public CodeTree(
        string sourceText,
        string sourceNameForErrorMessages,
        Dictionary<string, Setting> settings)
      {
         // Compile the text to a code tree.
         SourceText = sourceText;
         var tokenList = new TokenList(sourceText, sourceNameForErrorMessages, settings);
         var Look = new LookAhead(tokenList);

         RootCode = ParseSequence();
         Look.Require(TokenType.EndOfSourceText, sourceText, sourceNameForErrorMessages);

         // Some local helper functions.

         SequenceCode ParseSequence()
         {
            var codes = new List<Code>();

            while (true)
            {
               if (Look.Got(TokenType.Characters))
                  codes.Add(new CharacterCode(Look.Value));
               else if (Look.Got(TokenType.SpecialId))
                  // Ex. [he]
                  codes.Add(new SpecialCode(Look.Value));
               else if (Look.Got(TokenType.Merge))
               {
                  // [merge]
                  // [merge sceneId]
                  string? sceneId = Look.Got(TokenType.Id) ? Look.Value : null;
                  codes.Add(new MergeCode(sceneId));
               }
               else if (Look.Got(TokenType.Return))
               {
                  // [return]
                  codes.Add(new ReturnCode());
               }
               else if (Look.Got(TokenType.Scene))
               {
                  // [scene soundsLikeAScam]
                  Look.Require(TokenType.Id, sourceText, sourceNameForErrorMessages);
                  codes.Add(new SceneCode(Look.Value));
               }
               else if (Look.Got(TokenType.Score) || Look.Got(TokenType.Sort))
               {
                  // SCORE SCOREID [, SCOREID...]
                  // SORT SCOREID [, ID...]
                  var sortOnly = Look.Type == TokenType.Sort;
                  List<string> ids = new List<string>();
                  do
                  {
                     Look.Require(TokenType.ScoreId, sourceText, sourceNameForErrorMessages);
                     ids.Add(Look.Value);
                  } while (Look.Got(TokenType.Comma));
                  codes.Add(new ScoreCode(ids, sortOnly));
               }
               else if (Look.Got(TokenType.Text))
               {
                  Look.Require(TokenType.Id, sourceText, sourceNameForErrorMessages);
                  string id = Look.Value;
                  string text = "";
                  if (Look.Got(TokenType.Characters))
                     text = Look.Value;
                  Look.Require(TokenType.End, sourceText, sourceNameForErrorMessages);
                  codes.Add(new TextCode(id, text));
               }
               else if (Look.Got(TokenType.Set))
                  codes.Add(new SetCode(ParseExpressions(false)));
               else if (Look.Got(TokenType.When))
               {
                  if (Look.Got(TokenType.Else))
                     codes.Add(new WhenElseCode());
                  else
                     codes.Add(new WhenCode(ParseExpressions(true)));
               }
               else if (Look.Got(TokenType.If))
               {
                  var ifCode = ParseIf();
                  codes.Add(ifCode);

                  // The whole if/or case statement is terminated by 'end'.
                  Look.Require(TokenType.End, sourceText, sourceNameForErrorMessages);
               }
               else
               {
                  // Hopefully the token we've been looking at is something the caller is expecting to see next (i.e. end of source text).
                  return new SequenceCode(codes);
               }
            }
         }

         List<Expression> ParseExpressions(
            bool allowNotEqual)
         {
            // BOOLEANID
            // NOT BOOLEANID
            // STRINGID=ID
            // NOT STRINGID=ID
            // allowNotEqual: [when not a=b] makes sense. But [set not a=b] doesn't mean anything.
            var result = new List<Expression>();
            do
            {
               var not = Look.Got(TokenType.Not);
               string leftId;
               string? rightId = null;
               if (Look.Got(TokenType.BooleanId) || Look.Got(TokenType.ScoreId))
                  leftId = Look.Value;
               else
               {
                  if (not && !allowNotEqual)
                     throw new InvalidOperationException(string.Format($"file {sourceNameForErrorMessages}: unexpected {TokenType.Not} in\n{sourceText}"));
                  Look.Require(TokenType.StringId, sourceText, sourceNameForErrorMessages);
                  leftId = Look.Value;
                  Look.Require(TokenType.Equal, sourceText, sourceNameForErrorMessages);
                  Look.Require(TokenType.Id, sourceText, sourceNameForErrorMessages);
                  rightId = Look.Value;
               }
               result.Add(new Expression(not, leftId, rightId));
            } while (Look.Got(TokenType.Comma));
            return result;
         }

         /* This flat 'if' sequence with 'or' (which is like 'elif' but more Englishy)...

           [if reaction=Flee]
             A
           [or reaction=Escape]
             B
           [else]
             C
           [end]

          ...is equivalent to this nested sequence:

           if reaction=Flee
             A
           else
             if reaction=Escape
               B
             else
               C
         */
         IfCode ParseIf()
         {
            // This is called after getting 'if' or 'or'.
            // First get the expression. It's like one of these:
            //   [if brave]
            //   [if not killedInspector]
            var expressions = ParseExpressions(true);
            var trueCode = ParseSequence();
            Code? falseCode = null;

            if (Look.Got(TokenType.Else))
               falseCode = ParseSequence();
            else if (Look.Got(TokenType.Or))
               falseCode = ParseIf();
            // Otherwise must be 'end'. Let caller handle it.
            return new IfCode(expressions, trueCode, falseCode);
         }
      }
   }
}
