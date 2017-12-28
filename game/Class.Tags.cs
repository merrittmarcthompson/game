﻿using System;
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
      string name,
      string label)
    {
      return
        from tag in Collection
        where tag.Name == name && tag.Label == label
        select tag;
    }

    public IEnumerable<string> LookupAll(
      string name,
      string label)
    {
      return
        from tag in Collection
        where tag.Name == name && tag.Label == label
        select tag.Value;
    }

    public string LookupFirst(
      string name,
      string label)
    {
      var selected = LookupAll(name, label);
      // LookupFirst can return either a string or null. If it's a boolean tag, ex. [tag hero.isShort], and it is set, then LookupFirst will return "", which means "true". If it isn't set, it will return null, which means "false".
      if (selected.Any())
        return selected.First();
      return null;
    }

    public void Remove(
      string name,
      string label)
    {
      var selected = 
        from tag in Collection
        where tag.Name == name && tag.Label == label
        select tag;
      Collection.RemoveWhere(tag => selected.Contains(tag));
    }

    public IEnumerable<(string, string)> LookupAllWithLabel(
      string label)
    {
      return
        from tag in Collection
        where tag.Label == label
        select (tag.Name, tag.Value);
    }
  }
}
