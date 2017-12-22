using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
  public class ObjectTag : Game.Object
  {
    public string SpecifiedName;
    public string Label;
    public string Value;

    public ObjectTag(
      string specifiedName,
      string label,
      string value)
    {
      SpecifiedName = specifiedName;
      Label = label;
      Value = value;
    }

    public override void Reduce(
      Tags tags,
      string defaultName,
      ref string text)
    {
      // Get rid of any existing tags for the name and label.
      tags.Remove(SpecifiedName, defaultName, Label);

      // Create a new tag in the list. We're assuming there must be a defaultName.
      string name = SpecifiedName;
      if (name == null || name == "")
      {
        name = defaultName;
      }
      tags.Add(name, Label, Value);
    }
  }
}
