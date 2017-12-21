namespace Game
{
  public class Tag
  {
    public string Owner { get; set; }
    public string Label { get; set; }
    public string Value { get; set; }

    public Tag(
      string owner,
      string label,
      string value)
    {
      Owner = owner;
      Label = label;
      Value = value;
    }

    public override string ToString()
    {
      // Ex. "lucy.herosFirstName=Johnny"
      return Owner + "." + Label + "="+ Value;
    }
  }
}
