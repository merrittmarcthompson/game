using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
  public abstract class Objact
  {
    public abstract void Reduce(
      Dictionary<string, string> properties,
      ref string text,
      ref Dictionary<string, string> directives);
  }
}
