using System.Collections.Generic;

namespace Game
{
  public class ObjactSequence : Objact
  {
    // This is the sequence of objacts.
    public List<Objact> Objacts { get; set; }

    public ObjactSequence()
    {
      Objacts = new List<Objact>();
    }

    public override void Reduce(
      Tags tags,
      string defaultName,
      ref string text)
    {
      foreach (var objact in Objacts)
      {
        objact.Reduce(tags, defaultName, ref text);
      }
    }
  }
}
