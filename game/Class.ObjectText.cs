using System.Collections.Generic;

namespace Game
{
  public class ObjectText : Game.Object
  {
    // This is the text.
    public string Text;

    public ObjectText(
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
