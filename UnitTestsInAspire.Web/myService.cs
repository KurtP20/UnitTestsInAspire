namespace UnitTestsInAspire.Web
{
    public class myService
    {
        // This is a dummy implementation of a service.
        // The service should depend on external dependencies managed by Aspire, such as Postgres, Qdrant, etc.

        public int GetRandomNumber()
        {
            return new Random().Next(0, 100);
        }   
    }
}
