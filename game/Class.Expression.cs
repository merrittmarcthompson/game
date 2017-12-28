using System.Collections.Generic;

namespace Game
{
  public class Expression
  {
    public string LeftName { get; set; }
    public List<string> LeftLabels { get; set; } = new List<string>();
    public string RightName { get; set; }
    public List<string> RightLabels { get; set; } = new List<string>();
  }
}
