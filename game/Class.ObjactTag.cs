using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
  public class ObjactTag : Objact
  {
    public string Owner;
    public string Label;
    public string Value;

    public ObjactTag(
      string owner,
      string label,
      string value)
    {
      Owner = owner;
      Label = label;
      Value = value;
    }

    public override void Reduce(
      HashSet<(string, string, string)> tags,
      string defaultOwner,
      ref string text)
    {
      tags.Add((Static.PickOwner(Owner, defaultOwner), Label, Value));
    }
  }
}
