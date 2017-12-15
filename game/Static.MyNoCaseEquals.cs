using System;

namespace Game
{
  public static partial class Static
  {
    public static bool MyNoCaseEquals(
      this string left,
      string right)
    {
      return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
  }
}
