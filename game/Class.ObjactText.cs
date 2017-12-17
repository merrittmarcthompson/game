using System.Collections.Generic;

namespace Game
{
  public class ObjactText : Objact
  {
    // This is the text.
    public string Text;

    public ObjactText(
      string text)
    {
      Text = text;
    }

    public override void Reduce(
      HashSet<Tag> properties,
      string defaultOwner,
      ref string text)
    {
      text += Text;
    }
  }
}
