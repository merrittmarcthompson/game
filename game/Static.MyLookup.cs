using System.Collections.Generic;
using System.Linq;

namespace Game
{
  public static partial class Static
  {
    public static string MyLookup(
      this HashSet<(string, string, string)> properties,
      string item,
      string property)
    {
      // Properties consist of an item, a property it has, and the value of the property.
      return properties.Where(element => element.Item1 == item && element.Item2 == property).First().Item3;
    }
  }
}
