using System;
using System.Collections.Generic;

namespace Gamebook
{
   // Put this abstract class and all its short children in this one file so I don't have to flip betweent them all the time.

   public abstract class Operation
   {
      public abstract void Traverse(
        Func<Operation, bool> examine);
      public abstract override string ToString();
   }

   public class SequenceOperation : Operation
   {
      // This is the sequence of objects.
      public List<Operation> Operations { get; set; } = new List<Operation>();
      public string SourceText = null;

      public override void Traverse(
        Func<Operation, bool> examine)
      {
         foreach (var operation in Operations)
         {
            string previousSourceText = null;
            if (SourceText != null)
            {
               previousSourceText = Log.SetSourceCode(SourceText);
            }
            operation.Traverse(examine);
            Log.SetSourceCode(previousSourceText);
         }
      }

      public void Scan(
        Func<Operation, bool> examine)
      {
         foreach (var operation in Operations)
         {
            string previousSourceText = null;
            if (SourceText != null)
            {
               previousSourceText = Log.SetSourceCode(SourceText);
            }
            examine(operation);
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
         foreach (var operation in Operations)
         {
            string objectString = operation.ToString();
            if (objectString.Length > 16)
            {
               objectString = objectString.Substring(0, 16) + "...";
            }
            result += separator + objectString;
            separator = "|";
         }
         return result;
      }

      public SequenceOperation Append(
         SequenceOperation other)
      {
         // Implements the [merge] arrow feature that merges nodes.
         Operations.AddRange(other.Operations);
         return this;
      }
   }

   public class IfOperation : Operation
   {
      public List<Expression> Expressions;
      public Operation TrueOperation;
      public Operation FalseOperation;

      public override void Traverse(
        Func<Operation, bool> examine)
      {
         if (examine(this))
         {
            TrueOperation.Traverse(examine);
         }
         else if (FalseOperation != null)
         {
            FalseOperation.Traverse(examine);
         }
      }
      public override string ToString()
      {
         return "if " + Expressions.ToString();
      }
   }

   public class WhenOperation : Operation
   {
      public List<Expression> Expressions;

      public override void Traverse(
        Func<Operation, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "when " + Expressions.ToString();
      }
   }

   public class SetOperation : Operation
   {
      public List<Expression> Expressions;

      public override void Traverse(
         Func<Operation, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "set" + Expressions.ToString();
      }
   }

   public class ScoreOperation : Operation
   {
      public List<string> Ids = null;

      public ScoreOperation(
         List<string> ids)
      {
         Ids = ids;
      }
      public override void Traverse(
        Func<Operation, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "score " + Ids.ToString();
      }
   }

   public class SubstitutionOperation : Operation
   {
      // This is produced by code like this:
      //  His name was [hero.first].
      //  [Lucy.herosFirstName] was Lucy's pet name for him.

      public string Id = null;

      public override void Traverse(
        Func<Operation, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "[" + Id + "]";
      }
   }

   public class TextOperation : Operation
   {
      public string Id;
      public SequenceOperation Text;

      public TextOperation(
         string id,
         SequenceOperation text)
      {
         Id = id;
         Text = text;
      }
      public override void Traverse(
        Func<Operation, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return Text.ToString();
      }
   }

   public class CharacterOperation : Operation
   {
      public string Characters;

      public CharacterOperation(
        string characters)
      {
         Characters = characters;
      }

      public override void Traverse(
        Func<Operation, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return Characters;
      }
   }

   public class MergeOperation : Operation
   {
      public string SceneId;

      public MergeOperation(
         string sceneId)
      {
         SceneId = sceneId;
      }

      public override void Traverse(
        Func<Operation, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return "merge";
      }
   }

   public class SceneOperation : Operation
   {
      public string SceneId;

      public SceneOperation(
         string sceneId)
      {
         SceneId = sceneId;
      }
      public override void Traverse(
        Func<Operation, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "scene";
      }
   }
   public class SpecialOperation : Operation
   {
      public string Id;

      public SpecialOperation(
        string id)
      {
         Id = id;
      }

      public override void Traverse(
        Func<Operation, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "[" + Id + "]";
      }
   }
}
