using System.Collections.Generic;
using System.Linq;

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
      HashSet<(string, string, string)> properties,
      ref string text,
      ref Dictionary<string, string> directives)
    {
      string setting = properties.MyLookup("~", Id);
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
