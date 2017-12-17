using System.Collections.Generic;

namespace Game
{
  public class ObjactLookup : Objact
  {
    // This is produced by code like this:
    //  This is a paragraph.[p]
    //  His name was [First].
    //  [Lucy:{hero's first name}] was Lucy's pet name for him.

    // Owner is the explicitly specified owner from the code, ex. "Lucy". It's null or "" if there is no explicit owner.
    public string SpecifiedOwner;
    // Label is the label itself, ex "p", "First", "hero's first name".
    public string Label;

    public ObjactLookup(
      string specifiedOwner,
      string label)
    {
      SpecifiedOwner = specifiedOwner;
      Label = label;
    }

    public override void Reduce(
      HashSet<Tag> tags,
      string defaultOwner,
      ref string text)
    {
      // The defaultOwner is the context that the Reduce is being run in, i.e. we reducing the text for a location node or story node. The owner is the location or story ID. This is used when there is no explicit owner specified.
      string value = Static.SingleLookup(tags, SpecifiedOwner, defaultOwner, Label);
      if (value == null)
      {
        // Ex. "[{Lucy}? {}?:{hero's first name}]"
        text += "[{" + SpecifiedOwner + "}? {" + defaultOwner + "}?:{" + Label + "}]";
      }
      else
      {
        text += value;
      }
    }
  }
}
