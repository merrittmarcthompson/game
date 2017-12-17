﻿using System.Collections.Generic;
using System.Linq;

namespace Game
{
  public static partial class Static
  {
    public static IEnumerable<string> MultiLookup(
      HashSet<Tag> tags,
      string specifiedOwner,
      string defaultOwner,
      string label)
    {
      return
        from tag in MultiLookupTags(tags, specifiedOwner, defaultOwner, label)
        select tag.Value;
    }
  }
}
