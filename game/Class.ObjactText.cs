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

    public override void Reduce(
      HashSet<(string, string, string)> properties,
      string defaultOwner,
      ref string text)
    {
      text += TheText;
    }
  }
}
