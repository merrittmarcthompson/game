using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
   public class Tags
   {
      private class Tag : IEquatable<Tag>
      {
         public readonly string Name;
         public readonly string Label;
         public readonly object Value;

         public Tag(
           string name,
           string label,
           object value)
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

         public bool Equals(Tag other)
         {
            return Name == other.Name && Label == other.Label && Value == other.Value;
         }

         public override bool Equals(object other)
         {
            if (other is Tag otherTag)
               return Equals(otherTag);
            return false;
         }

         public override int GetHashCode()
         {
            var hashCode = (Name.GetHashCode() * 17 + Label.GetHashCode()) * 17 + Value.GetHashCode();
            return hashCode;
         }
      }

      private HashSet<Tag> Collection = new HashSet<Tag>();

      public IEnumerable<(string, string, object)> All()
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

      public void Unmerge(
         Tags otherTags)
      {
         Collection.RemoveWhere(tag => otherTags.Collection.Contains(tag));
      }

      public void Add(
        string name,
        string label,
        object value)
      {
         var candidateTag = new Tag(name, label, value ?? "");
         // Don't put the same tag in twice. If they look at the same pamphlet two times, the collection should not be bagged with:
         //    hero.hasRead=pamphlet
         //    hero.hasRead=pamphlet
         if (Collection.Contains(candidateTag))
            return;
         Collection.Add(candidateTag);
      }

      public IEnumerable<string> AllWithLabelAndValue(
         string label,
         object value)
      {
         return
            from tag in Collection
            where tag.Label == label && tag.Value.Equals(value)
            select tag.Name;
      }

      public IEnumerable<object> AllWithNameAndLabel(
        string name,
        string label,
        bool mustNotBeEmpty = false)
      {
         var result = 
           from tag in Collection
           where tag.Name == name && tag.Label == label
           select tag.Value;
         Log.FailWhenNull(mustNotBeEmpty && !result.Any(), null, name + "." + label);
         return result;
      }

      public object FirstWithNameAndLabel(
        string name,
        string label,
        bool mustNotBeNull = false)
      {
         var selected = AllWithNameAndLabel(name, label);
         // This can return either a string or null. If it's a boolean tag, ex. [tag hero.isShort], and it is set, then LookupFirst will return "", which means "true". If it isn't set, it will return null, which means "false".
         if (selected.Any())
            return selected.First();
         Log.FailWhenNull(mustNotBeNull, null, name + "." + label);
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
        object value)
      {
         var selected =
           from tag in Collection
           where tag.Name == name && tag.Label == label && tag.Value == value
           select tag;
         Collection.RemoveWhere(tag => selected.Contains(tag));
      }

      public IEnumerable<(string, object)> AllWithLabel(
        string label)
      {
         return
           from tag in Collection
           where tag.Label == label
           select (tag.Name, tag.Value);
      }

      public IEnumerable<(string, object)> AllWithName(
         string name)
      {
         return
            from tag in Collection
            where tag.Name == name
            select (tag.Label, tag.Value);
      }
   }
}
