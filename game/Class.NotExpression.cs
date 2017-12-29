namespace Game
{
   public class NotExpression
   {
      public bool Not;
      public Expression Expression;

      public override string ToString()
      {
         string result = "";
         if (Not)
         {
            result += "not ";
         }
         result += Expression;
         return result;
      }
  }
}
