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
      HashSet<(string, string, string)> tags,
      string defaultOwner,
      ref string text)
    {
      foreach (var objact in Objacts)
      {
        objact.Reduce(tags, defaultOwner, ref text);
      }
    }
  }
}
