using System.Collections.Generic;

namespace Game
{
  public class Location
  {
    // This is the description of the location from the graphml.
    public string SourceText;

    // These come from the group labels in the graphml.
    public string VisualZoneId;
    public string AuditoryZoneId;

    // Key is a hyperlink string extracted from the text. Value is a node ID from the graphml.
    public Dictionary<string, string> Targets;

    // For some quick debugging.
    public override string ToString()
    {
      var result = "";
      result += "Text: " + SourceText + "\n";
      result += "Auditory zone: " + AuditoryZoneId + "\n";
      result += "Visual zone: " + VisualZoneId + "\n";
      foreach (var target in Targets)
      {
        result += "Target: " + target.Key + "=>" + target.Value + "\n";
      }
      return result;
    }
  }
}