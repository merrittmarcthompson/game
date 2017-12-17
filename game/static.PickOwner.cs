namespace Game
{
  public static partial class Static
  {
    public static string PickOwner(
      string owner,
      string defaultOwner)
    {
      if (owner == null || owner == "")
      {
        if (defaultOwner == null || defaultOwner == "")
          return "~";
        else
          return defaultOwner;
      }
      else
        return owner;
    }
  }
}
