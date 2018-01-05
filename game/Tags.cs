using System;
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

      public IEnumerable<(string, string, string)> All()
      {
         return
            from tag in Collection
            select (tag.Name, tag.Label, tag.Value);
      }

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

      public IEnumerable<string> AllWithNameAndLabel(
        string name,
        string label)
      {
         return
           from tag in Collection
           where tag.Name == name && tag.Label == label
           select tag.Value;
      }

      public string FirstWithNameAndLabel(
        string name,
        string label)
      {
         var selected = AllWithNameAndLabel(name, label);
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

      public void Remove(
        string name,
        string label,
        string value)
      {
         var selected =
           from tag in Collection
           where tag.Name == name && tag.Label == label && tag.Value == value
           select tag;
         Collection.RemoveWhere(tag => selected.Contains(tag));
      }

      public IEnumerable<(string, string)> AllWithLabel(
        string label)
      {
         return
           from tag in Collection
           where tag.Label == label
           select (tag.Name, tag.Value);
      }

      public IEnumerable<(string, string)> AllWithName(
         string name)
      {
         return
            from tag in Collection
            where tag.Name == name
            select (tag.Label, tag.Value);
      }
   }
}
