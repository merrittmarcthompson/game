namespace Game
{
   public static partial class Transform
   {
      public static string JoinTexts(
        string left,
        string right)
      {
         // When you concatenate normalized texts, always put a space between them, unless the left one ends in a plus sign, ex. "hello" joined with "there" => "hello there", but "hello+" joined with "there" => "hellothere".  Useful for things like 'He said "+' joined with "'I am a fish."' => 'He said "I am a fish."'
         if (left.Length == 0)
            return right;
         if (left[left.Length - 1] != '+')
            return left + " " + right;
         return left.Substring(0, left.Length - 1) + right;
      }
   }
}