using System;
using System.Collections.Generic;

namespace Game
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
               previousSourceText = Log.SetSourceText(SourceText);
            }
            @object.Traverse(examine);
            if (previousSourceText != null)
            {
               Log.SetSourceText(previousSourceText);
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
   }

   public class IfObject : Object
   {
      public List<NotExpression> NotExpressions;
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
         return "if " + NotExpressions.ToString();
      }
   }

   public class WhenObject : Object
   {
      public List<NotExpression> NotExpressions;

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "when " + NotExpressions.ToString();
      }
   }

   public class NameObject : Object
   {
      public string Name;

      public NameObject(
        string name)
      {
         Name = name;
      }

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "name " + Name;
      }
   }

   public class SubstitutionObject : Object
   {
      // This is produced by code like this:
      //  His name was [hero.first].
      //  [Lucy.herosFirstName] was Lucy's pet name for him.

      public Expression Expression = new Expression();

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return "[" + Expression.ToString() + "]";
      }
   }

   public class TagObject : Object
   {
      public Expression Expression;
      public SequenceObject RightText;
      public bool IsUntag;
      public bool IsBag;

      public TagObject(
        Expression expression,
        SequenceObject rightText,
        bool isUntag,
        bool isBag)
      {
         Expression = expression;
         RightText = rightText;
         IsUntag = isUntag;
         IsBag = isBag;
      }

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
         if (RightText != null)
         {
            RightText.Traverse(examine);
         }
      }
      public override string ToString()
      {
         var id = "tag";
         if (IsUntag)
         {
            id = "untag";
         }
         else if (IsBag)
         {
            id = "bag";
         }
         return id + Expression.ToString();
      }
   }

   public class TextObject : Object
   {
      // This is the text.
      public string Text;

      public TextObject(
        string text)
      {
         Text = text;
      }

      public override void Traverse(
        Func<Object, bool> examine)
      {
         examine(this);
      }
      public override string ToString()
      {
         return Text;
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
