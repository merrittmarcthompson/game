using System.Collections.Generic;

namespace Game
{
  public class TagSpec
  {
    public string Name { get; set; }
    public List<string> Labels { get; set; } = new List<string>();
    public string Value { get; set; }
  }
}
