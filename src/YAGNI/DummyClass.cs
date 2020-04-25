namespace YAGNI
{
    public class DummyClass
    {
        public int Add(int a, int b)
        {
            if (a % 2 == 0)
            {
                return -1 * (a + b);
            }

            return a + b;
        }
    }
}
