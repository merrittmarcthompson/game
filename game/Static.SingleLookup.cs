using System.Collections.Generic;
using System.Linq;

namespace Game
{
  public static partial class Static
  {
    public static string SingleLookup(
      HashSet<Tag> tags,
      string specifiedOwner,
      string defaultOwner,
      string label)
    {
      var selected = Static.MultiLookup(tags, specifiedOwner, defaultOwner, label);
      if (selected.Any())
        return selected.First();
      return null;
    }
  }
}
