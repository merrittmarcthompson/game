using System;
using System.Collections.Generic;

namespace Gamebook
{
   // These classes make up the Code data structure.

   public abstract class Code
   {
      // Traverse is the routine that allows other modules to execute the code.
      public abstract void Traverse(
        Func<Code, bool> examine);
      // This makes Code objects appear in the debugger.
      public abstract override string ToString();

      public static Code Compile(
        string sourceCode)
      {
         // Compile the text to a code sequence.
         Log.SetSourceCode(sourceCode);
         var tokens = Token.Tokenize(sourceCode);
         if (tokens == null)
            return null;
         return SequenceCode.BuildFromTokens(tokens, sourceCode);
      }
   }

   public class SequenceCode: Code
   {
      // This is a sequence of code operations. The Traverse function is the only way to get to the Codes.
      private List<Code> Codes { get; set; } = new List<Code>();
      private string SourceText = null;

      public override void Traverse(
        Func<Code, bool> examine)
      {
         foreach (var code in Codes)
         {
            string previousSourceText = null;
            if (SourceText != null)
               previousSourceText = Log.SetSourceCode(SourceText);
            code.Traverse(examine);
            Log.SetSourceCode(previousSourceText);
         }
      }

      public override string ToString()
      {
         string result = "";
         string separator = "";
         foreach (var code in Codes)
         {
            string objectString = code.ToString();
            if (objectString.Length > 16)
               objectString = objectString.Substring(0, 16) + "...";
            result += separator + objectString;
            separator = "|";
         }
         return result;
      }

      private class LookAhead
      {
         private List<Token> Tokens;
         private int TokenIndex = 0;
         public string Value { get; private set; }

         public LookAhead(
            List<Token> tokens)
         {
            Tokens = tokens;
         }

         public bool TryGet(
            TokenType type)
         {
            // This should never go off the end. There is already an end of source text marker at the end of the tokens.
            var token = Tokens[TokenIndex++];
            if (token.Type == type)
            {
               Value = token.Value;
               return true;
            }
            if (TokenIndex == 0)
               throw new InvalidOperationException("Negative token index in code parser.");
            --TokenIndex;
            Value = null;
            return false;
         }

         public void MustGet(
            TokenType expected)
         {
            // This should never go off the end. There is already an end of source text marker at the end of the tokens.
            var actual = Tokens[TokenIndex++];
            if (actual.Type != expected)
               throw new InvalidOperationException(string.Format($"line {actual.LineNumber}: expected {expected} but got '{actual.Value}'"));
            Value = actual.Value;
         }
      }

      // All sequence codes come from BuildFromTokens.
      private SequenceCode() { }

      // This allows base Code class to construct a sequence.
      public static SequenceCode BuildFromTokens(
         List<Token> tokens,
         string sourceText)
      {
         LookAhead Look = new LookAhead(tokens);
         var sequenceCode = GetSequence();
         Look.MustGet(TokenType.EndOfSourceText);
         sequenceCode.SourceText = sourceText;
         return sequenceCode;

         // Some local helper functions.

         SequenceCode GetSequence()
         {
            var result = new SequenceCode();

            while (true)
            {
               if (Look.TryGet(TokenType.Characters))
                  result.Codes.Add(new CharacterCode(Look.Value));
               else if (Look.TryGet(TokenType.Special))
                  // Ex. [he]
                  result.Codes.Add(new SpecialCode(Look.Value));
               // I think this lets by too many mistakes. You put in some wrong ID somewhere, and it says, okay, it's a substitution, rather than complaining. Maybe have something like [insert bobsShoeSize] instead of [bobsShoeSize]?
               /*
               else if (GottenToken.Type == TokenType.Id)
               {
                  // This is a text substitution.
                  var substitutionCode = new SubstitutionCode();
                  substitutionCode.Id = GottenToken.Value;
                  result.Objects.Add(substitutionCode);
               }
               */
               else if (Look.TryGet(TokenType.Merge))
               {
                  // [merge]
                  // [merge sceneId]
                  string sceneId = null;
                  if (Look.TryGet(TokenType.Id))
                     sceneId = Look.Value;
                  result.Codes.Add(new MergeCode(sceneId));
               }
               else if (Look.TryGet(TokenType.Scene))
               {
                  // [scene soundsLikeAScam]
                  Look.MustGet(TokenType.Id);
                  result.Codes.Add(new SceneCode(Look.Value));
               }
               else if (Look.TryGet(TokenType.Score))
               {
                  // SCORE ID [, ID...]
                  List<string> ids = new List<string>();
                  do
                  {
                     Look.MustGet(TokenType.Id);
                     ids.Add(Look.Value);
                  } while (Look.TryGet(TokenType.Comma));
                  result.Codes.Add(new ScoreCode(ids));
               }
               else if (Look.TryGet(TokenType.Text))
               {
                  Look.MustGet(TokenType.Id);
                  string id = Look.Value;
                  string text = "";
                  if (Look.TryGet(TokenType.Characters))
                     text = Look.Value;
                  Look.MustGet(TokenType.End);
                  result.Codes.Add(new TextCode(id, text));
               }
               else if (Look.TryGet(TokenType.Set))
                  result.Codes.Add(new SetCode(GetExpressions(false)));
               else if (Look.TryGet(TokenType.When))
                  result.Codes.Add(new WhenCode(GetExpressions(true)));
               else if (Look.TryGet(TokenType.If))
               {
                  var ifCode = GetIf();
                  result.Codes.Add(ifCode);

                  // The whole if/or case statement is terminated by 'end'.
                  Look.MustGet(TokenType.End);
               }
               else
               {
                  // Hopefully the token we've been looking at is something the caller is expecting to see next (i.e. end of source text).
                  return result;
               }
            }
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
               var not = true;
               if (!Look.TryGet(TokenType.Not))
                  not = false;
               Look.MustGet(TokenType.Id);
               var leftId = Look.Value;
               string rightId = null;
               if (allowNotEqual || !not)
               {
                  if (Look.TryGet(TokenType.Equal))
                  {
                     Look.MustGet(TokenType.Id);
                     rightId = Look.Value;
                  }
               }
               result.Add(new Expression(not, leftId, rightId));
            } while (Look.TryGet(TokenType.Comma));
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
         IfCode GetIf()
         {
            // This is called after getting 'if' or 'or'.
            // First get the expression. It's like one of these:
            //   [if brave]
            //   [if not killedInspector]
            var expressions = GetExpressions(true);
            var trueCode = GetSequence();
            Code falseCode = null;

            if (Look.TryGet(TokenType.Else))
               falseCode = GetSequence();
            else if (Look.TryGet(TokenType.Or))
               falseCode = GetIf();
            // Otherwise must be 'end'. Let caller handle it.
            return new IfCode(expressions, trueCode, falseCode);
         }
      }
   }

   public class IfCode: Code
   {
      private List<Expression> Expressions;
      public IEnumerable<Expression> GetExpressions()
      {
         return Expressions;
      }
      public Code TrueCode { get; private set; }
      public Code FalseCode { get; private set; }

      private IfCode() { }
      public IfCode(
         List<Expression> expressions,
         Code trueCode,
         Code falseCode)
      {
         Expressions = expressions;
         TrueCode = trueCode;
         FalseCode = falseCode;
      }

      public override void Traverse(
        Func<Code, bool> examine)
      {
         if (examine(this))
            TrueCode.Traverse(examine);
         else if (FalseCode != null)
            FalseCode.Traverse(examine);
      }

      public override string ToString()
      {
         return "if " + Expressions.ToString();
      }
   }

   public class WhenCode: Code
   {
      private List<Expression> Expressions;
      public IEnumerable<Expression> GetExpressions()
      {
         return Expressions;
      }

      private WhenCode() { }
      public WhenCode(
         List<Expression> expressions)
      {
         Expressions = expressions;
      }

      public override void Traverse(
        Func<Code, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return "when " + Expressions.ToString();
      }
   }

   public class SetCode: Code
   {
      private List<Expression> Expressions;
      public IEnumerable<Expression> GetExpressions()
      {
         return Expressions;
      }

      private SetCode() { }
      public SetCode(
         List<Expression> expressions)
      {
         Expressions = expressions;
      }

      public override void Traverse(
         Func<Code, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return "set" + Expressions.ToString();
      }
   }

   public class ScoreCode: Code
   {
      public List<string> Ids { get; private set; }

      private ScoreCode() { }
      public ScoreCode(
         List<string> ids)
      {
         Ids = ids;
      }

      public override void Traverse(
        Func<Code, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return "score " + Ids.ToString();
      }
   }

   public class SubstitutionCode: Code
   {
      public string Id { get; private set; }

      private SubstitutionCode() { }
      public SubstitutionCode(
         string id)
      {
         Id = id;
      }

      public override void Traverse(
        Func<Code, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return "[" + Id + "]";
      }
   }

   public class TextCode: Code
   {
      public string Id { get; private set; }
      public string Text { get; private set; }

      private TextCode() { }
      public TextCode(
         string id,
         string text)
      {
         Id = id;
         Text = text;
      }

      public override void Traverse(
        Func<Code, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return Text.ToString();
      }
   }

   public class CharacterCode: Code
   {
      public string Characters { get; private set; }

      private CharacterCode() { }
      public CharacterCode(
        string characters)
      {
         Characters = characters;
      }

      public override void Traverse(
        Func<Code, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return Characters;
      }
   }

   public class MergeCode: Code
   {
      public string SceneId { get; private set; }

      private MergeCode() { }
      public MergeCode(
         string sceneId)
      {
         SceneId = sceneId;
      }

      public override void Traverse(
        Func<Code, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return "merge " + SceneId;
      }
   }

   public class SceneCode: Code
   {
      public string SceneId { get; private set; }

      private SceneCode() { }
      public SceneCode(
         string sceneId)
      {
         SceneId = sceneId;
      }

      public override void Traverse(
        Func<Code, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return "scene " + SceneId;
      }
   }

   public class SpecialCode: Code
   {
      public string Id { get; private set; }

      private SpecialCode() { }
      public SpecialCode(
        string id)
      {
         Id = id;
      }

      public override void Traverse(
        Func<Code, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return "[" + Id + "]";
      }
   }

   public class Expression
   {
      // This represents:
      //    tvOn
      //    not tvOn
      //    tvOn=mr_rogers
      //    not tvOn=mr_rogers
      public string LeftId { get; private set; }
      public string RightId { get; private set; }
      public bool Not { get; private set; }

      private Expression() { }
      public Expression(
         bool not,
         string leftId,
         string rightId)
      {
         Not = not;
         LeftId = leftId;
         RightId = rightId;
      }

      public override string ToString()
      {
         string result = "";
         if (Not)
            result += "not ";
         result += LeftId;
         if (RightId != null)
            result += "=" + RightId;
         return result;
      }
   }
}
