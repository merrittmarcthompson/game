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
      Tags tags,
      string defaultName,
      ref string text)
    {
      text += Text;
    }
  }
}
