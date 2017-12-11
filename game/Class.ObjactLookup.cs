using System.Collections.Generic;

namespace Game
{
  public class ObjactLookup : Objact
  {
    public string Id;

    public ObjactLookup(
      string id)
    {
      Id = id;
    }

    public override void Reduce(
      Dictionary<string, string> properties,
      ref string text,
      ref Dictionary<string, string> directives)
    {
      string setting = properties[Id];
      if (setting == null)
      {
        text += "?" + Id + "?";
      }
      else
      {
        text += setting;
      }
    }
  }
}
