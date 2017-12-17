using System.Collections.Generic;
using System.Linq;

namespace Game
{
  public static partial class Static
  {
    public static IEnumerable<Tag> MultiLookupTags(
      HashSet<Tag> tags,
      string specifiedOwner,
      string defaultOwner,
      string label)
    {
      // First try to find labels with the specified owner (if given). For example, "Lucy:{hero's first name}" has a specified owner. If there is a specified owner and you can't find the label, return an empty enumeration. The default owner is irrelevant--it's only used when there's no specified owner.
      if (specifiedOwner != null && specifiedOwner != "")
      {
        return 
          from tag in tags
          where tag.Owner == specifiedOwner && tag.Label == label
          select tag;
      }
      // If there was no specified owner, try to find labels with the default owner (if given). For example, "used" might mean "{n0::n4}:used". If you don't find any, continue on and try with the global owner.
      if (defaultOwner != null && defaultOwner != "")
      {
        var selectedWithDefault =
          from tag in tags
          where tag.Owner == defaultOwner && tag.Label == label
          select tag;
        if (selectedWithDefault.Any())
          return selectedWithDefault;
      }
      // If the default owner fails, continue on to see if you can find it with the global owner. For example, "First" coud mean "~:First".
      var selectedWithGlobal =
        from tag in tags
        where tag.Owner == "~" && tag.Label == label
        select tag;

      // If it didn't find any, this will be an empty empty enumeration--not 'null'.
      return selectedWithGlobal;
    }
  }
}