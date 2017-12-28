using System;
using System.Collections.Generic;

namespace Game
{
// Put this abstract class and all its short children in this one file so I don't have to flip betweent them all the time.

  public abstract class Object
  {
    public abstract void Traverse(
      Func<Game.Object, bool> examine);
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
  }

  public class WhenObject : Game.Object
  {
    public List<NotExpression> NotExpressions;

    public override void Traverse(
      Func<Game.Object, bool> examine)
    {
      examine(this);
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
  }
}
