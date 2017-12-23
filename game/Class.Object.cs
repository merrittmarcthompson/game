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

  public class ObjectIf : Game.Object
  {
    public string Name;
    public string Label;
    public string Value;
    public bool Not;
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

  public class ObjectName : Game.Object
  {
    public string Name;

    public ObjectName(
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

  public class ObjectSequence : Game.Object
  {
    // This is the sequence of objects.
    public List<Game.Object> Objects { get; set; }

    public ObjectSequence()
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
  }

  public class ObjectSubstitution : Game.Object
  {
    // This is produced by code like this:
    //  This is a paragraph.[p]
    //  His name was [first].
    //  [Lucy.herosFirstName] was Lucy's pet name for him.

    // SpecifiedName is the explicitly specified name from the code, ex. "Lucy". It's null or "" if there is no explicit name.
    public string SpecifiedName;
    // Label is the label itself, ex "p", "first", "herosFirstName".
    public string Label;

    public ObjectSubstitution(
      string specifiedName,
      string label)
    {
      SpecifiedName = specifiedName;
      Label = label;
    }

    public override void Traverse(
      Func<Game.Object, bool> examine)
    {
      examine(this);
    }
  }

  public class ObjectTag : Game.Object
  {
    public string SpecifiedName;
    public string Label;
    public string Value;
    public bool Untag;

    public ObjectTag(
      string specifiedName,
      string label,
      string value,
      bool untag)
    {
      SpecifiedName = specifiedName;
      Label = label;
      Value = value;
      Untag = untag;
    }

    public override void Traverse(
      Func<Game.Object, bool> examine)
    {
      examine(this);
    }
  }

  public class ObjectText : Game.Object
  {
    // This is the text.
    public string Text;

    public ObjectText(
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

}
