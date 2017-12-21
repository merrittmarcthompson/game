using System.Collections.Generic;

namespace Game
{
  public class ObjactLookup : Objact
  {
    // This is produced by code like this:
    //  This is a paragraph.[p]
    //  His name was [first].
    //  [Lucy:herosFirstName] was Lucy's pet name for him.

    // SpecifiedName is the explicitly specified name from the code, ex. "Lucy". It's null or "" if there is no explicit name.
    public string SpecifiedName;
    // Label is the label itself, ex "p", "first", "herosFirstName".
    public string Label;

    public ObjactLookup(
      string specifiedName,
      string label)
    {
      SpecifiedName = specifiedName;
      Label = label;
    }

    public override void Reduce(
      Tags tags,
      string defaultName,
      ref string text)
    {
      // The defaultName is the context that the Reduce is being run in, i.e. we reducing the text for a location node or story node. The name is the location or story ID. This is used when there is no explicit name specified.
      string value = tags.LookupFirst(SpecifiedName, defaultName, Label);
      if (value == null)
      {
        // Ex. "[{Lucy}? {}?:{hero's first name}]"
        text += "[{" + SpecifiedName + "}? {" + defaultName + "}?:{" + Label + "}]";
      }
      else
      {
        text += value;
      }
    }
  }
}
