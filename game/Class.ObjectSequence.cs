using System.Collections.Generic;

namespace Game
{
  public class ObjectSequence : Game.Object
  {
    // This is the sequence of objects.
    public List<Game.Object> Objects { get; set; }

    public ObjectSequence()
    {
      Objects = new List<Game.Object>();
    }

    public override void Reduce(
      Tags tags,
      string defaultName,
      ref string text)
    {
      foreach (var @object in Objects)
      {
        @object.Reduce(tags, defaultName, ref text);
      }
    }
  }
}
