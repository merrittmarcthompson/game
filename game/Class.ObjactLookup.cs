using System.Collections.Generic;

namespace Game
{
  public class ObjactLookup : Objact
  {
    public string Owner;
    public string Label;

    public ObjactLookup(
      string owner,
      string label)
    {
      Owner = owner;
      Label = label;
    }

    public override void Reduce(
      HashSet<(string, string, string)> tags,
      string defaultOwner,
      ref string text)
    {
      string value = Static.Lookup(tags, Owner, defaultOwner, Label);
      if (value == null)
      {
        text += "?" + Static.PickOwner(Owner, defaultOwner) + ":" + Label + "?";
      }
      else
      {
        text += value;
      }
    }
  }
}
