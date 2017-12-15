using System.Collections.Generic;

namespace Game
{
  public class ObjactText : Objact
  {
    // This is the text.
    public string TheText;

    public ObjactText(
      string text)
    {
      TheText = text;
    }

    // 'text' and 'directives' are accumulators.
    public override void Reduce(
      HashSet<(string, string, string)> properties,
      ref string text,
      ref Dictionary<string, string> directives)
    {
      text += TheText;
    }
  }
}
