using System.Collections.Generic;
using System.Linq;

namespace Game
{
  public static partial class Static
  {
    public static IEnumerable<string> MultiLookup(
      HashSet<(string, string, string)> tags,
      string owner,
      string defaultOwner,
      string label)
    {
      var pickedOwner = Static.PickOwner(owner, defaultOwner);
      // Tags consist of an owner (Item1), a label it has (Item2), and the value of the label (Item3).
      return
        from tag in tags
        where tag.Item1 == pickedOwner && tag.Item2 == label
        select tag.Item3;
    }
  }
}
