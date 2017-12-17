using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
  public class ObjactTag : Objact
  {
    public string SpecifiedOwner;
    public string Label;
    public string Value;

    public ObjactTag(
      string specifiedOwner,
      string label,
      string value)
    {
      SpecifiedOwner = specifiedOwner;
      Label = label;
      Value = value;
    }

    public override void Reduce(
      HashSet<Tag> tags,
      string defaultOwner,
      ref string text)
    {
      // Get rid of any existing tags for the owner and label.
      var selected = Static.MultiLookupTags(tags, SpecifiedOwner, defaultOwner, Label);
      tags.RemoveWhere(tag => selected.Contains(tag));

      // Create a new tag in the list. We're assuming there must be a defaultOwner.
      string owner = SpecifiedOwner;
      if (owner == null || owner == "")
      {
        owner = defaultOwner;
      }
      tags.Add(new Tag(owner, Label, Value));
    }
  }
}
