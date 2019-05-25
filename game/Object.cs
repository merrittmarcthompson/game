using System;
using System.Collections.Generic;

namespace Gamebook
{
   // Put this abstract class and all its short children in this one file so I don't have to flip betweent them all the time.

   public abstract class Object
   {
      public abstract void Traverse(
        Func<Object, bool> examine);
      public abstract override string ToString();
   }

   public class SequenceObject : Object
   {
      // This is the sequence of objects.
      public List<Object> Objects { get; set; } = new List<Object>();
      public string SourceText = null;

      public override void Traverse(
        Func<Object, bool> examine)
      {
         foreach (var @object in Objects)
         {
            string previousSourceText = null;
            if (SourceText != null)
            {
               previousSourceText = Log.SetSourceCode(SourceText);
            }
            @object.Traverse(examine);
            Log.SetSourceCode(previousSourceText);
         }
      }

      public void Scan(
        Func<Object, bool> examine)
      {
         foreach (var @object in Objects)
         {
            string previousSourceText = null;
            if (SourceText != null)
            {
               previousSourceText = Log.SetSourceCode(SourceText);
            }
            examine(@object);
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
         foreach (var @object in Objects)
         {
            string objectString = @object.ToString();
            if (objectString.Length > 16)
            {
               objectString = objectString.Substring(0, 16) + "...";
            }
            result += separator + objectString;
            separator = "|";
         }
         return result;
      }

      public SequenceObject Append(
         SequenceObject other)
      {
         // Implements the [merge] arrow feature that merges nodes.
         Objects.AddRange(other.Objects);
         return this;
      }
   }

   public class IfObject : Object
   {
      public List<Expression> Expressions;
      public Object TrueSource;
      public Object FalseSource;

      public override void Traverse(
        Func<Object, bool> examine)
      {
         if (examine(this))
         {
            TrueSource.Traverse(examine);
         }
         else if (FalseSource != null)
         {
            FalseSource.Traverse(examine);
         }
      }
      public override string ToString()
      {
         return "if " + Expressions.ToString();
      }
   }

   public class WhenObject : Object
   {
      public List<Expression> Expressions;

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "when " + Expressions.ToString();
      }
   }

   public class SetObject : Object
   {
      public List<Expression> Expressions;

      public override void Traverse(
         Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "set" + Expressions.ToString();
      }
   }

   public class ScoreObject : Object
   {
      public List<string> Ids = null;

      public ScoreObject(
         List<string> ids)
      {
         Ids = ids;
      }
      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "score " + Ids.ToString();
      }
   }

   public class SubstitutionObject : Object
   {
      // This is produced by code like this:
      //  His name was [hero.first].
      //  [Lucy.herosFirstName] was Lucy's pet name for him.

      public string Id = null;

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "[" + Id + "]";
      }
   }

   public class TextObject : Object
   {
      public string Id;
      public SequenceObject Text;

      public TextObject(
         string id,
         SequenceObject text)
      {
         Id = id;
         Text = text;
      }
      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return Text.ToString();
      }
   }

   public class CharacterObject : Object
   {
      public string Characters;

      public CharacterObject(
        string characters)
      {
         Characters = characters;
      }

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return Characters;
      }
   }

   public class MergeObject : Object
   {
      public string SceneId;

      public MergeObject(
         string sceneId)
      {
         SceneId = sceneId;
      }

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }

      public override string ToString()
      {
         return "merge";
      }
   }

   public class SceneObject : Object
   {
      public string SceneId;

      public SceneObject(
         string sceneId)
      {
         SceneId = sceneId;
      }
      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "scene";
      }
   }
   public class SpecialObject : Object
   {
      public string Id;

      public SpecialObject(
        string id)
      {
         Id = id;
      }

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "[" + Id + "]";
      }
   }
}
