#nullable enable
using System;
using System.Collections.Generic;

namespace Gamebook
{
   // Code trees are composed of Code objects. All of the concrete derived code classes (SequenceCode, IfCode, etc.) can masquerade as the abstract base class.

   public abstract class Code
   {
      // Traverse is the routine that allows other modules to examine the code.
      public abstract IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker = null);

      // This makes Code objects appear in the debugger.
      public abstract override string ToString();
   }

   // Sequence Code

   public class SequenceCode: Code
   {
      // This is a sequence of code operations. The Traverse function is the only way to get to the Codes.
      private readonly List<Code> Codes;

      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         foreach (var code in Codes)
            foreach (var subcode in code.Traverse(branchPicker))
               yield return subcode;
      }

      public override string ToString()
      {
         string result = "";
         foreach (var code in Codes)
         {
            string objectString = code.ToString();
            if (objectString.Length > 16)
               objectString = objectString.Substring(0, 16) + "... ";
         }
         return result;
      }

      public SequenceCode(
         List<Code> codes)
      {
         Codes = codes;
      }
   }

   public class IfCode: Code
   {
      public List<Expression> Expressions { get; private set; }
      public Code TrueCode { get; private set; }
      public Code? FalseCode { get; private set; }

      public IfCode(
         List<Expression> expressions,
         Code trueCode,
         Code? falseCode)
      {
         Expressions = expressions;
         TrueCode = trueCode;
         FalseCode = falseCode;
      }

      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         bool? branchesToExecute = branchPicker == null? null: branchesToExecute = branchPicker(Expressions);

         // This yield lets any clients see the IF itself, for example for reporting purposes. It is filtered out and ignored in most cases.
         yield return this;

         if (branchesToExecute == null || branchesToExecute == true)
            foreach (var code in TrueCode.Traverse())
               yield return code;
         if (FalseCode != null)
            if (branchesToExecute == null || branchesToExecute == false)
               foreach (var code in FalseCode.Traverse())
                  yield return code;
      }

      public override string ToString()
      {
         return "if " + Expressions.ToString();
      }
   }

   public class WhenCode: Code
   {
      public List<Expression> Expressions { get; private set; }

      public WhenCode(
         List<Expression> expressions)
      {
         Expressions = expressions;
      }

      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
      }

      public override string ToString()
      {
         return "when " + Expressions.ToString();
      }
   }

   public class WhenElseCode: Code
   {
      public WhenElseCode() { }

      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
      }

      public override string ToString()
      {
         return "when else";
      }
   }

   public class SetCode: Code
   {
      public List<Expression> Expressions { get; private set; }

      public SetCode(
         List<Expression> expressions)
      {
         Expressions = expressions;
      }

      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
      }

      public override string ToString()
      {
         return "set" + Expressions.ToString();
      }
   }

   public class ScoreCode: Code
   {
      public List<string> Ids { get; private set; }
      public bool SortOnly { get; private set; }

      public ScoreCode(
         List<string> ids,
         bool sortOnly)
      {
         Ids = ids;
         SortOnly = sortOnly;
      }
      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
      }

      public override string ToString()
      {
         return "score " + Ids.ToString();
      }
   }

   public class TextCode: Code
   {
      public string Id { get; private set; }
      public string Text { get; private set; }

      public TextCode(
         string id,
         string text)
      {
         Id = id;
         Text = text;
      }

      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
      }

      public override string ToString()
      {
         return Text.ToString();
      }
   }

   public class CharacterCode: Code
   {
      public string Characters { get; private set; }

      public CharacterCode(
        string characters)
      {
         Characters = characters;
      }
      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
      }

      public override string ToString()
      {
         return Characters;
      }
   }

   public class MergeCode: Code
   {
      public string? SceneId { get; private set; }

      public MergeCode(
         string? sceneId)
      {
         SceneId = sceneId;
      }

      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
      }

      public override string ToString()
      {
         return "merge " + SceneId;
      }
   }

   public class ReturnCode: Code
   {
      public ReturnCode() { }

      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
      }

      public override string ToString()
      {
         return "return";
      }
   }

   public class SceneCode: Code
   {
      public string SceneId { get; private set; }

      public SceneCode(
         string sceneId)
      {
         SceneId = sceneId;
      }

      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
      }

      public override string ToString()
      {
         return "scene " + SceneId;
      }
   }

   public class SpecialCode: Code
   {
      public string Id { get; private set; }

      public SpecialCode(
        string id)
      {
         Id = id;
      }
      public override IEnumerable<Code> Traverse(
         Func<List<Expression>, bool?>? branchPicker)
      {
         yield return this;
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
      public string? RightId { get; private set; }
      public bool Not { get; private set; }

      public Expression(
         bool not,
         string leftId,
         string? rightId)
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
