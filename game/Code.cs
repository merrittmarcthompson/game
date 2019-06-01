using System;
using System.Collections.Generic;

namespace Gamebook
{
   // Put this abstract class and all its short children in this one file so I don't have to flip betweent them all the time.

   public abstract class Code
   {
      public abstract void Traverse(
        Func<Code, bool> examine);
      public abstract override string ToString();

      public static Code Compile(
        string sourceCode)
      {
         // Compile the text to an object sequence.
         Log.SetSourceCode(sourceCode);
         var tokens = Transform.SourceTextToTokens(sourceCode);
         if (tokens == null)
            return null;
         return Transform.TokensToCode(tokens, sourceCode);
      }
   }

   public class SequenceCode : Code
   {
      // This is the sequence of objects.
      public List<Code> Codes { get; set; } = new List<Code>();
      public string SourceText = null;

      public override void Traverse(
        Func<Code, bool> examine)
      {
         foreach (var code in Codes)
         {
            string previousSourceText = null;
            if (SourceText != null)
            {
               previousSourceText = Log.SetSourceCode(SourceText);
            }
            code.Traverse(examine);
            Log.SetSourceCode(previousSourceText);
         }
      }

      public void Scan(
        Func<Code, bool> examine)
      {
         foreach (var code in Codes)
         {
            string previousSourceText = null;
            if (SourceText != null)
            {
               previousSourceText = Log.SetSourceCode(SourceText);
            }
            examine(code);
            if (previousSourceText != null)
            {
               Log.SetSourceCode(previousSourceText);
            }
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
            {
               objectString = objectString.Substring(0, 16) + "...";
            }
            result += separator + objectString;
            separator = "|";
         }
         return result;
      }

      public SequenceCode Append(
         SequenceCode other)
      {
         // Implements the [merge] arrow feature that merges nodes.
         Codes.AddRange(other.Codes);
         return this;
      }
   }

   public class IfCode : Code
   {
      public List<Expression> Expressions { get; private set; }
      public Code TrueCode { get; private set; }
      public Code FalseCode { get; private set; }

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
         {
            TrueCode.Traverse(examine);
         }
         else if (FalseCode != null)
         {
            FalseCode.Traverse(examine);
         }
      }
      public override string ToString()
      {
         return "if " + Expressions.ToString();
      }
   }

   public class WhenCode : Code
   {
      public List<Expression> Expressions;

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

   public class SetCode : Code
   {
      public List<Expression> Expressions;

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

   public class ScoreCode : Code
   {
      public List<string> Ids = null;

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

   public class SubstitutionCode : Code
   {
      // This is produced by code like this:
      //  His name was [hero.first].
      //  [Lucy.herosFirstName] was Lucy's pet name for him.

      public string Id = null;

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

   public class TextCode : Code
   {
      public string Id;
      public SequenceCode Text;

      public TextCode(
         string id,
         SequenceCode text)
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

   public class CharacterCode : Code
   {
      public string Characters;

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

   public class MergeCode : Code
   {
      public string SceneId;

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
         return "merge";
      }
   }

   public class SceneCode : Code
   {
      public string SceneId;

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
         return "scene";
      }
   }
   public class SpecialCode : Code
   {
      public string Id;

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
}
