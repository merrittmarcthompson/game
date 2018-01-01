namespace Game
{
   public class TokenType
   {
      public string Name { get; set; }

      public TokenType(
        string name)
      {
         Name = name;
      }

      public override string ToString()
      {
         return Name;
      }
   }
}
