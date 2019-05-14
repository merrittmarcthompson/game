namespace Game
{
   public static partial class Transform
   {
      public static string RemoveExtraBlanks(
        string text)
      {
         string fixedText = "";
         // true: skip leading spaces too.
         bool hadSpace = true;
         foreach (char letter in text)
         {
            if (letter == ' ')
            {
               if (!hadSpace)
               {
                  hadSpace = true;
                  fixedText += letter;
               }
            }
            else
            {
               hadSpace = false;
               fixedText += letter;
            }
         }
         // Get rid of trailing spaces too.
         return fixedText.Trim();
      }
   }
}
