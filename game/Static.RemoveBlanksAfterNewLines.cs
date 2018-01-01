namespace Game
{
   public static partial class Static
   {
      public static string RemoveBlanksAfterNewLines(
        string text)
      {
         string fixedText = "";
         // Remove blanks in front too.
         bool hadNewLine = true;
         foreach (char letter in text)
         {
            if (letter == '\n')
            {
               hadNewLine = true;
               fixedText += letter;
            }
            else if (letter == ' ')
            {
               if (!hadNewLine)
                  fixedText += letter;
            }
            else
            {
               hadNewLine = false;
               fixedText += letter;
            }
         }
         return fixedText;
      }
   }
}
