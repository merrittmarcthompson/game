using System;

namespace Game
{
  public static partial class Static
  {
    public static bool NoCaseEquals(
      this string left,
      string right)
    {
      return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
  }
}
