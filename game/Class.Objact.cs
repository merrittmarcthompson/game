﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
  public abstract class Objact
  {
    public abstract void Reduce(
      HashSet<(string, string, string)> tags,
      string defaultOwner,
      ref string text);
  }
}
