using System;
using System.Collections.Generic;

namespace Game
{
   // Put this abstract class and all its short children in this one file so I don't have to flip betweent them all the time.

   public abstract class Object
   {
      public abstract void Traverse(
        Func<Game.Object, bool> examine);
      public abstract override string ToString();
   }

   public class SequenceObject : Game.Object
   {
      // This is the sequence of objects.
      public List<Game.Object> Objects { get; set; }

      public SequenceObject()
      {
         Objects = new List<Game.Object>();
      }

      public override void Traverse(
        Func<Game.Object, bool> examine)
      {
         foreach (var @object in Objects)
         {
            examine(@object);
         }
      }

      public bool ContainsText()
      {
         foreach (var @object in Objects)
         {
            if (@object is TextObject)
               return true;
         }
         return false;
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
   }

   public class IfObject : Game.Object
   {
      public List<NotExpression> NotExpressions;
      public Game.Object TrueSource;
      public Game.Object FalseSource;

      public override void Traverse(
        Func<Game.Object, bool> examine)
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
         return "if " + NotExpressions.ToString();
      }
   }

   public class WhenObject : Game.Object
   {
      public List<NotExpression> NotExpressions;

      public override void Traverse(
        Func<Game.Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "when " + NotExpressions.ToString();
      }
   }

   public class NameObject : Game.Object
   {
      public string Name;

      public NameObject(
        string name)
      {
         Name = name;
      }

      public override void Traverse(
        Func<Game.Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "name " + Name;
      }
   }

   public class SubstitutionObject : Game.Object
   {
      // This is produced by code like this:
      //  His name was [hero.first].
      //  [Lucy.herosFirstName] was Lucy's pet name for him.

      public Expression Expression = new Expression();

      public override void Traverse(
        Func<Game.Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "[" + Expression.ToString() + "]";
      }
   }

   public class TagObject : Game.Object
   {
      public Expression Expression;
      public bool Untag;

      public TagObject(
        Expression expression,
        bool untag)
      {
         Expression = expression;
         Untag = untag;
      }

      public override void Traverse(
        Func<Game.Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return (Untag ? "untag " : "tag ") + Expression.ToString();
      }
   }

   public class TextObject : Game.Object
   {
      // This is the text.
      public string Text;

      public TextObject(
        string text)
      {
         Text = text;
      }

      public override void Traverse(
        Func<Game.Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return Text;
      }
   }

   public class SpecialObject : Game.Object
   {
      public string Id;

      public SpecialObject(
        string id)
      {
         Id = id;
      }

      public override void Traverse(
        Func<Game.Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "[" + Id + "]";
      }
   }
}
