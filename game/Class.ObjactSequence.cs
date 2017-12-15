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

    // 'text' and 'directives' are accumulators.
    public override void Reduce(
      HashSet<(string, string, string)> properties,
      ref string text,
      ref Dictionary<string, string> directives)
    {
      foreach (var objact in Objacts)
      {
        objact.Reduce(properties, ref text, ref directives);
      }
    }
  }
}
