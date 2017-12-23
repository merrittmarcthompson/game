namespace Game
{
  public static partial class Static
  {
    // ex. change "map.boneyard-simplified" into "map_boneyard_simplified".
    public static string MakeIntoId(
      string text)
    {
      var result = "";
      foreach (var letter in text)
      {
        if (letter >= 'a' && letter <= 'z' || letter >= 'A' && letter <= 'Z' || letter >= '0' && letter <= '9')
        {
          result += letter;
        }
        else
        {
          result += '_';
        }
      }
      return result;
    }
  }
}