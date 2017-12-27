using System.Collections.Generic;
using System.Linq;

namespace Game
{
  public class Tags
  {
    private class Tag
    {
      public string Name { get; set; }
      public string Label { get; set; }
      public string Value { get; set; }

      public Tag(
        string name,
        string label,
        string value)
      {
        Name = name;
        Label = label;
        Value = value;
      }

      public override string ToString()
      {
        // Ex. "lucy.herosFirstName=Johnny"
        return Name + "." + Label + "=" + Value;
      }
    }

    private HashSet<Tag> Collection = new HashSet<Tag>();

    public void Merge(
      Tags otherTags)
    {
      Collection.UnionWith(otherTags.Collection);
    }

    public void Add(
      string name,
      string label,
      string value)
    {
      Collection.Add(new Tag(name, label, value ?? ""));
    }

    private IEnumerable<Tag> LookupTags(
      string specifiedName,
      string defaultName,
      string label)
    {
      // First try to find labels with the specified name (if given). For example, "Lucy.herosFirstName" has a specified name. If there is a specified name and you can't find the label, return an empty enumeration. The default name is irrelevant--it's only used when there's no specified name.
      if (specifiedName != null && specifiedName != "")
      {
        return
          from tag in Collection
          where tag.Name == specifiedName && tag.Label == label
          select tag;
      }
      // If there was no specified name, try to find labels with the default name (if given). For example, "used" might mean "n0::n4.used". If you don't find any, continue on and try with the global name.
      if (defaultName != null && defaultName != "")
      {
        var selectedWithDefault =
          from tag in Collection
          where tag.Name == defaultName && tag.Label == label
          select tag;
        if (selectedWithDefault.Any())
          return selectedWithDefault;
      }
      // If the default name fails, continue on to see if you can find it with the global name. For example, "First" coud mean "~.First".
      var selectedWithGlobal =
        from tag in Collection
        where tag.Name == "~" && tag.Label == label
        select tag;

      // If it didn't find any, this will be an empty empty enumeration--not 'null'.
      return selectedWithGlobal;
    }

    public IEnumerable<string> LookupAll(
      string specifiedName,
      string defaultName,
      string label)
    {
      return
        from tag in LookupTags(specifiedName, defaultName, label)
        select tag.Value;
    }

    public string LookupFirst(
      string specifiedName,
      string defaultName,
      string label)
    {
      var selected = LookupAll(specifiedName, defaultName, label);
      if (selected.Any())
        return selected.First();
      return null;
    }

    public void Remove(
      string specifiedName,
      string defaultName,
      string label)
    {
      var selected = LookupTags(specifiedName, defaultName, label);
      Collection.RemoveWhere(tag => selected.Contains(tag));
    }

    public IEnumerable<(string, string, string)> All()
    {
      foreach (var tag in Collection)
      {
        yield return (tag.Name, tag.Label, tag.Value);
      }
    }
  }
}
